using UnityEngine;

namespace BetterFines
{
    /// <summary>Stateful 50/80 km/h limit from highway zone enter/exit.</summary>
    internal sealed class SpeedLimitTracker
    {
        private const float HighwayLimitKmh = 80f;

        private bool _insideHighwayZone;
        private float _activeLimitKmh = SpeedZone80Index.DefaultLimit;

        internal float ActiveLimitKmh => _activeLimitKmh;
        internal bool InsideHighwayZone => _insideHighwayZone;

        internal void Reset()
        {
            _insideHighwayZone = false;
            _activeLimitKmh = SpeedZone80Index.DefaultLimit;
        }

        internal void UpdateForPosition(Vector3 position)
        {
            var inside = SpeedZone80Index.ContainsHighwayZone(position);
            if (inside == _insideHighwayZone)
                return;

            _insideHighwayZone = inside;
            _activeLimitKmh = inside ? HighwayLimitKmh : SpeedZone80Index.DefaultLimit;
        }
    }
}
