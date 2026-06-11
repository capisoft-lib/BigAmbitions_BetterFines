using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BetterFines
{
    /// <summary>Oriented road segments with legal travel direction, baked offline from Gley edges.</summary>
    internal static class RoadDirectionIndex
    {
        private const string CsvFileName = "road_direction_segments.csv";
        private const string DataFolderName = "Data";
        private const float GridCellSizeM = 64f;

        internal readonly struct RoadSegment
        {
            internal RoadSegment(
                int segmentId,
                Vector3 center,
                Vector3 forward,
                Vector3 lateral,
                float halfLengthM,
                float halfWidthM,
                Bounds bounds)
            {
                SegmentId = segmentId;
                Center = center;
                Forward = forward;
                Lateral = lateral;
                HalfLengthM = halfLengthM;
                HalfWidthM = halfWidthM;
                Bounds = bounds;
            }

            internal int SegmentId { get; }
            internal Vector3 Center { get; }
            internal Vector3 Forward { get; }
            internal Vector3 Lateral { get; }
            internal float HalfLengthM { get; }
            internal float HalfWidthM { get; }
            internal Bounds Bounds { get; }
        }

        private static RoadSegment[] _segments = System.Array.Empty<RoadSegment>();
        private static readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>();
        private static float _gridMinX;
        private static float _gridMinZ;
        private static bool _loaded;

        internal static int SegmentCount => _segments.Length;
        internal static bool IsLoaded => _loaded;

        internal static void Initialize(string modRootPath)
        {
            _segments = System.Array.Empty<RoadSegment>();
            _grid.Clear();
            _loaded = false;

            if (string.IsNullOrEmpty(modRootPath))
                return;

            var path = Path.Combine(modRootPath, DataFolderName, CsvFileName);
            if (!File.Exists(path))
            {
                ModLog.Warn("Missing road direction CSV | path=" + DataFolderName + "/" + CsvFileName);
                return;
            }

            try
            {
                LoadFromCsv(path);
                _loaded = _segments.Length > 0;
                ModLog.Info("Road direction index ready | segments=" + _segments.Length);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("Failed to load road direction CSV: " + ex.Message);
            }
        }

        internal static void Invalidate()
        {
            _segments = System.Array.Empty<RoadSegment>();
            _grid.Clear();
            _loaded = false;
        }

        internal static bool TryFindNearestSegment(
            Vector3 position,
            float maxDistanceMeters,
            out RoadSegment segment,
            out float centerlineDistanceM)
        {
            segment = default;
            centerlineDistanceM = float.MaxValue;

            if (!_loaded || _segments.Length == 0)
                return false;

            var maxSq = maxDistanceMeters * maxDistanceMeters;
            var candidates = CollectCandidates(position);
            var bestIndex = -1;
            var bestSq = float.MaxValue;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = _segments[candidates[i]];
                if (!TryGetCenterlineDistanceSq(candidate, position, out var distSq) || distSq > maxSq)
                    continue;

                if (distSq >= bestSq)
                    continue;

                bestSq = distSq;
                bestIndex = candidates[i];
            }

            if (bestIndex < 0)
                return false;

            segment = _segments[bestIndex];
            centerlineDistanceM = Mathf.Sqrt(bestSq);
            return true;
        }

        private static void LoadFromCsv(string path)
        {
            var inv = CultureInfo.InvariantCulture;
            var parsed = new List<RoadSegment>(8192);

            using (var reader = new StreamReader(path))
            {
                var header = reader.ReadLine();
                if (string.IsNullOrEmpty(header))
                    return;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var cols = line.Split(',');
                    if (cols.Length < 16)
                        continue;

                    if (!int.TryParse(cols[0], NumberStyles.Integer, inv, out var segmentId) ||
                        !TryParseFloat(cols[6], inv, out var centerX) ||
                        !TryParseFloat(cols[7], inv, out var centerY) ||
                        !TryParseFloat(cols[8], inv, out var centerZ) ||
                        !TryParseFloat(cols[9], inv, out var forwardX) ||
                        !TryParseFloat(cols[10], inv, out var forwardZ) ||
                        !TryParseFloat(cols[11], inv, out var halfLength) ||
                        !TryParseFloat(cols[12], inv, out var halfWidth) ||
                        !TryParseFloat(cols[13], inv, out var boundsMinX) ||
                        !TryParseFloat(cols[14], inv, out var boundsMinZ) ||
                        !TryParseFloat(cols[15], inv, out var boundsMaxX) ||
                        !TryParseFloat(cols[16], inv, out var boundsMaxZ))
                        continue;

                    var forward = new Vector3(forwardX, 0f, forwardZ);
                    if (forward.sqrMagnitude < 0.0001f)
                        continue;
                    forward.Normalize();

                    var lateral = new Vector3(-forward.z, 0f, forward.x);
                    var center = new Vector3(centerX, centerY, centerZ);
                    var boundsCenter = new Vector3(
                        (boundsMinX + boundsMaxX) * 0.5f,
                        centerY,
                        (boundsMinZ + boundsMaxZ) * 0.5f);
                    var boundsSize = new Vector3(
                        Mathf.Max(0.1f, boundsMaxX - boundsMinX),
                        4f,
                        Mathf.Max(0.1f, boundsMaxZ - boundsMinZ));

                    parsed.Add(new RoadSegment(
                        segmentId,
                        center,
                        forward,
                        lateral,
                        halfLength,
                        halfWidth,
                        new Bounds(boundsCenter, boundsSize)));
                }
            }

            _segments = parsed.ToArray();
            BuildGrid();
        }

        private static void BuildGrid()
        {
            _grid.Clear();
            if (_segments.Length == 0)
                return;

            _gridMinX = _segments[0].Bounds.min.x;
            _gridMinZ = _segments[0].Bounds.min.z;

            for (var i = 0; i < _segments.Length; i++)
            {
                var bounds = _segments[i].Bounds;
                _gridMinX = Mathf.Min(_gridMinX, bounds.min.x);
                _gridMinZ = Mathf.Min(_gridMinZ, bounds.min.z);

                var minCellX = Mathf.FloorToInt((bounds.min.x - _gridMinX) / GridCellSizeM);
                var maxCellX = Mathf.FloorToInt((bounds.max.x - _gridMinX) / GridCellSizeM);
                var minCellZ = Mathf.FloorToInt((bounds.min.z - _gridMinZ) / GridCellSizeM);
                var maxCellZ = Mathf.FloorToInt((bounds.max.z - _gridMinZ) / GridCellSizeM);

                for (var cx = minCellX; cx <= maxCellX; cx++)
                {
                    for (var cz = minCellZ; cz <= maxCellZ; cz++)
                    {
                        var key = CellKey(cx, cz);
                        if (!_grid.TryGetValue(key, out var bucket))
                        {
                            bucket = new List<int>(4);
                            _grid[key] = bucket;
                        }

                        bucket.Add(i);
                    }
                }
            }
        }

        private static List<int> CollectCandidates(Vector3 position)
        {
            var cellX = Mathf.FloorToInt((position.x - _gridMinX) / GridCellSizeM);
            var cellZ = Mathf.FloorToInt((position.z - _gridMinZ) / GridCellSizeM);
            var merged = new List<int>(16);
            var seen = new HashSet<int>();

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dz = -1; dz <= 1; dz++)
                {
                    if (!_grid.TryGetValue(CellKey(cellX + dx, cellZ + dz), out var bucket))
                        continue;

                    for (var i = 0; i < bucket.Count; i++)
                    {
                        var segmentIndex = bucket[i];
                        if (seen.Add(segmentIndex))
                            merged.Add(segmentIndex);
                    }
                }
            }

            return merged;
        }

        private static bool TryGetCenterlineDistanceSq(RoadSegment segment, Vector3 position, out float distanceSq)
        {
            distanceSq = float.MaxValue;

            var offset = position - segment.Center;
            offset.y = 0f;
            var along = Vector3.Dot(offset, segment.Forward);
            var across = Vector3.Dot(offset, segment.Lateral);

            if (Mathf.Abs(across) > segment.HalfWidthM)
                return false;

            var alongClamped = Mathf.Clamp(along, -segment.HalfLengthM, segment.HalfLengthM);
            var nearest = segment.Center + segment.Forward * alongClamped;
            var delta = position - nearest;
            delta.y = 0f;
            distanceSq = delta.sqrMagnitude;
            return true;
        }

        private static long CellKey(int cellX, int cellZ) =>
            ((long)cellX << 32) ^ (uint)cellZ;

        private static bool TryParseFloat(string value, CultureInfo inv, out float result) =>
            float.TryParse(value, NumberStyles.Float, inv, out result);
    }
}
