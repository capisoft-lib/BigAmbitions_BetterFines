using System;
using UnityEngine;

namespace BetterFines
{
    internal sealed class BetterFinesDriver : MonoBehaviour
    {
        private readonly ViolationTracker _tracker = new ViolationTracker();
        private float _nextTick;
        private bool _hudFailureLogged;

        private void OnDestroy() => _tracker.Reset();

        private void Update()
        {
            SpeedWarningBanner.TickAutoHide();
            RedLightCameraFlash.Tick();

            if (Time.unscaledTime < _nextTick)
                return;

            _nextTick = Time.unscaledTime + 0.1f;

            ModUiText.PollLanguageChange();
            FineRecordStore.Tick();
            try
            {
                FinesStatusPanel.UpdateDisplaySafe();
            }
            catch (Exception ex)
            {
                if (!_hudFailureLogged)
                {
                    _hudFailureLogged = true;
                    ModLog.Warn(
                        "Fines HUD update failed; traffic enforcement will continue. " +
                        ex.GetType().Name + ": " + ex.Message);
                    Debug.LogException(ex);
                }
            }

            _tracker.Tick();
        }

        private void FixedUpdate()
        {
            if (RecidivismService.IsLicenseSuspended)
                VehicleDriveSuppressor.SuppressIfSuspended();
        }

        private void LateUpdate() => DrivingLicenseEnforcer.Tick();
    }
}
