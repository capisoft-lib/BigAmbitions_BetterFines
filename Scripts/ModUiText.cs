using Localizor;
using UnityEngine;

namespace BetterFines
{
    internal static class ModUiText
    {
        private static string _activeLocale = string.Empty;
        private static float _nextLocalePoll;

        internal static string OverSpeedLimitWarning =>
            Loc("betterfines_warning_over_speed_limit", "warning! over speed limit");

        internal static string WrongWayWarning =>
            Loc("betterfines_warning_wrong_way", "warning! wrong way");

        internal static string LicenseSuspendedWarning =>
            Loc(
                "betterfines_warning_license_suspended",
                "warning! your license is suspended and you cannot drive");

        internal static string SmsDepartmentTraffic =>
            Loc(
                "betterfines_sms_department_traffic",
                "The New York City Department of Transportation");

        internal static string SmsDepartmentMotorVehicles =>
            Loc(
                "betterfines_sms_department_motor_vehicles",
                "The New York State Department of Motor Vehicles");

        internal static string FinesPanelTitle =>
            Loc("betterfines_panel_title", "ACTIVE FINES");

        internal static string FormatFinesPanelTitle(int activeCount)
        {
            if (activeCount <= 0)
                return FinesPanelTitle;

            return LocFormat(
                "betterfines_panel_title_count",
                "ACTIVE FINES ({count})",
                "count",
                LocaleFormat.Integer(activeCount));
        }

        internal static string FormatFineLine(ViolationType type, int amount, int daysRemaining) =>
            LocFormat(
                "betterfines_panel_fine_line",
                "{type} {amount} — expires in {days}d",
                new System.Collections.Generic.Dictionary<string, string>
                {
                    { "type", ViolationLabel(type) },
                    { "amount", LocaleFormat.Money(amount) },
                    { "days", LocaleFormat.Integer(daysRemaining) }
                });

        internal static string FormatTotal(int total) =>
            LocFormat("betterfines_panel_total", "Total: {total}", "total", LocaleFormat.Money(total));

        internal static string FormatSurcharge(int percent) =>
            LocFormat(
                "betterfines_panel_surcharge",
                "Surcharge: +{percent}%",
                "percent",
                LocaleFormat.Integer(percent));

        internal static string FormatLicenseSuspended(int daysRemaining) =>
            LocFormat(
                "betterfines_panel_license_suspended",
                "License suspended — {days}d remaining",
                "days",
                LocaleFormat.Integer(daysRemaining));

        internal static void PollLanguageChange()
        {
            var now = Time.unscaledTime;
            if (now < _nextLocalePoll)
                return;
            _nextLocalePoll = now + 0.5f;

            var locale = ResolveLoadedLocale();
            if (locale == _activeLocale)
                return;

            _activeLocale = locale;
            SpeedWarningBanner.RefreshLocalizedText();
            FinesStatusPanel.RefreshLocalizedText();
        }

        private static string ViolationLabel(ViolationType type) =>
            type switch
            {
                ViolationType.RedLight => Loc("betterfines_violation_red_light", "Red light"),
                ViolationType.WrongWay => Loc("betterfines_violation_wrong_way", "Wrong way"),
                ViolationType.Pedestrian => Loc("betterfines_violation_pedestrian", "Pedestrian"),
                _ => Loc("betterfines_violation_speeding", "Speeding")
            };

        private static string LocFormat(string key, string fallback, string token, string value)
        {
            var text = Loc(key, fallback);
            return text.Replace("{" + token + "}", value);
        }

        private static string LocFormat(
            string key,
            string fallback,
            System.Collections.Generic.Dictionary<string, string> tokens)
        {
            var text = Loc(key, fallback);
            foreach (var pair in tokens)
                text = text.Replace("{" + pair.Key + "}", pair.Value);
            return text;
        }

        private static string Loc(string key, string fallback)
        {
            try
            {
                var text = key.GetLocalization();
                if (!string.IsNullOrWhiteSpace(text) && text != key)
                    return text;
            }
            catch
            {
                // Mod locale key not registered yet.
            }

            return fallback;
        }

        private static string ResolveLoadedLocale()
        {
            try
            {
                var locale = LocalizorManager.LoadedLocale;
                if (!string.IsNullOrWhiteSpace(locale))
                    return locale.Trim().Replace('_', '-');
            }
            catch
            {
                // Localizor not ready yet.
            }

            return "en";
        }
    }
}
