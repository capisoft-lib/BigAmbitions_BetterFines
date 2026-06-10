using UnityEngine;

namespace BetterFines
{
    /// <summary>Loads traffic stop geometry once after city load; never retries after the first successful bake.</summary>
    internal sealed class TrafficDataBootstrap : MonoBehaviour
    {
        private const float RetryIntervalSec = 0.5f;
        private const float MaxWaitSec = 90f;

        private float _nextAttemptAt;
        private float _deadlineAt;
        private float _startedAt;

        private void OnEnable()
        {
            _nextAttemptAt = 0f;
            _startedAt = Time.unscaledTime;
            _deadlineAt = _startedAt + MaxWaitSec;
        }

        private void Update()
        {
            if (TrafficDataStore.LoadCompleted && TrafficDataStore.Stops.IsBuilt)
            {
                enabled = false;
                return;
            }

            if (Time.unscaledTime >= _deadlineAt)
            {
                TrafficDataStore.FinalizeLoadAttempt();
                enabled = false;
                return;
            }

            if (Time.unscaledTime < _nextAttemptAt)
                return;

            _nextAttemptAt = Time.unscaledTime + RetryIntervalSec;
            if (!TrafficDataStore.TryLoadOnce())
                return;

            ModLog.Info("Traffic bootstrap complete | elapsed_sec=" +
                        (Time.unscaledTime - _startedAt).ToString("0.0"));
            enabled = false;
        }
    }
}
