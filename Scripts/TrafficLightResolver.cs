using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GleyTrafficSystem;
using UnityEngine;

namespace BetterFines
{
    internal enum TrafficApproachSignal
    {
        None,
        Red,
        Yellow
    }

    internal readonly struct TrafficApproachInfo
    {
        internal TrafficApproachInfo(
            TrafficApproachSignal signal,
            Vector3 stopLinePosition,
            Vector3 roadForward,
            Waypoint referenceWaypoint)
        {
            Signal = signal;
            StopLinePosition = stopLinePosition;
            RoadForward = roadForward;
            ReferenceWaypoint = referenceWaypoint;
        }

        internal TrafficApproachSignal Signal { get; }
        internal Vector3 StopLinePosition { get; }
        internal Vector3 RoadForward { get; }
        internal Waypoint ReferenceWaypoint { get; }
    }

    internal static class TrafficLightResolver
    {
        private readonly struct CachedApproachGeometry
        {
            internal CachedApproachGeometry(
                int intersectionEntryIndex,
                Vector3 stopLinePosition,
                Vector3 roadForward,
                Waypoint referenceWaypoint)
            {
                IntersectionEntryIndex = intersectionEntryIndex;
                StopLinePosition = stopLinePosition;
                RoadForward = roadForward;
                ReferenceWaypoint = referenceWaypoint;
            }

            internal int IntersectionEntryIndex { get; }
            internal Vector3 StopLinePosition { get; }
            internal Vector3 RoadForward { get; }
            internal Waypoint ReferenceWaypoint { get; }
        }

        private static readonly FieldInfo AssociatedIntersectionField =
            typeof(Waypoint).GetField("associatedIntersection",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo IntersectionStateField =
            typeof(TrafficLightsIntersection).GetField("intersectionState",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Dictionary<int, CachedApproachGeometry> GeometryCache =
            new Dictionary<int, CachedApproachGeometry>();

        internal static void ClearBakeCache() => GeometryCache.Clear();

        internal static bool TryBakeStop(
            Waypoint waypoint,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out TrafficStopIndex.BakedStop baked)
        {
            baked = default;

            if (waypoint == null || !waypoint.enter || waypoint.exit || !waypoint.stop)
                return false;

            if (!(AssociatedIntersectionField?.GetValue(waypoint) is TrafficLightsIntersection intersection))
                return false;

            if (intersection.stopWaypoints == null)
                return false;

            if (!TryGetBakeForward(waypoint, waypointLookup, out var bakeForward))
                return false;

            for (var i = 0; i < intersection.stopWaypoints.Count; i++)
            {
                var entry = intersection.stopWaypoints[i];
                if (entry?.roadWaypoints == null || !entry.roadWaypoints.Contains(waypoint.listIndex))
                    continue;

                if (!TryGetCachedGeometry(waypoint, i, entry, bakeForward, waypointLookup,
                        out var stopLine, out var roadForward, out var referenceWaypoint))
                    return false;

                baked = new TrafficStopIndex.BakedStop(
                    waypoint.listIndex,
                    stopLine,
                    roadForward,
                    intersection,
                    i,
                    referenceWaypoint);
                return referenceWaypoint != null;
            }

            return false;
        }

        internal static bool TryReadSignal(
            TrafficLightsIntersection intersection,
            int entryIndex,
            out TrafficApproachSignal signal)
        {
            signal = TrafficApproachSignal.None;
            if (intersection == null)
                return false;

            var state = IntersectionStateField?.GetValue(intersection) as TrafficLightsColor[];
            if (state == null || entryIndex < 0 || entryIndex >= state.Length)
                return false;

            signal = MapSignal(state[entryIndex]);
            return true;
        }

        internal static bool TryReadAlignedSignal(
            TrafficLightsIntersection intersection,
            Vector3 playerForward,
            int fallbackEntryIndex,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out TrafficApproachSignal signal,
            out int entryIndex)
        {
            signal = TrafficApproachSignal.None;
            entryIndex = fallbackEntryIndex;

            if (intersection == null)
                return false;

            var state = IntersectionStateField?.GetValue(intersection) as TrafficLightsColor[];
            if (state == null || state.Length == 0)
                return false;

            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < 0.01f)
                return false;
            playerForward.Normalize();

            var bestDot = 0.25f;
            var bestIndex = -1;

            if (intersection.stopWaypoints != null)
            {
                for (var i = 0; i < intersection.stopWaypoints.Count && i < state.Length; i++)
                {
                    var entry = intersection.stopWaypoints[i];
                    if (!TryGetEntryForward(entry, waypointLookup, out var entryForward))
                        continue;

                    var dot = Vector3.Dot(playerForward, entryForward);
                    if (dot < bestDot)
                        continue;

                    bestDot = dot;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0 &&
                fallbackEntryIndex >= 0 &&
                fallbackEntryIndex < state.Length)
                bestIndex = fallbackEntryIndex;

            if (bestIndex < 0)
                return false;

            entryIndex = bestIndex;
            signal = MapSignal(state[bestIndex]);
            return signal != TrafficApproachSignal.None;
        }

        private static bool TryGetEntryForward(
            object entry,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out Vector3 forward)
        {
            forward = Vector3.zero;
            var indices = GetRoadWaypointIndices(entry);
            if (indices == null || indices.Count == 0 || waypointLookup == null)
                return false;

            var sum = Vector3.zero;
            var count = 0;
            Waypoint previous = null;

            foreach (var index in indices)
            {
                if (!waypointLookup.TryGetValue(index, out var waypoint) || waypoint == null)
                    continue;

                if (previous != null)
                {
                    var step = waypoint.position - previous.position;
                    step.y = 0f;
                    if (step.sqrMagnitude > 0.01f)
                    {
                        sum += step.normalized;
                        count++;
                    }
                }

                previous = waypoint;
            }

            if (count == 0 && waypointLookup.TryGetValue(indices[0], out var first) && first != null)
                return TryGetBakeForward(first, waypointLookup, out forward);

            if (count == 0)
                return false;

            forward = sum.normalized;
            return forward.sqrMagnitude > 0.01f;
        }

        internal static bool TryGetApproachInfo(
            Waypoint waypoint,
            Vector3 travelForward,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out TrafficApproachInfo info)
        {
            info = default;

            if (waypoint == null || !waypoint.enter || waypoint.exit || !waypoint.stop)
                return false;

            if (!TryBakeStop(waypoint, waypointLookup, out var baked))
                return false;

            if (!TryReadSignal(baked.Intersection, baked.IntersectionEntryIndex, out var signal))
                return false;

            if (signal == TrafficApproachSignal.None)
                return false;

            info = new TrafficApproachInfo(
                signal,
                baked.StopLinePosition,
                baked.RoadForward,
                baked.ReferenceWaypoint);
            return true;
        }

        private static bool TryGetBakeForward(
            Waypoint waypoint,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out Vector3 forward)
        {
            forward = Vector3.zero;
            var sum = Vector3.zero;
            var count = 0;

            if (waypoint.neighbors != null)
            {
                foreach (var neighborIndex in waypoint.neighbors)
                {
                    if (waypointLookup == null ||
                        !waypointLookup.TryGetValue(neighborIndex, out var neighbor) ||
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
            }

            if (waypoint.prev != null)
            {
                foreach (var prevIndex in waypoint.prev)
                {
                    if (waypointLookup == null ||
                        !waypointLookup.TryGetValue(prevIndex, out var prev) ||
                        prev == null ||
                        prev.temporaryDisabled)
                        continue;

                    var step = waypoint.position - prev.position;
                    step.y = 0f;
                    if (step.sqrMagnitude < 0.01f)
                        continue;

                    sum += step.normalized;
                    count++;
                }
            }

            if (count == 0)
                return false;

            forward = sum.normalized;
            return forward.sqrMagnitude > 0.01f;
        }

        internal static bool IsViolationSignal(TrafficApproachSignal signal, bool includeOrange)
        {
            switch (signal)
            {
                case TrafficApproachSignal.Red:
                    return true;
                case TrafficApproachSignal.Yellow:
                    return includeOrange;
                default:
                    return false;
            }
        }

        private static TrafficApproachSignal MapSignal(TrafficLightsColor color)
        {
            switch (color)
            {
                case TrafficLightsColor.Yellow:
                    return TrafficApproachSignal.Yellow;
                case TrafficLightsColor.Red:
                    return TrafficApproachSignal.Red;
                default:
                    return TrafficApproachSignal.None;
            }
        }

        private static bool TryGetCachedGeometry(
            Waypoint waypoint,
            int intersectionEntryIndex,
            object entry,
            Vector3 travelForward,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out Vector3 stopLinePosition,
            out Vector3 roadForward,
            out Waypoint referenceWaypoint)
        {
            if (GeometryCache.TryGetValue(waypoint.listIndex, out var cached) &&
                cached.IntersectionEntryIndex == intersectionEntryIndex)
            {
                stopLinePosition = cached.StopLinePosition;
                roadForward = cached.RoadForward;
                referenceWaypoint = cached.ReferenceWaypoint;
                return referenceWaypoint != null;
            }

            if (!TryComputeStopLine(entry, travelForward, waypointLookup, out stopLinePosition, out roadForward,
                    out referenceWaypoint))
                return false;

            GeometryCache[waypoint.listIndex] = new CachedApproachGeometry(
                intersectionEntryIndex,
                stopLinePosition,
                roadForward,
                referenceWaypoint);
            return true;
        }

        private static bool TryComputeStopLine(
            object entry,
            Vector3 travelForward,
            IReadOnlyDictionary<int, Waypoint> waypointLookup,
            out Vector3 stopLinePosition,
            out Vector3 roadForward,
            out Waypoint referenceWaypoint)
        {
            stopLinePosition = default;
            roadForward = travelForward;
            referenceWaypoint = null;

            var roadWaypointIndices = GetRoadWaypointIndices(entry);
            if (roadWaypointIndices == null || roadWaypointIndices.Count == 0)
                return false;

            var anchorPosition = default(Vector3);
            var hasAnchor = false;
            var minAhead = float.MaxValue;

            foreach (var index in roadWaypointIndices)
            {
                if (waypointLookup == null || !waypointLookup.TryGetValue(index, out var wp) || wp == null)
                    continue;

                var ahead = Vector3.Dot(wp.position, travelForward);
                if (ahead >= minAhead)
                    continue;

                minAhead = ahead;
                anchorPosition = wp.position;
                hasAnchor = true;
            }

            if (!hasAnchor)
                return false;

            var bestAhead = float.MinValue;
            var secondBestAhead = float.MinValue;
            Vector3 furthestPosition = default;
            Vector3 secondFurthestPosition = default;
            var hasFurthest = false;

            foreach (var index in roadWaypointIndices)
            {
                if (waypointLookup == null || !waypointLookup.TryGetValue(index, out var wp) || wp == null)
                    continue;

                var ahead = Vector3.Dot(wp.position - anchorPosition, travelForward);
                if (ahead > bestAhead)
                {
                    secondBestAhead = bestAhead;
                    secondFurthestPosition = furthestPosition;
                    bestAhead = ahead;
                    furthestPosition = wp.position;
                    referenceWaypoint = wp;
                    hasFurthest = true;
                }
                else if (ahead > secondBestAhead)
                {
                    secondBestAhead = ahead;
                    secondFurthestPosition = wp.position;
                }
            }

            if (!hasFurthest)
                return false;

            var step = furthestPosition - secondFurthestPosition;
            step.y = 0f;
            if (step.sqrMagnitude > 0.25f && Vector3.Dot(step.normalized, travelForward) > 0.2f)
                roadForward = step.normalized;
            else
                roadForward = travelForward;

            stopLinePosition = furthestPosition;

            if (TryGetLightCentroid(entry, out var lightCentroid))
            {
                stopLinePosition = ProjectOntoRoadAxis(lightCentroid, furthestPosition, roadForward);
            }

            stopLinePosition.y = furthestPosition.y;
            return true;
        }

        private static IReadOnlyList<int> GetRoadWaypointIndices(object entry)
        {
            if (entry == null)
                return null;

            var property = entry.GetType().GetProperty("roadWaypoints",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(entry) is IReadOnlyList<int> fromProperty)
                return fromProperty;

            var field = entry.GetType().GetField("roadWaypoints",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(entry) as IReadOnlyList<int>;
        }

        private static bool TryGetLightCentroid(object entry, out Vector3 centroid)
        {
            centroid = default;
            var positions = new List<Vector3>();
            CollectLightPositions(entry, positions);

            if (positions.Count == 0)
                return false;

            var sum = Vector3.zero;
            for (var i = 0; i < positions.Count; i++)
                sum += positions[i];

            centroid = sum / positions.Count;
            centroid.y = 0f;
            return true;
        }

        private static void CollectLightPositions(object source, List<Vector3> positions)
        {
            if (source == null)
                return;

            var type = source.GetType();

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                CollectMemberValue(field.GetValue(source), field.Name, positions);

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;

                object value;
                try
                {
                    value = property.GetValue(source);
                }
                catch
                {
                    continue;
                }

                CollectMemberValue(value, property.Name, positions);
            }
        }

        private static void CollectMemberValue(object value, string memberName, List<Vector3> positions)
        {
            if (value == null || !IsLightMemberName(memberName))
                return;

            switch (value)
            {
                case GameObject gameObject when gameObject != null:
                    AddTransformPosition(gameObject.transform, positions);
                    break;
                case Component component when component != null:
                    AddTransformPosition(component.transform, positions);
                    break;
                case IEnumerable<GameObject> gameObjects:
                    foreach (var gameObject in gameObjects)
                        AddTransformPosition(gameObject != null ? gameObject.transform : null, positions);
                    break;
                case IEnumerable<Component> components:
                    foreach (var component in components)
                        AddTransformPosition(component != null ? component.transform : null, positions);
                    break;
            }
        }

        private static bool IsLightMemberName(string memberName)
        {
            var lower = memberName.ToLowerInvariant();
            return lower.Contains("light") ||
                   lower.Contains("red") ||
                   lower.Contains("yellow") ||
                   lower.Contains("green");
        }

        private static void AddTransformPosition(Transform transform, List<Vector3> positions)
        {
            if (transform == null)
                return;

            var position = transform.position;
            position.y = 0f;
            positions.Add(position);
        }

        private static Vector3 ProjectOntoRoadAxis(Vector3 point, Vector3 axisOrigin, Vector3 axisForward)
        {
            axisForward.y = 0f;
            if (axisForward.sqrMagnitude < 0.01f)
                return axisOrigin;

            axisForward.Normalize();
            var offset = point - axisOrigin;
            offset.y = 0f;
            return axisOrigin + axisForward * Vector3.Dot(offset, axisForward);
        }
    }
}
