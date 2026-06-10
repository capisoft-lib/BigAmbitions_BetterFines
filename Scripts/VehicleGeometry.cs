using System.Reflection;
using UnityEngine;

namespace BetterFines
{
    internal static class VehicleGeometry
    {
        private const float DefaultLengthM = 4.5f;
        private const float MinLengthM = 2.5f;
        private const float MaxLengthM = 14f;
        private const float VehicleCrossFraction = 1f / 3f;

        private static int _cachedVehicleId;
        private static float _cachedLengthM = DefaultLengthM;

        internal static float CrossFraction => VehicleCrossFraction;

        internal static void ClearCache()
        {
            _cachedVehicleId = 0;
            _cachedLengthM = DefaultLengthM;
        }

        internal static float GetPlayerVehicleLengthM()
        {
            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle == null)
                    return DefaultLengthM;

                var vehicleId = vehicle.GetInstanceID();
                if (vehicleId == _cachedVehicleId)
                    return _cachedLengthM;

                var length = DefaultLengthM;
                if (TryGetLengthFromPoints(vehicle, out var fromPoints))
                    length = ClampLength(fromPoints);
                else if (TryGetLengthFromBounds(vehicle, out var fromBounds))
                    length = ClampLength(fromBounds);

                _cachedVehicleId = vehicleId;
                _cachedLengthM = length;
                return length;
            }
            catch
            {
                // ignore
            }

            return DefaultLengthM;
        }

        internal static float GetRequiredPastStopLineM(float vehicleLengthM)
        {
            return Mathf.Max(0.35f, vehicleLengthM * VehicleCrossFraction);
        }

        internal static bool TryGetPlayerFrontPosition(out Vector3 front)
        {
            front = default;
            try
            {
                var vehicle = GameManager.Instance?.selectedVehicle;
                if (vehicle == null)
                    return false;

                front = vehicle.FrontPoint;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float ClampLength(float length)
        {
            return Mathf.Clamp(length, MinLengthM, MaxLengthM);
        }

        private static bool TryGetLengthFromPoints(VehicleController vehicle, out float length)
        {
            length = 0f;
            var front = vehicle.FrontPoint;
            if (!TryGetNamedPoint(vehicle, "BackPoint", out var back) &&
                !TryGetNamedPoint(vehicle, "RearPoint", out back) &&
                !TryGetNamedPoint(vehicle, "backPoint", out back))
                return false;

            var delta = front - back;
            delta.y = 0f;
            if (delta.sqrMagnitude < 1f)
                return false;

            length = delta.magnitude;
            return true;
        }

        private static bool TryGetNamedPoint(VehicleController vehicle, string memberName, out Vector3 point)
        {
            point = default;
            var type = vehicle.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var property = type.GetProperty(memberName, flags);
            if (property?.GetValue(vehicle) is Vector3 propertyPoint)
            {
                point = propertyPoint;
                return true;
            }

            var field = type.GetField(memberName, flags);
            if (field?.GetValue(vehicle) is Vector3 fieldPoint)
            {
                point = fieldPoint;
                return true;
            }

            return false;
        }

        private static bool TryGetLengthFromBounds(VehicleController vehicle, out float length)
        {
            length = 0f;
            var forward = vehicle.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                return false;

            forward.Normalize();

            var min = float.MaxValue;
            var max = float.MinValue;
            var found = false;
            var colliders = vehicle.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                ProjectBounds(collider.bounds, forward, ref min, ref max);
                found = true;
            }

            if (!found)
                return false;

            length = max - min;
            return length > 1f;
        }

        private static void ProjectBounds(Bounds bounds, Vector3 forward, ref float min, ref float max)
        {
            var center = bounds.center;
            var extents = bounds.extents;

            for (var xi = -1; xi <= 1; xi += 2)
            {
                for (var yi = -1; yi <= 1; yi += 2)
                {
                    for (var zi = -1; zi <= 1; zi += 2)
                    {
                        var corner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                        var projection = Vector3.Dot(corner, forward);
                        if (projection < min)
                            min = projection;
                        if (projection > max)
                            max = projection;
                    }
                }
            }
        }
    }
}
