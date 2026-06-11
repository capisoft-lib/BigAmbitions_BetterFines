using UnityEngine;

namespace BetterFines
{
    internal static class FineAmountResolver
    {
        private const int MinimumFineAmount = 25;

        internal static int Resolve(ViolationType type, int fixedAmount)
        {
            var clampedFixed = Mathf.Max(MinimumFineAmount, fixedAmount);
            if (BetterFinesConfig.FineAmountMode != FineAmountMode.PreviousSupplierMarginPercent)
                return clampedFixed;

            var margin = PreviousDaySupplierMarginService.GetPreviousDaySupplierMargin();
            if (margin <= 0f)
            {
                ModLog.Info(
                    "Fine amount fallback | type=" + type +
                    " | reason=no_previous_supplier_margin" +
                    " | fixed=" + clampedFixed);
                return clampedFixed;
            }

            var amount = Mathf.RoundToInt(margin * BetterFinesConfig.FineMarginPercent / 100f);
            amount = Mathf.Max(MinimumFineAmount, amount);
            ModLog.Info(
                "Fine amount from margin | type=" + type +
                " | margin=" + margin.ToString("0") +
                " | percent=" + BetterFinesConfig.FineMarginPercent.ToString("0") +
                " | amount=" + amount);
            return amount;
        }
    }
}
