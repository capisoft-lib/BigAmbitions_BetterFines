using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace BetterFines
{
    internal static class TrafficApproachZoneCsvExporter
    {
        private const string CsvFileName = "traffic_approach_zones.csv";
        private const string DataFolderName = "Data";

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

        internal static void TryExportOnce(TrafficStopIndex stops)
        {
            if (!BetterFinesConfig.DumpTrafficApproachZones)
                return;

            if (_exported || stops == null || !stops.IsBuilt || stops.ZoneCount == 0)
                return;

            if (string.IsNullOrEmpty(_modRootPath))
                return;

            try
            {
                var folder = Path.Combine(_modRootPath, DataFolderName);
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, CsvFileName);
                File.WriteAllText(path, BuildCsv(stops), Encoding.UTF8);
                _exported = true;
                ModLog.Info("Exported traffic approach zones | count=" + stops.ZoneCount +
                            " | path=" + DataFolderName + "/" + CsvFileName);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn("Failed to export traffic approach zones: " + ex.Message);
            }
        }

        private static string BuildCsv(TrafficStopIndex stops)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(4096);
            sb.AppendLine(
                "waypoint_list_index," +
                "stop_x,stop_y,stop_z," +
                "forward_x,forward_y,forward_z," +
                "approach_ahead_m,past_stop_m,half_width_m," +
                "near_left_x,near_left_z,near_right_x,near_right_z," +
                "far_right_x,far_right_z,far_left_x,far_left_z," +
                "bounds_center_x,bounds_center_y,bounds_center_z," +
                "bounds_size_x,bounds_size_y,bounds_size_z");

            foreach (var zone in stops.Zones)
            {
                sb.Append(zone.WaypointListIndex).Append(',');
                AppendVec(sb, zone.StopLinePosition, inv);
                sb.Append(',');
                AppendVec(sb, zone.RoadForward, inv);
                sb.Append(',');
                sb.Append(TrafficApproachZone.ApproachAheadM.ToString(inv)).Append(',');
                sb.Append(TrafficApproachZone.PastStopM.ToString(inv)).Append(',');
                sb.Append(TrafficApproachZone.HalfWidthM.ToString(inv)).Append(',');
                AppendPair(sb, zone.CornerNearLeft, inv);
                sb.Append(',');
                AppendPair(sb, zone.CornerNearRight, inv);
                sb.Append(',');
                AppendPair(sb, zone.CornerFarRight, inv);
                sb.Append(',');
                AppendPair(sb, zone.CornerFarLeft, inv);
                sb.Append(',');
                AppendVec(sb, zone.WorldBounds.center, inv);
                sb.Append(',');
                AppendVec(sb, zone.WorldBounds.size, inv);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void AppendVec(StringBuilder sb, Vector3 v, CultureInfo inv)
        {
            sb.Append(v.x.ToString(inv)).Append(',');
            sb.Append(v.y.ToString(inv)).Append(',');
            sb.Append(v.z.ToString(inv));
        }

        private static void AppendPair(StringBuilder sb, Vector3 v, CultureInfo inv)
        {
            sb.Append(v.x.ToString(inv)).Append(',');
            sb.Append(v.z.ToString(inv));
        }
    }
}
