using UnityEngine;

namespace BetterFines
{
    internal static class RecidivismService
    {
        internal static int ApplySurcharge(int baseAmount, int activeAfterCount)
        {
            if (baseAmount <= 0)
                return 0;

            var percent = GetSurchargePercent(activeAfterCount);
            if (percent <= 0)
                return baseAmount;

            return Mathf.Max(baseAmount, Mathf.RoundToInt(baseAmount * (1f + percent / 100f)));
        }

        internal static int GetSurchargePercent(int activeAfterCount)
        {
            if (!BetterFinesConfig.RecidivismEnabled)
                return 0;

            if (activeAfterCount >= BetterFinesConfig.RecidivismTier2Count)
                return BetterFinesConfig.RecidivismTier2Percent;

            if (activeAfterCount >= BetterFinesConfig.RecidivismTier1Count)
                return BetterFinesConfig.RecidivismTier1Percent;

            return 0;
        }

        internal static int GetCurrentSurchargePercent()
        {
            var nextCount = FineRecordStore.ActiveCount + 1;
            return GetSurchargePercent(nextCount);
        }

        internal static bool RegisterIssuedFine(ViolationType type, int chargedAmount)
        {
            var save = SaveGameManager.Current;
            if (save == null)
                return false;

            var activeAfter = FineRecordStore.ActiveCount + 1;
            FineRecordStore.AddFine(
                type,
                chargedAmount,
                save.Day,
                save.Hour,
                Mathf.RoundToInt(save.Minute),
                BetterFinesConfig.FineLifetimeDays);

            if (!BetterFinesConfig.LicenseRevokeEnabled ||
                activeAfter < BetterFinesConfig.LicenseRevokeCount)
                return false;

            FineRecordStore.SetLicenseSuspended(true);
            DrivingLicenseEnforcer.OnLicenseSuspended();
            ModLog.Info("Driving license suspended | active_fines=" + activeAfter);
            return true;
        }

        internal static bool IsLicenseSuspended =>
            BetterFinesConfig.LicenseRevokeEnabled && FineRecordStore.LicenseSuspended;

        internal static int DaysUntilLicenseRestored()
        {
            var save = SaveGameManager.Current;
            if (save == null)
                return 0;

            return FineRecordStore.MaxDaysRemaining(save.Day);
        }
    }
}
