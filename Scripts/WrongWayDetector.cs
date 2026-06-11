using UnityEngine;

namespace BetterFines
{
    /// <summary>Wrong-way detection via offline road direction segments (no Gley waypoint scan).</summary>
    internal sealed class WrongWayDetector
    {
        private const float WrongWayHeadingDot = -0.55f;
        private const float SegmentCacheMoveThresholdM = 12f;
        private int _cachedSegmentId = -1;
        private Vector3 _cachedPosition;
        private bool _cachedWrongWay;

        internal bool TryIsDrivingWrongWay(
            Vector3 position,
            float headingDeg,
            float maxDistanceMeters,
            out int segmentId)
        {
            segmentId = -1;

            if (!RoadDirectionIndex.IsLoaded)
                return false;

            if (TryReuseCachedResult(position, maxDistanceMeters, out segmentId))
                return _cachedWrongWay;

            if (!RoadDirectionIndex.TryFindNearestSegment(
                    position,
                    maxDistanceMeters,
                    out var segment,
                    out _))
                return false;

            var heading = HeadingToForward(headingDeg);
            var wrongWay = Vector3.Dot(heading, segment.Forward) <= WrongWayHeadingDot;
            RememberResult(position, segment.SegmentId, wrongWay);

            if (!wrongWay)
                return false;

            segmentId = segment.SegmentId;
            return true;
        }

        internal void Invalidate()
        {
            _cachedSegmentId = -1;
        }

        private bool TryReuseCachedResult(Vector3 position, float maxDistanceMeters, out int segmentId)
        {
            segmentId = -1;
            if (_cachedSegmentId < 0)
                return false;

            var moved = position - _cachedPosition;
            moved.y = 0f;
            if (moved.sqrMagnitude > SegmentCacheMoveThresholdM * SegmentCacheMoveThresholdM)
                return false;

            if (!RoadDirectionIndex.TryFindNearestSegment(
                    position,
                    maxDistanceMeters,
                    out var segment,
                    out _) ||
                segment.SegmentId != _cachedSegmentId)
                return false;

            segmentId = _cachedSegmentId;
            return true;
        }

        private void RememberResult(Vector3 position, int segmentId, bool wrongWay)
        {
            _cachedPosition = position;
            _cachedSegmentId = segmentId;
            _cachedWrongWay = wrongWay;
        }

        private static Vector3 HeadingToForward(float headingDeg)
        {
            var rad = headingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }
    }
}
