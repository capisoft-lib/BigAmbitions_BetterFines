using UnityEngine;

namespace BetterFines
{
    /// <summary>Axis-aligned approach rectangle in front of a traffic stop (XZ plane).</summary>
    internal readonly struct TrafficApproachZone
    {
        internal const float ApproachAheadM = 45f;
        internal const float PastStopM = 20f;
        internal const float HalfWidthM = 12f;
        internal const float GroundLiftM = 0.35f;

        internal TrafficApproachZone(
            int waypointListIndex,
            Vector3 stopLinePosition,
            Vector3 roadForward,
            Vector3 cornerNearLeft,
            Vector3 cornerNearRight,
            Vector3 cornerFarRight,
            Vector3 cornerFarLeft,
            Bounds worldBounds)
        {
            WaypointListIndex = waypointListIndex;
            StopLinePosition = stopLinePosition;
            RoadForward = roadForward;
            CornerNearLeft = cornerNearLeft;
            CornerNearRight = cornerNearRight;
            CornerFarRight = cornerFarRight;
            CornerFarLeft = cornerFarLeft;
            WorldBounds = worldBounds;
        }

        internal int WaypointListIndex { get; }
        internal Vector3 StopLinePosition { get; }
        internal Vector3 RoadForward { get; }
        internal Vector3 CornerNearLeft { get; }
        internal Vector3 CornerNearRight { get; }
        internal Vector3 CornerFarRight { get; }
        internal Vector3 CornerFarLeft { get; }
        internal Bounds WorldBounds { get; }

        internal static TrafficApproachZone FromBakedStop(TrafficStopIndex.BakedStop stop)
        {
            var roadForward = stop.RoadForward;
            roadForward.y = 0f;
            if (roadForward.sqrMagnitude < 0.01f)
                roadForward = Vector3.forward;
            roadForward.Normalize();

            var lateral = new Vector3(-roadForward.z, 0f, roadForward.x);
            var groundY = stop.StopLinePosition.y + GroundLiftM;
            var stopOnGround = new Vector3(stop.StopLinePosition.x, groundY, stop.StopLinePosition.z);

            var nearEdge = stopOnGround - roadForward * ApproachAheadM;
            var farEdge = stopOnGround + roadForward * PastStopM;

            var nearLeft = nearEdge - lateral * HalfWidthM;
            var nearRight = nearEdge + lateral * HalfWidthM;
            var farRight = farEdge + lateral * HalfWidthM;
            var farLeft = farEdge - lateral * HalfWidthM;

            var bounds = new Bounds(stopOnGround, Vector3.zero);
            Encapsulate(ref bounds, nearLeft);
            Encapsulate(ref bounds, nearRight);
            Encapsulate(ref bounds, farRight);
            Encapsulate(ref bounds, farLeft);
            bounds.Expand(new Vector3(0.5f, 4f, 0.5f));

            return new TrafficApproachZone(
                stop.WaypointListIndex,
                stopOnGround,
                roadForward,
                nearLeft,
                nearRight,
                farRight,
                farLeft,
                bounds);
        }

        private static void Encapsulate(ref Bounds bounds, Vector3 point)
        {
            if (bounds.size.sqrMagnitude < 0.001f)
                bounds = new Bounds(point, Vector3.zero);
            else
                bounds.Encapsulate(point);
        }
    }
}
