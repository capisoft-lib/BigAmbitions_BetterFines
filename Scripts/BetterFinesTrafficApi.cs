using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterFines.Public
{
    public enum VehicleTrafficSignal
    {
        None,
        Green,
        Yellow,
        Red
    }

    public readonly struct TrafficLightInfo
    {
        public TrafficLightInfo(int instanceId, Vector3 position, Vector3 forward, VehicleTrafficSignal signal)
        {
            InstanceId = instanceId;
            Position = position;
            Forward = forward;
            Signal = signal;
        }

        public int InstanceId { get; }
        public Vector3 Position { get; }
        public Vector3 Forward { get; }
        public VehicleTrafficSignal Signal { get; }
    }

    /// <summary>Read-only traffic-light snapshot for companion mods (e.g. BetterPedestrians).</summary>
    public static class BetterFinesTrafficApi
    {
        public static bool IsReady => TrafficDataStore.TryEnsureLoaded() && TrafficLightVisualIndex.Count > 0;

        public static int LightCount => TrafficLightVisualIndex.Count;

        public static IReadOnlyList<TrafficLightInfo> GetLights()
        {
            if (!TrafficDataStore.TryEnsureLoaded())
                return Array.Empty<TrafficLightInfo>();

            var lights = TrafficLightVisualIndex.Lights;
            if (lights == null || lights.Length == 0)
                return Array.Empty<TrafficLightInfo>();

            var result = new TrafficLightInfo[lights.Length];
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                light.TryReadActiveSignal(out var signal);
                result[i] = new TrafficLightInfo(
                    light.InstanceId,
                    light.Position,
                    light.HasForward ? light.Forward : Vector3.forward,
                    MapSignal(signal));
            }

            return result;
        }

        public static bool TryFindNearestLight(Vector3 position, float maxDistanceM, out TrafficLightInfo light)
        {
            light = default;
            if (!TrafficDataStore.TryEnsureLoaded())
                return false;

            var lights = TrafficLightVisualIndex.Lights;
            if (lights == null || lights.Length == 0)
                return false;

            var maxDistanceSq = maxDistanceM * maxDistanceM;
            var bestDistanceSq = float.MaxValue;
            var found = false;

            for (var i = 0; i < lights.Length; i++)
            {
                var candidate = lights[i];
                var distanceSq = HorizontalDistanceSq(position, candidate.Position);
                if (distanceSq > maxDistanceSq || distanceSq >= bestDistanceSq)
                    continue;

                candidate.TryReadActiveSignal(out var signal);
                bestDistanceSq = distanceSq;
                light = new TrafficLightInfo(
                    candidate.InstanceId,
                    candidate.Position,
                    candidate.HasForward ? candidate.Forward : Vector3.forward,
                    MapSignal(signal));
                found = true;
            }

            return found;
        }

        /// <summary>True when vehicles may proceed (pedestrians should wait).</summary>
        public static bool IsVehicleProceedSignal(VehicleTrafficSignal signal)
        {
            return signal == VehicleTrafficSignal.Green || signal == VehicleTrafficSignal.Yellow;
        }

        /// <summary>True when vehicles must stop (pedestrians may cross).</summary>
        public static bool IsVehicleStopSignal(VehicleTrafficSignal signal)
        {
            return signal == VehicleTrafficSignal.Red;
        }

        private static VehicleTrafficSignal MapSignal(TrafficApproachSignal signal)
        {
            switch (signal)
            {
                case TrafficApproachSignal.Red:
                    return VehicleTrafficSignal.Red;
                case TrafficApproachSignal.Yellow:
                    return VehicleTrafficSignal.Yellow;
                default:
                    return VehicleTrafficSignal.Green;
            }
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
