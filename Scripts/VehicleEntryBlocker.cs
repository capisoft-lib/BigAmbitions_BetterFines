using System;
using Player.HUD.ItemInfoOverlays;

namespace BetterFines
{
    internal static class VehicleEntryBlocker
    {
        private const string DriveCtaKey = "click_to_drive";

        internal static void BlockDriveCta(Action blockedAttempt)
        {
            if (blockedAttempt == null)
                return;

            if (CtaManager.ctaKey != DriveCtaKey || CtaManager.ctaAction == null)
                return;

            if (ReferenceEquals(CtaManager.ctaAction, blockedAttempt))
                return;

            CtaManager.ctaAction = blockedAttempt;
        }
    }
}
