using System;
using UnityEngine;

namespace BetterFines
{
    internal static class DrivingLicenseEnforcer
    {
        private const float WarningCooldownSec = 2.5f;

        private static readonly Action BlockedDriveAttemptDelegate = BlockedDriveAttempt;
        private static float _nextWarningAt;

        internal static void Initialize()
        {
        }

        internal static void Shutdown()
        {
            _nextWarningAt = 0f;
            VehicleDriveSuppressor.Reset();
        }

        internal static void OnLicenseSuspended()
        {
            CancelPendingDriveGoal();
            ShowBlockedWarning(force: true);
        }

        internal static void Tick()
        {
            if (!RecidivismService.IsLicenseSuspended)
            {
                VehicleDriveSuppressor.SuppressIfSuspended();
                return;
            }

            VehicleEntryBlocker.BlockDriveCta(BlockedDriveAttemptDelegate);
            VehicleDriveSuppressor.SuppressIfSuspended();
        }

        private static void BlockedDriveAttempt()
        {
            CancelPendingDriveGoal();
            ShowBlockedWarning();
        }

        private static void ShowBlockedWarning(bool force = false)
        {
            if (!force && Time.unscaledTime < _nextWarningAt)
                return;

            _nextWarningAt = Time.unscaledTime + WarningCooldownSec;
            SpeedWarningBanner.ShowLicenseSuspended();
            ModLog.Info("Blocked vehicle entry due to suspended license.");
        }

        private static void CancelPendingDriveGoal()
        {
            try
            {
                GameManager.Instance?.playerController?.ResetNavigation();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to cancel pending drive goal: " + ex.Message);
            }
        }
    }
}
