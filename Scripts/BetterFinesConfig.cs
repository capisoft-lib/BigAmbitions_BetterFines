using System;
using System.Globalization;
using System.IO;
using System.Text;
using BAModAPI;
using BigAmbitions.Mods;
using UnityEngine;

namespace BetterFines
{
    internal static class BetterFinesConfig
    {
        private const string ConfigFileName = "better_fines_config.json";

        private const string SpeedingEnabledKey = "speeding_enabled";
        private const string SpeedingFineKey = "speeding_fine_amount";
        private const string SpeedingMinDelayKey = "speeding_min_delay_sec";
        private const string SpeedingOverLimitKey = "speeding_over_limit_kmh";
        private const string SpeedingTriggerDelayKey = "speeding_trigger_delay_sec";

        private const string RedLightEnabledKey = "red_light_enabled";
        private const string RedLightFineKey = "red_light_fine_amount";
        private const string RedLightMinDelayKey = "red_light_min_delay_sec";
        private const string RedLightMinSpeedKey = "red_light_min_speed_kmh";
        private const string RedLightOrangeFineKey = "red_light_orange_fine";

        private const string WrongWayEnabledKey = "wrong_way_enabled";
        private const string WrongWayFineKey = "wrong_way_fine_amount";
        private const string WrongWayMinDelayKey = "wrong_way_min_delay_sec";
        private const string WrongWayMinSpeedKey = "wrong_way_min_speed_kmh";
        private const string WrongWayTriggerDelayKey = "wrong_way_trigger_delay_sec";
        private const string ValueDollarsKey = "betterfines_options_value_dollars";
        private const string ValueSecondsKey = "betterfines_options_value_seconds";
        private const string ValueKmhKey = "betterfines_options_value_kmh";
        private const string ValueDaysKey = "betterfines_options_value_days";
        private const string ValuePercentKey = "betterfines_options_value_percent";
        private const string ValueCountKey = "betterfines_options_value_count";

        private const string RecidivismEnabledKey = "recidivism_enabled";
        private const string FineLifetimeDaysKey = "fine_lifetime_days";
        private const string RecidivismTier1CountKey = "recidivism_tier1_count";
        private const string RecidivismTier1PercentKey = "recidivism_tier1_percent";
        private const string RecidivismTier2CountKey = "recidivism_tier2_count";
        private const string RecidivismTier2PercentKey = "recidivism_tier2_percent";
        private const string LicenseRevokeEnabledKey = "license_revoke_enabled";
        private const string LicenseRevokeCountKey = "license_revoke_count";

        private static ModContext _context;
        private static string _configPath;
        private static DateTime _lastConfigWriteUtc = DateTime.MinValue;

        internal static bool EnforceSpeeding { get; private set; } = true;
        internal static bool EnforceRedLights { get; private set; } = true;
        internal static float GraceKmh { get; private set; } = 5f;
        internal static float SpeedHoldSec { get; private set; } = 3f;
        internal static int SpeedFineAmount { get; private set; } = 150;
        internal static int RedLightFineAmount { get; private set; } = 250;
        internal static float SpeedingMinDelaySec { get; private set; } = 10f;
        internal static float RedLightMinDelaySec { get; private set; } = 5f;
        internal static float RedLightMinSpeedKmh { get; private set; } = 8f;
        internal static bool RedLightOrangeFine { get; private set; }
        internal static bool EnforceWrongWay { get; private set; }
        internal static int WrongWayFineAmount { get; private set; } = 200;
        internal static float WrongWayMinDelaySec { get; private set; } = 10f;
        internal static float WrongWayMinSpeedKmh { get; private set; } = 8f;
        internal static float WrongWayHoldSec { get; private set; } = 3f;
        internal static float RoadLookupMaxM { get; private set; } = 40f;
        internal static float RedLightLookupMaxM { get; private set; } = 35f;
        internal static bool RecidivismEnabled { get; private set; } = true;
        internal static int FineLifetimeDays { get; private set; } = 5;
        internal static int RecidivismTier1Count { get; private set; } = 3;
        internal static int RecidivismTier1Percent { get; private set; } = 50;
        internal static int RecidivismTier2Count { get; private set; } = 5;
        internal static int RecidivismTier2Percent { get; private set; } = 100;
        internal static bool LicenseRevokeEnabled { get; private set; } = true;
        internal static int LicenseRevokeCount { get; private set; } = 10;
        internal static bool LogEnabled { get; private set; }
        internal static bool DebugRedLight { get; private set; }
        internal static bool DebugTrafficZones { get; private set; }

        internal static bool ShouldDrawTrafficZones => DebugTrafficZones || DebugRedLight;

        internal static void Initialize(ModContext context)
        {
            _context = context;
            EnsureConfigPath();
            Load();
            ModLog.Initialize(context);
            RegisterOptions();
        }

        /// <summary>
        /// Called from city load. Options may already be registered at initialization.
        /// </summary>
        internal static void EnsureReadyForRuntime(ModContext context)
        {
            if (_context == null)
            {
                Initialize(context);
                return;
            }

            if (string.IsNullOrEmpty(_configPath))
                EnsureConfigPath();

            ReloadIfChanged();
        }

        private static void EnsureConfigPath()
        {
            _configPath = _context != null && !string.IsNullOrEmpty(_context.ModRootPath)
                ? Path.Combine(_context.ModRootPath, ConfigFileName)
                : null;
        }

        internal static void ReloadIfChanged()
        {
            if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
                return;

            var writeTime = File.GetLastWriteTimeUtc(_configPath);
            if (writeTime <= _lastConfigWriteUtc)
                return;

            Load();
        }

        internal static void Shutdown()
        {
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);
            _context = null;
            _configPath = null;
            _lastConfigWriteUtc = DateTime.MinValue;
            ResetDefaults();
        }

        private static void RegisterOptions()
        {
            if (_context == null)
                return;

            OptionsService.RemoveModOptions(_context.ModId);

            var options = new ModOptions()
                .AddHeader("betterfines_options_header")
                .AddToggle(SpeedingEnabledKey, "betterfines_options_speeding_enabled", EnforceSpeeding, value =>
                {
                    EnforceSpeeding = value;
                    Save();
                })
                .AddSlider(SpeedingFineKey, "betterfines_options_speeding_fine", 25, 1000,
                    SpeedFineAmount, value =>
                    {
                        SpeedFineAmount = Mathf.Clamp(value, 25, 1000);
                        Save();
                    }, ValueDollarsKey)
                .AddSlider(SpeedingMinDelayKey, "betterfines_options_speeding_min_delay", 5, 300,
                    Mathf.RoundToInt(SpeedingMinDelaySec), value =>
                    {
                        SpeedingMinDelaySec = Mathf.Clamp(value, 5, 300);
                        Save();
                    }, ValueSecondsKey)
                .AddSlider(SpeedingOverLimitKey, "betterfines_options_speeding_over_limit", 0, 30,
                    Mathf.RoundToInt(GraceKmh), value =>
                    {
                        GraceKmh = Mathf.Clamp(value, 0, 30);
                        Save();
                    }, ValueKmhKey)
                .AddSlider(SpeedingTriggerDelayKey, "betterfines_options_speeding_trigger_delay", 1, 10,
                    Mathf.RoundToInt(SpeedHoldSec), value =>
                    {
                        SpeedHoldSec = Mathf.Clamp(value, 1, 10);
                        Save();
                    }, ValueSecondsKey)
                .AddSplitter()
                .AddToggle(WrongWayEnabledKey, "betterfines_options_wrong_way_enabled", EnforceWrongWay, value =>
                {
                    EnforceWrongWay = value;
                    Save();
                })
                .AddSlider(WrongWayFineKey, "betterfines_options_wrong_way_fine", 25, 1000,
                    WrongWayFineAmount, value =>
                    {
                        WrongWayFineAmount = Mathf.Clamp(value, 25, 1000);
                        Save();
                    }, ValueDollarsKey)
                .AddSlider(WrongWayMinDelayKey, "betterfines_options_wrong_way_min_delay", 5, 300,
                    Mathf.RoundToInt(WrongWayMinDelaySec), value =>
                    {
                        WrongWayMinDelaySec = Mathf.Clamp(value, 5, 300);
                        Save();
                    }, ValueSecondsKey)
                .AddSlider(WrongWayMinSpeedKey, "betterfines_options_wrong_way_min_speed", 0, 40,
                    Mathf.RoundToInt(WrongWayMinSpeedKmh), value =>
                    {
                        WrongWayMinSpeedKmh = Mathf.Clamp(value, 0, 40);
                        Save();
                    }, ValueKmhKey)
                .AddSlider(WrongWayTriggerDelayKey, "betterfines_options_wrong_way_trigger_delay", 1, 10,
                    Mathf.RoundToInt(WrongWayHoldSec), value =>
                    {
                        WrongWayHoldSec = Mathf.Clamp(value, 1, 10);
                        Save();
                    }, ValueSecondsKey)
                .AddSplitter()
                .AddToggle(RedLightEnabledKey, "betterfines_options_red_light_enabled", EnforceRedLights, value =>
                {
                    EnforceRedLights = value;
                    Save();
                })
                .AddSlider(RedLightFineKey, "betterfines_options_red_light_fine", 25, 1000,
                    RedLightFineAmount, value =>
                    {
                        RedLightFineAmount = Mathf.Clamp(value, 25, 1000);
                        Save();
                    }, ValueDollarsKey)
                .AddSlider(RedLightMinDelayKey, "betterfines_options_red_light_min_delay", 5, 300,
                    Mathf.RoundToInt(RedLightMinDelaySec), value =>
                    {
                        RedLightMinDelaySec = Mathf.Clamp(value, 5, 300);
                        Save();
                    }, ValueSecondsKey)
                .AddSlider(RedLightMinSpeedKey, "betterfines_options_red_light_min_speed", 0, 40,
                    Mathf.RoundToInt(RedLightMinSpeedKmh), value =>
                    {
                        RedLightMinSpeedKmh = Mathf.Clamp(value, 0, 40);
                        Save();
                    }, ValueKmhKey)
                .AddToggle(RedLightOrangeFineKey, "betterfines_options_red_light_orange", RedLightOrangeFine, value =>
                {
                    RedLightOrangeFine = value;
                    Save();
                })
                .AddSplitter()
                .AddHeader("betterfines_options_recidivism_header")
                .AddToggle(RecidivismEnabledKey, "betterfines_options_recidivism_enabled", RecidivismEnabled, value =>
                {
                    RecidivismEnabled = value;
                    Save();
                })
                .AddSlider(FineLifetimeDaysKey, "betterfines_options_fine_lifetime_days", 1, 30,
                    FineLifetimeDays, value =>
                    {
                        FineLifetimeDays = Mathf.Clamp(value, 1, 30);
                        Save();
                    }, ValueDaysKey)
                .AddSlider(RecidivismTier1CountKey, "betterfines_options_recidivism_tier1_count", 2, 20,
                    RecidivismTier1Count, value =>
                    {
                        RecidivismTier1Count = Mathf.Clamp(value, 2, 20);
                        Save();
                    }, ValueCountKey)
                .AddSlider(RecidivismTier1PercentKey, "betterfines_options_recidivism_tier1_percent", 0, 300,
                    RecidivismTier1Percent, value =>
                    {
                        RecidivismTier1Percent = Mathf.Clamp(value, 0, 300);
                        Save();
                    }, ValuePercentKey)
                .AddSlider(RecidivismTier2CountKey, "betterfines_options_recidivism_tier2_count", 2, 20,
                    RecidivismTier2Count, value =>
                    {
                        RecidivismTier2Count = Mathf.Clamp(value, 2, 20);
                        Save();
                    }, ValueCountKey)
                .AddSlider(RecidivismTier2PercentKey, "betterfines_options_recidivism_tier2_percent", 0, 300,
                    RecidivismTier2Percent, value =>
                    {
                        RecidivismTier2Percent = Mathf.Clamp(value, 0, 300);
                        Save();
                    }, ValuePercentKey)
                .AddToggle(LicenseRevokeEnabledKey, "betterfines_options_license_revoke_enabled", LicenseRevokeEnabled, value =>
                {
                    LicenseRevokeEnabled = value;
                    Save();
                })
                .AddSlider(LicenseRevokeCountKey, "betterfines_options_license_revoke_count", 3, 30,
                    LicenseRevokeCount, value =>
                    {
                        LicenseRevokeCount = Mathf.Clamp(value, 3, 30);
                        Save();
                    }, ValueCountKey);

            OptionsService.Register(_context.ModId, options);
            ModLog.Info("Mod options registered (" + ConfigFileName + ").");
        }

        private static void Load()
        {
            ResetDefaults();

            if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
                return;

            try
            {
                ApplyJson(File.ReadAllText(_configPath));
                _lastConfigWriteUtc = File.GetLastWriteTimeUtc(_configPath);
                ModLog.Info("Loaded " + ConfigFileName);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to read " + ConfigFileName + ": " + ex.Message);
            }
        }

        private static void Save()
        {
            if (string.IsNullOrEmpty(_configPath))
                return;

            try
            {
                File.WriteAllText(_configPath, BuildJson(), Encoding.UTF8);
                _lastConfigWriteUtc = File.GetLastWriteTimeUtc(_configPath);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to write " + ConfigFileName + ": " + ex.Message);
            }
        }

        private static string BuildJson()
        {
            var inv = CultureInfo.InvariantCulture;
            return "{\n" +
                   "  \"speeding_enabled\": " + (EnforceSpeeding ? "true" : "false") + ",\n" +
                   "  \"speeding_fine_amount\": " + SpeedFineAmount + ",\n" +
                   "  \"speeding_min_delay_sec\": " + SpeedingMinDelaySec.ToString(inv) + ",\n" +
                   "  \"speeding_over_limit_kmh\": " + GraceKmh.ToString(inv) + ",\n" +
                   "  \"speeding_trigger_delay_sec\": " + SpeedHoldSec.ToString(inv) + ",\n" +
                   "  \"wrong_way_enabled\": " + (EnforceWrongWay ? "true" : "false") + ",\n" +
                   "  \"wrong_way_fine_amount\": " + WrongWayFineAmount + ",\n" +
                   "  \"wrong_way_min_delay_sec\": " + WrongWayMinDelaySec.ToString(inv) + ",\n" +
                   "  \"wrong_way_min_speed_kmh\": " + WrongWayMinSpeedKmh.ToString(inv) + ",\n" +
                   "  \"wrong_way_trigger_delay_sec\": " + WrongWayHoldSec.ToString(inv) + ",\n" +
                   "  \"red_light_enabled\": " + (EnforceRedLights ? "true" : "false") + ",\n" +
                   "  \"red_light_fine_amount\": " + RedLightFineAmount + ",\n" +
                   "  \"red_light_min_delay_sec\": " + RedLightMinDelaySec.ToString(inv) + ",\n" +
                   "  \"red_light_min_speed_kmh\": " + RedLightMinSpeedKmh.ToString(inv) + ",\n" +
                   "  \"red_light_orange_fine\": " + (RedLightOrangeFine ? "true" : "false") + ",\n" +
                   "  \"road_lookup_max_m\": " + RoadLookupMaxM.ToString(inv) + ",\n" +
                   "  \"red_light_lookup_max_m\": " + RedLightLookupMaxM.ToString(inv) + ",\n" +
                   "  \"recidivism_enabled\": " + (RecidivismEnabled ? "true" : "false") + ",\n" +
                   "  \"fine_lifetime_days\": " + FineLifetimeDays + ",\n" +
                   "  \"recidivism_tier1_count\": " + RecidivismTier1Count + ",\n" +
                   "  \"recidivism_tier1_percent\": " + RecidivismTier1Percent + ",\n" +
                   "  \"recidivism_tier2_count\": " + RecidivismTier2Count + ",\n" +
                   "  \"recidivism_tier2_percent\": " + RecidivismTier2Percent + ",\n" +
                   "  \"license_revoke_enabled\": " + (LicenseRevokeEnabled ? "true" : "false") + ",\n" +
                   "  \"license_revoke_count\": " + LicenseRevokeCount + ",\n" +
                   "  \"log_enabled\": " + (LogEnabled ? "true" : "false") + ",\n" +
                   "  \"debug_red_light\": " + (DebugRedLight ? "true" : "false") + ",\n" +
                   "  \"debug_traffic_zones\": " + (DebugTrafficZones ? "true" : "false") + "\n" +
                   "}";
        }

        private static void ResetDefaults()
        {
            EnforceSpeeding = true;
            EnforceRedLights = true;
            GraceKmh = 5f;
            SpeedHoldSec = 3f;
            SpeedFineAmount = 150;
            RedLightFineAmount = 250;
            SpeedingMinDelaySec = 10f;
            RedLightMinDelaySec = 5f;
            RedLightMinSpeedKmh = 8f;
            RedLightOrangeFine = false;
            EnforceWrongWay = false;
            WrongWayFineAmount = 200;
            WrongWayMinDelaySec = 10f;
            WrongWayMinSpeedKmh = 8f;
            WrongWayHoldSec = 3f;
            RoadLookupMaxM = 40f;
            RedLightLookupMaxM = 35f;
            RecidivismEnabled = true;
            FineLifetimeDays = 5;
            RecidivismTier1Count = 3;
            RecidivismTier1Percent = 50;
            RecidivismTier2Count = 5;
            RecidivismTier2Percent = 100;
            LicenseRevokeEnabled = true;
            LicenseRevokeCount = 10;
            LogEnabled = false;
            DebugRedLight = false;
            DebugTrafficZones = false;
        }

        private static void ApplyJson(string json)
        {
            EnforceSpeeding = ReadBool(json, "speeding_enabled", ReadBool(json, "enforce_speeding", EnforceSpeeding));
            EnforceRedLights = ReadBool(json, "red_light_enabled", ReadBool(json, "enforce_red_lights", EnforceRedLights));
            GraceKmh = Mathf.Max(0f, ReadFloat(json, "speeding_over_limit_kmh", ReadFloat(json, "grace_kmh", GraceKmh)));
            SpeedHoldSec = Mathf.Max(1f, ReadFloat(json, "speeding_trigger_delay_sec", ReadFloat(json, "speed_hold_sec", SpeedHoldSec)));
            SpeedFineAmount = Mathf.Max(25, ReadInt(json, "speeding_fine_amount", ReadInt(json, "speed_fine_amount", SpeedFineAmount)));
            RedLightFineAmount = Mathf.Max(25, ReadInt(json, "red_light_fine_amount", RedLightFineAmount));
            SpeedingMinDelaySec = Mathf.Max(5f, ReadFloat(json, "speeding_min_delay_sec", ReadFloat(json, "cooldown_sec", SpeedingMinDelaySec)));
            RedLightMinDelaySec = Mathf.Max(5f, ReadFloat(json, "red_light_min_delay_sec", ReadFloat(json, "cooldown_sec", RedLightMinDelaySec)));
            RedLightMinSpeedKmh = Mathf.Max(0f, ReadFloat(json, "red_light_min_speed_kmh", RedLightMinSpeedKmh));
            RedLightOrangeFine = ReadBool(json, "red_light_orange_fine", RedLightOrangeFine);
            EnforceWrongWay = ReadBool(json, "wrong_way_enabled", ReadBool(json, "enforce_wrong_way", EnforceWrongWay));
            WrongWayFineAmount = Mathf.Max(25, ReadInt(json, "wrong_way_fine_amount", WrongWayFineAmount));
            WrongWayMinDelaySec = Mathf.Max(5f, ReadFloat(json, "wrong_way_min_delay_sec", WrongWayMinDelaySec));
            WrongWayMinSpeedKmh = Mathf.Max(0f, ReadFloat(json, "wrong_way_min_speed_kmh", WrongWayMinSpeedKmh));
            WrongWayHoldSec = Mathf.Max(1f, ReadFloat(json, "wrong_way_trigger_delay_sec", ReadFloat(json, "wrong_way_hold_sec", WrongWayHoldSec)));
            RoadLookupMaxM = Mathf.Max(10f, ReadFloat(json, "road_lookup_max_m", RoadLookupMaxM));
            RedLightLookupMaxM = Mathf.Max(10f, ReadFloat(json, "red_light_lookup_max_m", RedLightLookupMaxM));
            RecidivismEnabled = ReadBool(json, "recidivism_enabled", RecidivismEnabled);
            FineLifetimeDays = Mathf.Clamp(ReadInt(json, "fine_lifetime_days", FineLifetimeDays), 1, 30);
            RecidivismTier1Count = Mathf.Clamp(ReadInt(json, "recidivism_tier1_count", RecidivismTier1Count), 2, 20);
            RecidivismTier1Percent = Mathf.Clamp(ReadInt(json, "recidivism_tier1_percent", RecidivismTier1Percent), 0, 300);
            RecidivismTier2Count = Mathf.Clamp(ReadInt(json, "recidivism_tier2_count", RecidivismTier2Count), 2, 20);
            RecidivismTier2Percent = Mathf.Clamp(ReadInt(json, "recidivism_tier2_percent", RecidivismTier2Percent), 0, 300);
            LicenseRevokeEnabled = ReadBool(json, "license_revoke_enabled", LicenseRevokeEnabled);
            LicenseRevokeCount = Mathf.Clamp(ReadInt(json, "license_revoke_count", LicenseRevokeCount), 3, 30);
            LogEnabled = ReadBool(json, "log_enabled", LogEnabled);
            DebugRedLight = ReadBool(json, "debug_red_light", DebugRedLight);
            DebugTrafficZones = ReadBool(json, "debug_traffic_zones", DebugTrafficZones);
        }

        private static float ReadFloat(string json, string key, float fallback)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return fallback;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return fallback;

            var end = json.IndexOfAny(new[] { ',', '}', '\n', '\r' }, colon + 1);
            if (end < 0)
                end = json.Length;

            var raw = json.Substring(colon + 1, end - colon - 1).Trim();
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        private static int ReadInt(string json, string key, int fallback)
        {
            var value = ReadFloat(json, key, fallback);
            return Mathf.RoundToInt(value);
        }

        private static bool ReadBool(string json, string key, bool fallback)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return fallback;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return fallback;

            var end = json.IndexOfAny(new[] { ',', '}', '\n', '\r' }, colon + 1);
            if (end < 0)
                end = json.Length;

            var raw = json.Substring(colon + 1, end - colon - 1).Trim().Trim('"').ToLowerInvariant();
            if (raw == "true")
                return true;
            if (raw == "false")
                return false;
            return fallback;
        }
    }
}
