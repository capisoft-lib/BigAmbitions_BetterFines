using System.Collections.Generic;
using GleyTrafficSystem;
using UnityEngine;

namespace BetterFines
{
    /// <summary>Resolves per-road speed limits and red-light violations from Gley traffic waypoints.</summary>
    internal sealed class RoadSpeedService
    {
        private const float DefaultSpeedKmh = 45f;
        private const float WrongWayHeadingDot = -0.55f;
        private const float WaypointCacheMoveThresholdM = 12f;
        private const float FullRescanIntervalSec = 2f;
        private const float RoadAlignMaxM = 25f;
        private const float MinHeadingDot = 0.55f;

        private Waypoint _cachedNearestWaypoint;
        private Vector3 _cachedNearestPosition;
        private float _lastFullWaypointScanAt = -999f;

        internal int TrafficStopCount => TrafficDataStore.Stops.StopCount;

        internal bool TryGetRoadSpeedLimit(Vector3 position, float maxDistanceMeters, out float limitKmh)
        {
            limitKmh = DefaultSpeedKmh;

            if (!TrafficDataStore.TryEnsureLoaded())
                return false;

            if (!TryFindNearestWaypoint(position, maxDistanceMeters, out var wp) || wp == null)
                return false;

            limitKmh = wp.maxSpeed > 5f ? wp.maxSpeed : DefaultSpeedKmh;
            return true;
        }

        internal bool TryGetNearestWaypoint(Vector3 position, float maxDistanceMeters, out Waypoint waypoint)
        {
            return TryFindNearestWaypoint(position, maxDistanceMeters, out waypoint);
        }

        internal bool TryIsDrivingWrongWay(
            Vector3 position,
            float headingDeg,
            float maxDistanceMeters,
            out Waypoint waypoint)
        {
            waypoint = null;

            if (!TrafficDataStore.TryEnsureLoaded())
                return false;

            if (!TryFindNearestWaypoint(position, maxDistanceMeters, out var roadWaypoint) ||
                roadWaypoint == null)
                return false;

            if (!TryGetLegalRoadDirection(roadWaypoint, out var legalForward))
                return false;

            var forward = HeadingToForward(headingDeg);
            if (Vector3.Dot(forward, legalForward) > WrongWayHeadingDot)
                return false;

            waypoint = roadWaypoint;
            return true;
        }

        internal bool TryGetTrafficViolationApproach(
            Vector3 approachPosition,
            Vector3 frontPosition,
            float headingDeg,
            float maxDistanceMeters,
            bool includeOrange,
            float vehicleLengthM,
            float now,
            out Waypoint waypoint,
            out TrafficApproachSignal signal)
        {
            waypoint = null;
            signal = TrafficApproachSignal.None;

            if (!TrafficDataStore.TryEnsureLoaded() || !TrafficDataStore.Stops.IsBuilt)
            {
                ModLog.DebugRedLight("index not ready | built=" + TrafficDataStore.Stops.IsBuilt +
                                     " | stops=" + TrafficDataStore.Stops.StopCount);
                return false;
            }

            var forward = HeadingToForward(headingDeg);
            var searchRadius = maxDistanceMeters;

            if (!TrafficDataStore.Stops.TryFindViolationNear(
                    approachPosition,
                    frontPosition,
                    forward,
                    searchRadius,
                    includeOrange,
                    vehicleLengthM,
                    now,
                    out waypoint,
                    out signal))
                return false;

            ModLog.DebugRedLight("violation candidate wp=" + waypoint.listIndex + " signal=" + signal);
            return waypoint != null;
        }

        internal void Invalidate()
        {
            _cachedNearestWaypoint = null;
            _lastFullWaypointScanAt = -999f;
        }

        private static Vector3 HeadingToForward(float headingDeg)
        {
            var rad = headingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        private bool TryFindNearestAligned(Vector3 position, Vector3 forward, float maxDistanceMeters, out Waypoint waypoint)
        {
            if (TryReuseAlignedWaypoint(position, forward, maxDistanceMeters, out waypoint))
                return true;

            if (!TryScanNearestAligned(position, forward, maxDistanceMeters, out waypoint))
                return false;

            RememberNearestWaypoint(position, waypoint);
            return true;
        }

        private bool TryReuseAlignedWaypoint(
            Vector3 position,
            Vector3 forward,
            float maxDistanceMeters,
            out Waypoint waypoint)
        {
            waypoint = null;
            var cached = _cachedNearestWaypoint;
            if (cached == null || cached.temporaryDisabled)
                return false;

            var moved = position - _cachedNearestPosition;
            moved.y = 0f;
            if (moved.sqrMagnitude > WaypointCacheMoveThresholdM * WaypointCacheMoveThresholdM)
                return false;

            var delta = cached.position - position;
            delta.y = 0f;
            if (delta.sqrMagnitude > maxDistanceMeters * maxDistanceMeters)
                return false;

            if (!TryGetWaypointTravelDirection(cached, forward, out _))
                return false;

            waypoint = cached;
            return true;
        }

        private bool TryGetWaypointTravelDirection(Waypoint waypoint, Vector3 forward, out Vector3 direction)
        {
            direction = Vector3.zero;
            var lookup = TrafficDataStore.WaypointLookup;
            if (lookup == null)
                return false;

            var bestDot = MinHeadingDot;
            var found = false;

            if (waypoint.neighbors != null)
            {
                foreach (var neighborIndex in waypoint.neighbors)
                {
                    if (!lookup.TryGetValue(neighborIndex, out var neighbor) ||
                        neighbor == null ||
                        neighbor.temporaryDisabled)
                        continue;

                    var step = neighbor.position - waypoint.position;
                    step.y = 0f;
                    if (step.sqrMagnitude < 0.01f)
                        continue;

                    step.Normalize();
                    var dot = Vector3.Dot(forward, step);
                    if (dot < bestDot)
                        continue;

                    bestDot = dot;
                    direction = step;
                    found = true;
                }
            }

            if (waypoint.prev != null)
            {
                foreach (var prevIndex in waypoint.prev)
                {
                    if (!lookup.TryGetValue(prevIndex, out var prev) ||
                        prev == null ||
                        prev.temporaryDisabled)
                        continue;

                    var step = waypoint.position - prev.position;
                    step.y = 0f;
                    if (step.sqrMagnitude < 0.01f)
                        continue;

                    step.Normalize();
                    var dot = Vector3.Dot(forward, step);
                    if (dot < bestDot)
                        continue;

                    bestDot = dot;
                    direction = step;
                    found = true;
                }
            }

            return found;
        }

        private bool TryFindNearestWaypoint(Vector3 worldPos, float maxDistance, out Waypoint waypoint)
        {
            if (TryReuseCachedWaypoint(worldPos, maxDistance, out waypoint))
                return true;

            if (!TryScanNearestWaypoint(worldPos, maxDistance, out waypoint))
                return false;

            RememberNearestWaypoint(worldPos, waypoint);
            return true;
        }

        private bool TryReuseCachedWaypoint(Vector3 position, float maxDistanceMeters, out Waypoint waypoint)
        {
            waypoint = null;
            var cached = _cachedNearestWaypoint;
            if (cached == null || cached.temporaryDisabled)
                return false;

            var moved = position - _cachedNearestPosition;
            moved.y = 0f;
            if (moved.sqrMagnitude > WaypointCacheMoveThresholdM * WaypointCacheMoveThresholdM)
                return false;

            var delta = cached.position - position;
            delta.y = 0f;
            if (delta.sqrMagnitude > maxDistanceMeters * maxDistanceMeters)
                return false;

            waypoint = cached;
            return true;
        }

        private void RememberNearestWaypoint(Vector3 position, Waypoint waypoint)
        {
            _cachedNearestWaypoint = waypoint;
            _cachedNearestPosition = position;
            _lastFullWaypointScanAt = Time.unscaledTime;
        }

        private bool ShouldForceFullWaypointScan(Vector3 position)
        {
            if (_cachedNearestWaypoint == null)
                return true;

            if (Time.unscaledTime - _lastFullWaypointScanAt >= FullRescanIntervalSec)
                return true;

            var moved = position - _cachedNearestPosition;
            moved.y = 0f;
            return moved.sqrMagnitude > WaypointCacheMoveThresholdM * WaypointCacheMoveThresholdM;
        }

        private bool TryScanNearestWaypoint(Vector3 worldPos, float maxDistance, out Waypoint waypoint)
        {
            waypoint = null;
            var waypoints = TrafficDataStore.Waypoints;
            if (waypoints == null || waypoints.Length == 0)
                return false;

            if (!ShouldForceFullWaypointScan(worldPos) &&
                TryReuseCachedWaypoint(worldPos, maxDistance, out waypoint))
                return true;

            var maxSq = maxDistance * maxDistance;
            var bestSq = float.MaxValue;
            Waypoint best = null;

            for (var i = 0; i < waypoints.Length; i++)
            {
                var wp = waypoints[i];
                if (wp == null || wp.temporaryDisabled)
                    continue;

                var delta = wp.position - worldPos;
                delta.y = 0f;
                var sq = delta.sqrMagnitude;
                if (sq > maxSq || sq >= bestSq)
                    continue;

                bestSq = sq;
                best = wp;
            }

            waypoint = best;
            return best != null;
        }

        private bool TryScanNearestAligned(Vector3 position, Vector3 forward, float maxDistanceMeters, out Waypoint waypoint)
        {
            waypoint = null;
            var waypoints = TrafficDataStore.Waypoints;
            if (waypoints == null || waypoints.Length == 0)
                return false;

            if (!ShouldForceFullWaypointScan(position) &&
                TryReuseAlignedWaypoint(position, forward, maxDistanceMeters, out waypoint))
                return true;

            var maxSq = maxDistanceMeters * maxDistanceMeters;
            var bestSq = float.MaxValue;
            Waypoint best = null;

            for (var i = 0; i < waypoints.Length; i++)
            {
                var wp = waypoints[i];
                if (wp == null || wp.temporaryDisabled)
                    continue;

                var delta = wp.position - position;
                delta.y = 0f;
                var sq = delta.sqrMagnitude;
                if (sq > maxSq || sq >= bestSq)
                    continue;

                if (!TryGetWaypointTravelDirection(wp, forward, out _))
                    continue;

                bestSq = sq;
                best = wp;
            }

            waypoint = best;
            return best != null;
        }

        private bool TryGetLegalRoadDirection(Waypoint waypoint, out Vector3 direction)
        {
            direction = Vector3.zero;
            var lookup = TrafficDataStore.WaypointLookup;
            if (waypoint?.neighbors == null || waypoint.neighbors.Count == 0 || lookup == null)
                return false;

            var sum = Vector3.zero;
            var count = 0;

            foreach (var neighborIndex in waypoint.neighbors)
            {
                if (!lookup.TryGetValue(neighborIndex, out var neighbor) ||
                    neighbor == null ||
                    neighbor.temporaryDisabled)
                    continue;

                var step = neighbor.position - waypoint.position;
                step.y = 0f;
                if (step.sqrMagnitude < 0.01f)
                    continue;

                sum += step.normalized;
                count++;
            }

            if (count == 0)
                return false;

            direction = sum.normalized;
            return true;
        }
    }
}
