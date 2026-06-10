using GleyTrafficSystem;
using GleyUrbanAssets;
using System.Collections.Generic;

namespace BetterFines
{
    /// <summary>Shared, load-once traffic data for BetterFines (stop index + waypoint lookup).</summary>
    internal static class TrafficDataStore
    {
        private static Waypoint[] _waypoints;
        private static Dictionary<int, Waypoint> _waypointByListIndex;
        private static CurrentSceneData _sceneData;
        private static readonly TrafficStopIndex _stops = new TrafficStopIndex();
        private static bool _loadCompleted;

        internal static TrafficStopIndex Stops => _stops;

        internal static IReadOnlyDictionary<int, Waypoint> WaypointLookup => _waypointByListIndex;

        internal static bool LoadCompleted => _loadCompleted;

        internal static void Initialize(string modRootPath)
        {
            TrafficApproachZoneCsvExporter.Initialize(modRootPath);
            TrafficLightVisualIndex.Initialize(modRootPath);
        }

        /// <summary>One-shot load used by bootstrap and legacy callers after load has completed.</summary>
        internal static bool TryEnsureLoaded()
        {
            if (_loadCompleted)
                return _stops.IsBuilt;

            return TryLoadOnce();
        }

        /// <summary>Bakes traffic stops once; returns true when the index is ready.</summary>
        internal static bool TryLoadOnce()
        {
            if (_loadCompleted)
                return _stops.IsBuilt;

            try
            {
                var scene = CurrentSceneData.GetSceneInstance();
                if (scene == null)
                    return false;

                var array = scene.allWaypoints;
                if (array == null || array.Length == 0)
                    return false;

                _sceneData = scene;
                _waypoints = array;
                _waypointByListIndex = new Dictionary<int, Waypoint>(array.Length);
                for (var i = 0; i < array.Length; i++)
                {
                    var wp = array[i];
                    if (wp != null && wp.listIndex >= 0)
                        _waypointByListIndex[wp.listIndex] = wp;
                }

                _stops.Build(array, _waypointByListIndex);
                if (!_stops.IsBuilt)
                    return false;

                _loadCompleted = true;
                TrafficLightVisualIndex.TryLoadOnce(_stops);
                TrafficApproachZoneCsvExporter.TryExportOnce(_stops);
                ModLog.Info("Traffic data ready | stops=" + _stops.StopCount +
                            " | zones=" + _stops.ZoneCount +
                            " | visual_lights=" + TrafficLightVisualIndex.Count);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void FinalizeLoadAttempt()
        {
            if (_loadCompleted)
                return;

            _loadCompleted = true;
            ModLog.Warn("Traffic data load timed out before waypoints were ready.");
        }

        internal static Waypoint[] Waypoints => _waypoints;

        internal static void Invalidate()
        {
            _waypoints = null;
            _waypointByListIndex = null;
            _sceneData = null;
            _loadCompleted = false;
            _stops.Clear();
            TrafficLightVisualIndex.Invalidate();
            TrafficApproachZoneCsvExporter.Shutdown();
        }
    }
}
