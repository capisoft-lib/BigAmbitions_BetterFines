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
        private static float _nextMoveWarningAt;

        internal static void SuppressIfSuspended()
        {
            if (!RecidivismService.IsLicenseSuspended)
            {
                RestoreInputIfNeeded();
                return;
            }

            var vehicle = GameManager.Instance?.selectedVehicle;
            if (vehicle == null || !vehicle.controlledByPlayer)
            {
                RestoreInputIfNeeded();
                return;
            }

            if (vehicle is not CarController car || car.vehicleController == null)
                return;

            LockDrivingInput(car.vehicleController);
        }

        internal static void Reset()
        {
            _nextMoveWarningAt = 0f;
            RestoreInputIfNeeded();
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

            if (physics.vehicleRigidbody != null)
            {
                physics.vehicleRigidbody.velocity = Vector3.zero;
                physics.vehicleRigidbody.angularVelocity = Vector3.zero;
            }

            if (!tryingToMove || Time.unscaledTime < _nextMoveWarningAt)
                return;

            _nextMoveWarningAt = Time.unscaledTime + 2.5f;
            SpeedWarningBanner.ShowLicenseSuspended();
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
