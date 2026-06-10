using System.Collections.Generic;

using GleyTrafficSystem;

using UnityEngine;



namespace BetterFines

{

    /// <summary>

    /// Load-time bake of stop-line geometry. Per-tick: nearby stop scan + red approach latch.

    /// Signal color is read live; latch preserves red seen before crossing if poll misses the moment.

    /// </summary>

    internal sealed class TrafficStopIndex

    {

        internal readonly struct BakedStop

        {

            internal BakedStop(

                int waypointListIndex,

                Vector3 stopLinePosition,

                Vector3 roadForward,

                TrafficLightsIntersection intersection,

                int intersectionEntryIndex,

                Waypoint referenceWaypoint)

            {

                WaypointListIndex = waypointListIndex;

                StopLinePosition = stopLinePosition;

                RoadForward = roadForward;

                Intersection = intersection;

                IntersectionEntryIndex = intersectionEntryIndex;

                ReferenceWaypoint = referenceWaypoint;

            }



            internal int WaypointListIndex { get; }

            internal Vector3 StopLinePosition { get; }

            internal Vector3 RoadForward { get; }

            internal TrafficLightsIntersection Intersection { get; }

            internal int IntersectionEntryIndex { get; }

            internal Waypoint ReferenceWaypoint { get; }

        }



        private const float MinHeadingDot = 0.3f;

        private const float MinApproachHeadingDot = 0.25f;

        private const float ApproachLatchHoldSec = 10f;



        private readonly Dictionary<int, BakedStop> _stopsByListIndex = new Dictionary<int, BakedStop>();

        private TrafficApproachZone[] _zones = System.Array.Empty<TrafficApproachZone>();

        private bool _built;



        private int _latchedWaypointIndex = -1;

        private TrafficLightsIntersection _latchedIntersection;

        private float _latchedUntil;



        internal int StopCount => _stopsByListIndex.Count;

        internal int ZoneCount => _zones.Length;

        internal TrafficApproachZone[] Zones => _zones;

        internal IEnumerable<BakedStop> BakedStops => _stopsByListIndex.Values;

        internal bool IsBuilt => _built;

        internal bool TryGetBakedStop(int waypointListIndex, out BakedStop baked) =>
            _stopsByListIndex.TryGetValue(waypointListIndex, out baked);



        internal void Clear()

        {

            _stopsByListIndex.Clear();

            _zones = System.Array.Empty<TrafficApproachZone>();

            _built = false;

            _latchedWaypointIndex = -1;

            _latchedIntersection = null;

            _latchedUntil = -1f;

            TrafficLightResolver.ClearBakeCache();

        }



        internal void Build(Waypoint[] waypoints, IReadOnlyDictionary<int, Waypoint> lookup)

        {

            Clear();

            if (waypoints == null || waypoints.Length == 0)

                return;



            for (var i = 0; i < waypoints.Length; i++)

            {

                var wp = waypoints[i];

                if (wp == null || wp.temporaryDisabled)

                    continue;



                if (!TrafficLightResolver.TryBakeStop(wp, lookup, out var baked))

                    continue;



                _stopsByListIndex[baked.WaypointListIndex] = baked;

            }



            if (_stopsByListIndex.Count == 0)
            {
                _built = false;
                ModLog.DebugRedLight("Traffic stop index not ready yet (0 stops baked).");
                return;
            }

            _zones = new TrafficApproachZone[_stopsByListIndex.Count];
            var zoneIndex = 0;
            foreach (var baked in _stopsByListIndex.Values)
                _zones[zoneIndex++] = TrafficApproachZone.FromBakedStop(baked);

            _built = true;
            ModLog.Info("Traffic stop index | stops=" + _stopsByListIndex.Count + " | zones=" + _zones.Length);
        }



        internal bool TryFindViolationNear(

            Vector3 approachPosition,

            Vector3 frontPosition,

            Vector3 forward,

            float searchRadiusM,

            bool includeOrange,

            float vehicleLengthM,

            float now,

            out Waypoint waypoint,

            out TrafficApproachSignal signal)

        {

            waypoint = null;

            signal = TrafficApproachSignal.None;



            if (!_built)

                return false;



            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)

                return false;

            forward.Normalize();



            var maxSq = searchRadiusM * searchRadiusM;

            BakedStop? bestStop = null;

            var bestLongitudinal = float.MaxValue;

            var bestSignal = TrafficApproachSignal.None;



            if (_latchedUntil > 0f && now >= _latchedUntil)
                ClearApproachLatch();

            var aligned = 0;

            var redAhead = 0;

            var crossedRed = 0;

            var debugClosestAhead = float.MaxValue;

            var debugClosestSignal = TrafficApproachSignal.None;

            var debugClosestWp = -1;



            foreach (var baked in _stopsByListIndex.Values)

            {

                var toApproach = baked.StopLinePosition - approachPosition;

                toApproach.y = 0f;

                if (toApproach.sqrMagnitude > maxSq)

                    continue;



                var roadForward = baked.RoadForward;

                roadForward.y = 0f;

                if (roadForward.sqrMagnitude < 0.01f)

                    continue;

                roadForward.Normalize();



                var approachDistance = toApproach.magnitude;

                var alignsRoad = Vector3.Dot(forward, roadForward) >= MinHeadingDot;

                var alignsApproach = approachDistance > 0.5f &&

                                     Vector3.Dot(forward, toApproach / approachDistance) >= MinApproachHeadingDot;

                if (!alignsRoad && !alignsApproach)

                    continue;



                aligned++;



                var lookup = TrafficDataStore.WaypointLookup;

                if (!TrafficLightResolver.TryReadAlignedSignal(

                        baked.Intersection,

                        forward,

                        baked.IntersectionEntryIndex,

                        lookup,

                        out var liveSignal,

                        out _))

                    continue;



                var ahead = GetLongitudinalDistance(approachPosition, baked.StopLinePosition, roadForward);

                if (ahead < debugClosestAhead)

                {

                    debugClosestAhead = ahead;

                    debugClosestSignal = liveSignal;

                    debugClosestWp = baked.WaypointListIndex;

                }



                UpdateApproachLatch(baked, liveSignal, approachPosition, roadForward, now, includeOrange);



                if (TrafficLightResolver.IsViolationSignal(liveSignal, includeOrange) && IsInApproachZone(ahead))

                    redAhead++;



                if (!QualifiesForFine(baked, liveSignal, frontPosition, forward, vehicleLengthM, includeOrange, now))

                    continue;



                crossedRed++;

                var longitudinal = GetLongitudinalDistance(frontPosition, baked.StopLinePosition, roadForward);

                if (longitudinal >= bestLongitudinal)

                    continue;



                bestLongitudinal = longitudinal;

                bestStop = baked;

                bestSignal = TrafficLightResolver.IsViolationSignal(liveSignal, includeOrange)

                    ? liveSignal

                    : TrafficApproachSignal.Red;

            }



            if (!bestStop.HasValue)

            {

                ModLog.DebugRedLight(

                    "no violation | aligned=" + aligned +

                    " red_ahead=" + redAhead +

                    " crossed=" + crossedRed +

                    " closest=" + debugClosestSignal +

                    " ahead_m=" + (debugClosestAhead < float.MaxValue ? debugClosestAhead.ToString("0") : "?") +

                    " wp=" + debugClosestWp +

                    " latched_wp=" + _latchedWaypointIndex +

                    " latch_left=" + Mathf.Max(0f, _latchedUntil - now).ToString("0.0") + "s");

                return false;

            }



            var chosen = bestStop.Value;

            waypoint = chosen.ReferenceWaypoint;

            signal = bestSignal;

            return waypoint != null;

        }



        private void UpdateApproachLatch(

            BakedStop baked,

            TrafficApproachSignal signal,

            Vector3 playerPosition,

            Vector3 roadForward,

            float now,

            bool includeOrange)

        {

            if (!TrafficLightResolver.IsViolationSignal(signal, includeOrange))

                return;



            var longitudinal = GetLongitudinalDistance(playerPosition, baked.StopLinePosition, roadForward);

            if (!IsInApproachZone(longitudinal))

                return;



            _latchedWaypointIndex = baked.WaypointListIndex;

            _latchedIntersection = baked.Intersection;

            _latchedUntil = now + ApproachLatchHoldSec;

        }



        private bool QualifiesForFine(

            BakedStop baked,

            TrafficApproachSignal liveSignal,

            Vector3 playerPosition,

            Vector3 forward,

            float vehicleLengthM,

            bool includeOrange,

            float now)

        {

            if (!HasCrossedStopLine(playerPosition, forward, baked, vehicleLengthM))

                return false;



            if (TrafficLightResolver.IsViolationSignal(liveSignal, includeOrange))

                return true;



            if (now >= _latchedUntil)
                return false;

            return ReferenceEquals(_latchedIntersection, baked.Intersection) ||

                   _latchedWaypointIndex == baked.WaypointListIndex;

        }



        internal void ClearApproachLatch()

        {

            _latchedWaypointIndex = -1;

            _latchedIntersection = null;

            _latchedUntil = -1f;

        }



        private static bool HasCrossedStopLine(

            Vector3 frontPosition,

            Vector3 forward,

            BakedStop stop,

            float vehicleLengthM)

        {

            var roadForward = stop.RoadForward;

            roadForward.y = 0f;

            if (roadForward.sqrMagnitude < 0.01f)

                return false;



            roadForward.Normalize();



            var toStop = stop.StopLinePosition - frontPosition;

            toStop.y = 0f;



            var longitudinal = Vector3.Dot(toStop, roadForward);

            var requiredPastM = VehicleGeometry.GetRequiredPastStopLineM(vehicleLengthM);

            if (longitudinal > -requiredPastM)

                return false;



            var lateralAxis = new Vector3(-roadForward.z, 0f, roadForward.x);

            if (Mathf.Abs(Vector3.Dot(toStop, lateralAxis)) > TrafficApproachZone.HalfWidthM)

                return false;



            return Vector3.Dot(forward, roadForward) >= MinHeadingDot;

        }



        private static bool IsInApproachZone(float longitudinal)

        {

            return longitudinal > -TrafficApproachZone.PastStopM &&
                   longitudinal <= TrafficApproachZone.ApproachAheadM;

        }



        private static float GetLongitudinalDistance(

            Vector3 playerPosition,

            Vector3 stopLinePosition,

            Vector3 roadForward)

        {

            roadForward.y = 0f;

            if (roadForward.sqrMagnitude < 0.01f)

                return float.MaxValue;



            roadForward.Normalize();

            var toStop = stopLinePosition - playerPosition;

            toStop.y = 0f;

            return Vector3.Dot(toStop, roadForward);

        }

    }

}


