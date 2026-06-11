using UnityEngine;

namespace BetterFines
{
    /// <summary>
    /// Keeps the player inside an already-entered vehicle but blocks movement.
    /// Never calls ExitVehicle — CarController re-enters and spams
    /// carcontroller_no_exitpositon_found when the exit warp fails.
    /// </summary>
    internal static class VehicleDriveSuppressor
    {
        private static bool _inputLocked;
        private static bool _vehicleFrozen;
        private static VehicleController _frozenVehicle;
        private static float _nextMoveWarningAt;

        internal static void SuppressIfSuspended()
        {
            if (!RecidivismService.IsLicenseSuspended)
            {
                RestoreIfNeeded();
                return;
            }

            var vehicle = GameManager.Instance?.selectedVehicle;
            if (vehicle == null || !vehicle.controlledByPlayer)
            {
                RestoreIfNeeded();
                return;
            }

            if (!_vehicleFrozen || _frozenVehicle != vehicle)
            {
                if (_vehicleFrozen && _frozenVehicle != null)
                    _frozenVehicle.SetFreeze(false);

                vehicle.SetFreeze(true);
                _frozenVehicle = vehicle;
                _vehicleFrozen = true;
            }

            if (vehicle is CarController car && car.vehicleController != null)
                LockDrivingInput(car.vehicleController);
        }

        internal static void Reset()
        {
            _nextMoveWarningAt = 0f;
            RestoreIfNeeded();
        }

        private static void LockDrivingInput(global::NWH.VehiclePhysics2.VehicleController physics)
        {
            var tryingToMove = false;
            if (physics.input != null)
            {
                tryingToMove = physics.input.Throttle > 0.01f
                    || Mathf.Abs(physics.input.Steering) > 0.01f;

                if (!_inputLocked)
                {
                    physics.input.autoSetInput = false;
                    _inputLocked = true;
                }

                physics.input.Throttle = 0f;
                physics.input.Brakes = 1f;
                physics.input.Steering = 0f;
            }

            if (physics.steering != null)
                physics.steering.externallyAddedAngle = 0f;

            if (!tryingToMove || Time.unscaledTime < _nextMoveWarningAt)
                return;

            _nextMoveWarningAt = Time.unscaledTime + 2.5f;
            SpeedWarningBanner.ShowLicenseSuspended();
        }

        private static void RestoreIfNeeded()
        {
            if (_vehicleFrozen && _frozenVehicle != null)
            {
                _frozenVehicle.SetFreeze(false);
                _frozenVehicle = null;
                _vehicleFrozen = false;
            }

            RestoreInputIfNeeded();
        }

        private static void RestoreInputIfNeeded()
        {
            if (!_inputLocked)
                return;

            _inputLocked = false;
            var vehicle = GameManager.Instance?.selectedVehicle;
            if (vehicle is not CarController car || car.vehicleController?.input == null)
                return;

            car.vehicleController.input.autoSetInput = true;
        }
    }
}
