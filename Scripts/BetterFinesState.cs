using BaPlayerLocation.Subscriber;
using UnityEngine;

namespace BetterFines
{
    internal static class BetterFinesState
    {
        internal static PlayerLocationSnapshot LastSnapshot { get; private set; }
        internal static bool HasSnapshot { get; private set; }

        internal static void ApplySnapshot(PlayerLocationSnapshot snapshot)
        {
            LastSnapshot = snapshot;
            HasSnapshot = snapshot.IsAvailable;
        }

        internal static void Reset()
        {
            LastSnapshot = default;
            HasSnapshot = false;
        }

        internal static bool IsDrivingCar()
        {
            return HasSnapshot && LastSnapshot.MovementKind == MovementKind.Car;
        }

        internal static float SpeedKmh => HasSnapshot ? Mathf.Max(0f, LastSnapshot.SpeedMps * 3.6f) : 0f;
    }
}
