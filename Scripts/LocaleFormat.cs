using System.Globalization;
using Extensions;

namespace BetterFines
{
    internal static class LocaleFormat
    {
        internal static string Money(int amount)
        {
            try
            {
                if (GenericExtensions.cultureInfo != null)
                    return amount.ToShortCurrencyFormat();
            }
            catch
            {
                // Fall through to invariant currency formatting.
            }

            var culture = GenericExtensions.cultureInfo ?? CultureInfo.InvariantCulture;
            return amount.ToString("C0", culture);
        }

        internal static string Integer(int value)
        {
            try
            {
                if (GenericExtensions.cultureInfo != null)
                    return value.ToFormattedNumber();
            }
            catch
            {
                // Fall through to invariant number formatting.
            }

            var culture = GenericExtensions.cultureInfo ?? CultureInfo.InvariantCulture;
            return value.ToString("N0", culture);
        }
    }
}
