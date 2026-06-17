namespace BetterFines.Public
{
    /// <summary>Companion-mod API to issue BetterFines tickets (e.g. BetterPedestrians).</summary>
    public static class BetterFinesFineApi
    {
        public static bool TryChargePedestrianFine()
        {
            if (!BetterPedestriansBridge.CanChargePedestrianFine())
                return false;

            return FineService.TryChargeFine(
                ViolationType.Pedestrian,
                FineAmountResolver.Resolve(ViolationType.Pedestrian, BetterFinesConfig.FixedFineAmount));
        }
    }
}
