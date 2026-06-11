using BaPlayerLocation.Subscriber;

using UnityEngine;



namespace BetterFines

{

    internal sealed class ViolationTracker

    {

        private readonly WrongWayDetector _wrongWay = new WrongWayDetector();
        private readonly SpeedLimitTracker _speedLimit = new SpeedLimitTracker();
        private readonly RedLightCrossingDetector _redLightCrossings = new RedLightCrossingDetector();



        private const float SpeedCheckIntervalSec = 0.5f;
        private const float RedLightCheckIntervalSec = 0.1f;
        private const float WrongWayCheckIntervalSec = 0.5f;
        private const float SpeedLookupMinKmh = 20f;
        private const float ViolationClearGraceSec = 0.75f;

        private float _nextSpeedCheckAt;
        private float _speedBelowThresholdSince = -1f;
        private float _wrongWayClearSince = -1f;
        private float _nextRedLightCheckAt;
        private float _nextWrongWayCheckAt;

        private float _speedViolationSince = -1f;

        private float _wrongWayViolationSince = -1f;

        private float _lastSpeedFineTime = -999f;

        private float _lastWrongWayFineTime = -999f;

        private float _lastRedLightFineTime = -999f;



        internal void Reset()

        {

            _speedViolationSince = -1f;

            _wrongWayViolationSince = -1f;

            _lastSpeedFineTime = -999f;

            _lastWrongWayFineTime = -999f;

            _lastRedLightFineTime = -999f;

            _nextSpeedCheckAt = 0f;
            _nextRedLightCheckAt = 0f;
            _nextWrongWayCheckAt = 0f;
            _speedBelowThresholdSince = -1f;
            _wrongWayClearSince = -1f;

            _wrongWay.Invalidate();
            _speedLimit.Reset();
            _redLightCrossings.Reset();
            VehicleGeometry.ClearCache();

            SpeedWarningBanner.Hide();

        }



        internal void Tick()

        {

            BetterFinesConfig.ReloadIfChanged();



            if (!GameState.ShouldEnforceTickets())

            {

                ClearSpeedViolation();

                ClearWrongWayViolation();

                return;

            }



            if (!BetterFinesState.IsDrivingCar())

            {

                ClearSpeedViolation();

                ClearWrongWayViolation();

                return;

            }



            var snapshot = BetterFinesState.LastSnapshot;

            var now = Time.unscaledTime;



            if (BetterFinesConfig.EnforceSpeeding && now >= _nextSpeedCheckAt)
            {
                _nextSpeedCheckAt = now + (_speedViolationSince >= 0f ? 0.25f : SpeedCheckIntervalSec);
                TickSpeeding(snapshot, now);
            }

            if (BetterFinesConfig.EnforceWrongWay && now >= _nextWrongWayCheckAt)
            {
                _nextWrongWayCheckAt = now + (_wrongWayViolationSince >= 0f ? 0.25f : WrongWayCheckIntervalSec);
                TickWrongWay(snapshot, now);
            }

            if (BetterFinesConfig.EnforceRedLights && now >= _nextRedLightCheckAt)
            {
                _nextRedLightCheckAt = now + RedLightCheckIntervalSec;
                TickRedLight(snapshot, now);
            }
        }



        private void TickSpeeding(PlayerLocationSnapshot snapshot, float now)

        {

            if (now - _lastSpeedFineTime < BetterFinesConfig.SpeedingMinDelaySec)

            {

                ClearSpeedViolation();

                return;

            }

            var speedKmh = BetterFinesState.SpeedKmh;

            if (_speedViolationSince < 0f && speedKmh < SpeedLookupMinKmh)
            {
                ClearSpeedViolation();
                return;
            }

            _speedLimit.UpdateForPosition(snapshot.Position);
            var limitKmh = _speedLimit.ActiveLimitKmh;
            var threshold = BetterFinesConfig.GetSpeedingThresholdKmh(limitKmh);

            if (speedKmh <= threshold)
            {
                if (_speedViolationSince < 0f)
                {
                    ClearSpeedViolation();
                    return;
                }

                if (_speedBelowThresholdSince < 0f)
                    _speedBelowThresholdSince = now;

                if (now - _speedBelowThresholdSince < ViolationClearGraceSec)
                    return;

                ClearSpeedViolation();
                return;
            }

            _speedBelowThresholdSince = -1f;



            if (_speedViolationSince < 0f)

            {

                _speedViolationSince = now;

                SpeedWarningBanner.ShowSpeeding();

                return;

            }



            if (now - _speedViolationSince < BetterFinesConfig.SpeedHoldSec)

                return;



            if (!FineService.TryChargeFine(
                    ViolationType.Speeding,
                    FineAmountResolver.Resolve(ViolationType.Speeding, BetterFinesConfig.FixedFineAmount)))

                return;



            _lastSpeedFineTime = now;

            ClearSpeedViolation();

            ModLog.Info(

                "Speeding ticket | speed=" + speedKmh.ToString("0") +

                " | limit=" + limitKmh.ToString("0"));

        }



        private void TickWrongWay(PlayerLocationSnapshot snapshot, float now)

        {

            if (now - _lastWrongWayFineTime < BetterFinesConfig.WrongWayMinDelaySec)

            {

                ClearWrongWayViolation();

                return;

            }



            if (BetterFinesState.SpeedKmh < BetterFinesConfig.WrongWayMinSpeedKmh)

            {

                ClearWrongWayViolation();

                return;

            }



            if (!_wrongWay.TryIsDrivingWrongWay(
                    snapshot.Position,
                    snapshot.HeadingDeg,
                    BetterFinesConfig.RoadLookupMaxM,
                    out var segmentId) || segmentId < 0)
            {
                if (_wrongWayViolationSince < 0f)
                {
                    ClearWrongWayViolation();
                    return;
                }

                if (_wrongWayClearSince < 0f)
                    _wrongWayClearSince = now;

                if (now - _wrongWayClearSince < ViolationClearGraceSec)
                    return;

                ClearWrongWayViolation();
                return;
            }

            _wrongWayClearSince = -1f;



            if (_wrongWayViolationSince < 0f)

            {

                _wrongWayViolationSince = now;

                SpeedWarningBanner.ShowWrongWay();

                return;

            }



            if (now - _wrongWayViolationSince < BetterFinesConfig.WrongWayHoldSec)

                return;



            if (!FineService.TryChargeFine(
                    ViolationType.WrongWay,
                    FineAmountResolver.Resolve(ViolationType.WrongWay, BetterFinesConfig.FixedFineAmount)))

                return;



            _lastWrongWayFineTime = now;

            ClearWrongWayViolation();

            ModLog.Info(

                "Wrong-way ticket | segment=" + segmentId +

                " | speed=" + BetterFinesState.SpeedKmh.ToString("0"));

        }



        private void TickRedLight(PlayerLocationSnapshot snapshot, float now)

        {
            var frontPosition = snapshot.Position;
            if (VehicleGeometry.TryGetPlayerFrontPosition(out var bumper))
                frontPosition = bumper;

            var speedKmh = BetterFinesState.SpeedKmh;
            var vehicleForward = HeadingToForward(snapshot.HeadingDeg);
            var violationFound = _redLightCrossings.TryFindViolationCrossing(
                frontPosition,
                vehicleForward,
                speedKmh,
                BetterFinesConfig.RedLightMinSpeedKmh,
                BetterFinesConfig.RedLightLookupMaxM,
                BetterFinesConfig.RedLightOrangeFine,
                out var crossing);

            if (now - _lastRedLightFineTime < BetterFinesConfig.RedLightMinDelaySec)
                return;

            if (!violationFound)
                return;

            if (!FineService.TryChargeFine(
                    ViolationType.RedLight,
                    FineAmountResolver.Resolve(ViolationType.RedLight, BetterFinesConfig.FixedFineAmount)))
            {
                ModLog.Warn("Red light fine charge failed after visual crossing.");
                return;
            }

            _lastRedLightFineTime = now;
            ModLog.Info(
                "Red light ticket | signal=" + crossing.Signal +
                " | speed=" + crossing.SpeedKmh.ToString("0") +
                " | group=" + crossing.GroupInstanceId +
                " | name=" + crossing.GroupName +
                " | long_m=" + crossing.LongitudinalM.ToString("0.0") +
                " | lateral_m=" + crossing.LateralM.ToString("0.0"));
        }


        private static Vector3 HeadingToForward(float headingDeg)
        {
            var rad = headingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }



        private void ClearSpeedViolation()
        {
            if (_speedViolationSince >= 0f)
                SpeedWarningBanner.HideSpeeding();

            _speedViolationSince = -1f;
            _speedBelowThresholdSince = -1f;
        }



        private void ClearWrongWayViolation()
        {
            if (_wrongWayViolationSince >= 0f)
                SpeedWarningBanner.HideWrongWay();

            _wrongWayViolationSince = -1f;
            _wrongWayClearSince = -1f;
        }

    }

}


