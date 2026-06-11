using System;
using Entities;
using Helpers;
using UnityEngine;

namespace BetterFines
{
    internal static class PreviousDaySupplierMarginService
    {
        internal static float GetPreviousDaySupplierMargin()
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save == null || save.Day <= 0)
                    return 0f;

                var previousDay = save.Day - 1;
                FinancialSummary summary = null;

                var summaries = save.financialSummaries;
                if (summaries != null)
                {
                    for (var i = 0; i < summaries.Count; i++)
                    {
                        var candidate = summaries[i];
                        if (candidate != null && candidate.dayNumber == previousDay)
                        {
                            summary = candidate;
                            break;
                        }
                    }
                }

                if (summary == null)
                {
                    var recent = FinancialSummaryHelper.GetLastFinancialSummaries(1);
                    if (recent != null && recent.Count > 0)
                        summary = recent[0];
                }

                if (summary == null || summary.dayNumber != previousDay)
                    return 0f;

                return SumSupplierMargin(summary);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to read previous supplier margin: " + ex.Message);
                return 0f;
            }
        }

        private static float SumSupplierMargin(FinancialSummary summary)
        {
            var statements = summary.businessIncomeStatements;
            if (statements == null || statements.Count == 0)
                return 0f;

            var margin = 0f;
            for (var i = 0; i < statements.Count; i++)
            {
                var statement = statements[i];
                if (statement == null)
                    continue;

                margin += Mathf.Max(0f, statement.TotalSales - statement.TotalResources);
            }

            return margin;
        }
    }
}
