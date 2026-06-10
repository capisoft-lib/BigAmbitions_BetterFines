using System.Collections.Generic;
using UnityEngine;

namespace BetterFines
{
    /// <summary>Detects one-shot stop-line crossings from vanilla visual traffic-light groups.</summary>
    internal sealed class RedLightCrossingDetector
    {
        internal readonly struct Crossing
        {
            internal Crossing(
                int groupInstanceId,
                string groupName,
                TrafficApproachSignal signal,
                float speedKmh,
                float longitudinalM,
                float lateralM)
            {
                GroupInstanceId = groupInstanceId;
                GroupName = groupName;
                Signal = signal;
                SpeedKmh = speedKmh;
                LongitudinalM = longitudinalM;
                LateralM = lateralM;
            }

            internal int GroupInstanceId { get; }
            internal string GroupName { get; }
            internal TrafficApproachSignal Signal { get; }
            internal float SpeedKmh { get; }
            internal float LongitudinalM { get; }
            internal float LateralM { get; }
        }

        private struct LineState
        {
            internal bool HasLast;
            internal bool Armed;
            internal float LastLongitudinalM;
        }

        private const float MinTrafficDirectionDot = 0.45f;
        private const float ArmBeforeLineM = 1.5f;
        private const float CrossPastLineM = 0.35f;
        private const float LineBackM = -1f;
        private const float LineForwardM = 11.5f;

        private readonly Dictionary<int, LineState> _states = new Dictionary<int, LineState>();

        internal void Reset()
        {
            _states.Clear();
        }

        internal bool TryFindViolationCrossing(
            Vector3 frontPosition,
            Vector3 vehicleForward,
            float speedKmh,
            float minSpeedKmh,
            float maxDistanceM,
            bool includeOrange,
            out Crossing crossing)
        {
            crossing = default;

            if (!TrafficDataStore.TryEnsureLoaded())
                return false;

            var lights = TrafficLightVisualIndex.Lights;
            if (lights == null || lights.Length == 0)
                return false;

            vehicleForward.y = 0f;
            if (vehicleForward.sqrMagnitude < 0.01f)
                return false;
            vehicleForward.Normalize();

            var maxDistanceSq = maxDistanceM * maxDistanceM;
            var found = false;
            var bestDistanceSq = float.MaxValue;

            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (!light.HasSignalGroup ||
                    !light.HasForward ||
                    !light.TryReadActiveSignal(out var signal, out var signalForward))
                    continue;

                if (HorizontalDistanceSq(frontPosition, light.Position) > maxDistanceSq)
                    continue;

                if (!TryBuildAxes(light, signalForward, out var lineAxis, out var trafficForward))
                    continue;

                if (Vector3.Dot(vehicleForward, trafficForward) < MinTrafficDirectionDot)
                    continue;

                var toFront = frontPosition - light.Position;
                toFront.y = 0f;
                var lateral = Vector3.Dot(toFront, lineAxis);
                if (lateral < LineBackM || lateral > LineForwardM)
                {
                    DisarmIfFar(light.InstanceId);
                    continue;
                }

                var longitudinal = Vector3.Dot(light.Position - frontPosition, trafficForward);
                var crossed = UpdateCrossingState(light.InstanceId, longitudinal);
                if (!crossed)
                    continue;

                if (speedKmh < minSpeedKmh ||
                    !TrafficLightResolver.IsViolationSignal(signal, includeOrange))
                    continue;

                var distanceSq = HorizontalDistanceSq(frontPosition, light.Position);
                if (distanceSq >= bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                crossing = new Crossing(
                    light.InstanceId,
                    light.Name,
                    signal,
                    speedKmh,
                    longitudinal,
                    lateral);
                found = true;
            }

            return found;
        }

        private static bool TryBuildAxes(
            TrafficLightVisualIndex.VisualLight light,
            Vector3 signalForward,
            out Vector3 lineAxis,
            out Vector3 trafficForward)
        {
            lineAxis = light.Forward;
            lineAxis.y = 0f;
            signalForward.y = 0f;
            trafficForward = signalForward;

            if (lineAxis.sqrMagnitude < 0.01f ||
                trafficForward.sqrMagnitude < 0.01f)
                return false;

            lineAxis.Normalize();
            trafficForward.Normalize();
            return true;
        }

        private bool UpdateCrossingState(int groupInstanceId, float longitudinalM)
        {
            _states.TryGetValue(groupInstanceId, out var state);

            var crossed = state.HasLast &&
                          state.Armed &&
                          state.LastLongitudinalM > 0f &&
                          longitudinalM <= -CrossPastLineM;

            if (longitudinalM >= ArmBeforeLineM)
                state.Armed = true;
            else if (longitudinalM <= -CrossPastLineM)
                state.Armed = false;

            state.HasLast = true;
            state.LastLongitudinalM = longitudinalM;
            _states[groupInstanceId] = state;
            return crossed;
        }

        private void DisarmIfFar(int groupInstanceId)
        {
            if (!_states.TryGetValue(groupInstanceId, out var state))
                return;

            state.Armed = false;
            _states[groupInstanceId] = state;
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
