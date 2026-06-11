using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GleyTrafficSystem;
using UnityEngine;

namespace BetterFines
{
    /// <summary>One-shot export of Gley waypoint speed limits for offline zone baking.</summary>
    internal static class RoadSpeedLimitCsvExporter
    {
        private const string CsvFileName = "road_speed_limits.csv";
        private const string DataFolderName = "Data";
        private const float DefaultLimitKmh = 45f;

        private static readonly Regex RoadLanePattern =
            new Regex(@"Road_(\d+)-Lane_(\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string _modRootPath;
        private static bool _exported;

        internal static void Initialize(string modRootPath)
        {
            _modRootPath = modRootPath;
            _exported = false;
        }

        internal static void Shutdown()
        {
            _modRootPath = null;
            _exported = false;
        }

        internal static void TryExportOnce(Waypoint[] waypoints)
        {
            if (!BetterFinesConfig.DumpRoadSpeedLimits)
                return;

            if (_exported || waypoints == null || waypoints.Length == 0)
                return;

            if (string.IsNullOrEmpty(_modRootPath))
                return;

            try
            {
                var folder = Path.Combine(_modRootPath, DataFolderName);
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, CsvFileName);
                var summary = BuildCsv(waypoints, out var csv);
                File.WriteAllText(path, csv, Encoding.UTF8);
                _exported = true;
                ModLog.Info(
                    "Exported road speed limits | waypoints=" + summary.WaypointCount +
                    " | active=" + summary.ActiveCount +
                    " | distinct_limits=" + summary.DistinctLimitCount +
                    " | path=" + DataFolderName + "/" + CsvFileName);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("Failed to export road speed limits: " + ex.Message);
            }
        }

        private readonly struct ExportSummary
        {
            internal ExportSummary(int waypointCount, int activeCount, int distinctLimitCount)
            {
                WaypointCount = waypointCount;
                ActiveCount = activeCount;
                DistinctLimitCount = distinctLimitCount;
            }

            internal int WaypointCount { get; }
            internal int ActiveCount { get; }
            internal int DistinctLimitCount { get; }
        }

        private static ExportSummary BuildCsv(Waypoint[] waypoints, out string csv)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(waypoints.Length * 96);
            sb.AppendLine(
                "list_index,name,road_id,lane,pos_x,pos_y,pos_z," +
                "max_speed_kmh,effective_limit_kmh,neighbors,prev," +
                "enter,exit,stop,temporary_disabled");

            var distinctLimits = new HashSet<int>();
            var activeCount = 0;

            for (var i = 0; i < waypoints.Length; i++)
            {
                var wp = waypoints[i];
                if (wp == null)
                    continue;

                if (!wp.temporaryDisabled)
                    activeCount++;

                var rawLimit = wp.maxSpeed;
                var effectiveLimit = rawLimit > 5f ? rawLimit : DefaultLimitKmh;
                distinctLimits.Add(Mathf.RoundToInt(effectiveLimit));

                TryParseRoadLane(wp.name, out var roadId, out var lane);

                sb.Append(wp.listIndex).Append(',');
                AppendCsv(sb, wp.name);
                sb.Append(',');
                sb.Append(roadId).Append(',');
                sb.Append(lane).Append(',');
                sb.Append(wp.position.x.ToString(inv)).Append(',');
                sb.Append(wp.position.y.ToString(inv)).Append(',');
                sb.Append(wp.position.z.ToString(inv)).Append(',');
                sb.Append(rawLimit.ToString(inv)).Append(',');
                sb.Append(effectiveLimit.ToString(inv)).Append(',');
                AppendIndexList(sb, wp.neighbors);
                sb.Append(',');
                AppendIndexList(sb, wp.prev);
                sb.Append(',');
                sb.Append(wp.enter ? "1" : "0").Append(',');
                sb.Append(wp.exit ? "1" : "0").Append(',');
                sb.Append(wp.stop ? "1" : "0").Append(',');
                sb.Append(wp.temporaryDisabled ? "1" : "0");
                sb.AppendLine();
            }

            csv = sb.ToString();
            return new ExportSummary(waypoints.Length, activeCount, distinctLimits.Count);
        }

        private static void TryParseRoadLane(string name, out int roadId, out int lane)
        {
            roadId = -1;
            lane = -1;
            if (string.IsNullOrEmpty(name))
                return;

            var match = RoadLanePattern.Match(name);
            if (!match.Success)
                return;

            int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out roadId);
            int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lane);
        }

        private static void AppendIndexList(StringBuilder sb, List<int> indices)
        {
            if (indices == null || indices.Count == 0)
                return;

            for (var i = 0; i < indices.Count; i++)
            {
                if (i > 0)
                    sb.Append(';');
                sb.Append(indices[i]);
            }
        }

        private static void AppendCsv(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                sb.Append(value);
                return;
            }

            sb.Append('"');
            sb.Append(value.Replace("\"", "\"\""));
            sb.Append('"');
        }
    }
}
