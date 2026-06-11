using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BetterFines
{
    /// <summary>Oriented 80 km/h rectangles baked offline; default limit elsewhere is 50 km/h.</summary>
    internal static class SpeedZone80Index
    {
        private const string CsvFileName = "road_speed_zones_80.csv";
        private const string DataFolderName = "Data";
        private const float DefaultLimitKmh = 50f;
        private const float HighwayLimitKmh = 80f;
        private const float GridCellSizeM = 64f;
        private const int GridZoneThreshold = 32;

        private readonly struct SpeedZone
        {
            internal SpeedZone(
                Vector3 center,
                Vector3 forward,
                Vector3 lateral,
                float halfLengthM,
                float halfWidthM,
                Bounds bounds)
            {
                Center = center;
                Forward = forward;
                Lateral = lateral;
                HalfLengthM = halfLengthM;
                HalfWidthM = halfWidthM;
                Bounds = bounds;
            }

            internal Vector3 Center { get; }
            internal Vector3 Forward { get; }
            internal Vector3 Lateral { get; }
            internal float HalfLengthM { get; }
            internal float HalfWidthM { get; }
            internal Bounds Bounds { get; }
        }

        private static SpeedZone[] _zones = System.Array.Empty<SpeedZone>();
        private static readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>();
        private static float _gridMinX;
        private static float _gridMinZ;
        private static bool _loaded;

        internal static float DefaultLimit => DefaultLimitKmh;
        internal static int ZoneCount => _zones.Length;
        internal static bool IsLoaded => _loaded;

        internal static void Initialize(string modRootPath)
        {
            _zones = System.Array.Empty<SpeedZone>();
            _grid.Clear();
            _loaded = false;

            if (string.IsNullOrEmpty(modRootPath))
                return;

            var path = Path.Combine(modRootPath, DataFolderName, CsvFileName);
            if (!File.Exists(path))
            {
                ModLog.Warn("Missing speed zone CSV | path=" + DataFolderName + "/" + CsvFileName +
                            " | default_limit=" + DefaultLimitKmh.ToString("0"));
                return;
            }

            try
            {
                LoadFromCsv(path);
                _loaded = _zones.Length > 0;
                ModLog.Info("Speed zone index ready | zones=" + _zones.Length +
                            " | default_limit=" + DefaultLimitKmh.ToString("0") +
                            " | highway_limit=" + HighwayLimitKmh.ToString("0"));
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("Failed to load speed zone CSV: " + ex.Message);
            }
        }

        internal static void Invalidate()
        {
            _zones = System.Array.Empty<SpeedZone>();
            _grid.Clear();
            _loaded = false;
        }

        internal static bool ContainsHighwayZone(Vector3 position)
        {
            if (!_loaded || _zones.Length == 0)
                return false;

            if (_zones.Length <= GridZoneThreshold)
            {
                for (var i = 0; i < _zones.Length; i++)
                {
                    if (ContainsPoint(_zones[i], position))
                        return true;
                }

                return false;
            }

            var candidates = CollectCandidates(position);
            for (var i = 0; i < candidates.Count; i++)
            {
                if (ContainsPoint(_zones[candidates[i]], position))
                    return true;
            }

            return false;
        }

        private static void LoadFromCsv(string path)
        {
            var inv = CultureInfo.InvariantCulture;
            var parsed = new List<SpeedZone>(1024);

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
                    if (cols.Length < 18)
                        continue;

                    if (!TryParseFloat(cols[7], inv, out var centerX) ||
                        !TryParseFloat(cols[8], inv, out var centerY) ||
                        !TryParseFloat(cols[9], inv, out var centerZ) ||
                        !TryParseFloat(cols[10], inv, out var forwardX) ||
                        !TryParseFloat(cols[11], inv, out var forwardZ) ||
                        !TryParseFloat(cols[12], inv, out var halfLength) ||
                        !TryParseFloat(cols[13], inv, out var halfWidth) ||
                        !TryParseFloat(cols[14], inv, out var boundsMinX) ||
                        !TryParseFloat(cols[15], inv, out var boundsMinZ) ||
                        !TryParseFloat(cols[16], inv, out var boundsMaxX) ||
                        !TryParseFloat(cols[17], inv, out var boundsMaxZ))
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

                    parsed.Add(new SpeedZone(
                        center,
                        forward,
                        lateral,
                        halfLength,
                        halfWidth,
                        new Bounds(boundsCenter, boundsSize)));
                }
            }

            _zones = parsed.ToArray();
            if (_zones.Length <= GridZoneThreshold)
            {
                _grid.Clear();
                return;
            }

            BuildGrid();
        }

        private static void BuildGrid()
        {
            _grid.Clear();
            if (_zones.Length == 0 || _zones.Length <= GridZoneThreshold)
                return;

            _gridMinX = _zones[0].Bounds.min.x;
            _gridMinZ = _zones[0].Bounds.min.z;

            for (var i = 0; i < _zones.Length; i++)
            {
                var bounds = _zones[i].Bounds;
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
            var merged = new List<int>(8);
            var seen = new HashSet<int>();

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dz = -1; dz <= 1; dz++)
                {
                    if (!_grid.TryGetValue(CellKey(cellX + dx, cellZ + dz), out var bucket))
                        continue;

                    for (var i = 0; i < bucket.Count; i++)
                    {
                        var zoneIndex = bucket[i];
                        if (seen.Add(zoneIndex))
                            merged.Add(zoneIndex);
                    }
                }
            }

            return merged;
        }

        private static bool ContainsPoint(SpeedZone zone, Vector3 position)
        {
            var bounds = zone.Bounds;
            if (position.x < bounds.min.x || position.x > bounds.max.x ||
                position.z < bounds.min.z || position.z > bounds.max.z)
                return false;

            var offset = position - zone.Center;
            offset.y = 0f;
            var along = Vector3.Dot(offset, zone.Forward);
            var across = Vector3.Dot(offset, zone.Lateral);
            return Mathf.Abs(along) <= zone.HalfLengthM && Mathf.Abs(across) <= zone.HalfWidthM;
        }

        private static long CellKey(int cellX, int cellZ) =>
            ((long)cellX << 32) ^ (uint)cellZ;

        private static bool TryParseFloat(string value, CultureInfo inv, out float result) =>
            float.TryParse(value, NumberStyles.Float, inv, out result);
    }
}
