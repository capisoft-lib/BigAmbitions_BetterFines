using System;
using System.Collections.Generic;
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

        private const float SpeedingOverLimitPercent = 10f;
        internal const float SpeedingMinDelaySec = 5f;
        internal const float SpeedHoldSec = 3f;
        internal const float WrongWayMinDelaySec = 5f;
        internal const float WrongWayHoldSec = 3f;

        private const string SpeedingEnabledKey = "speeding_enabled";
        private const string VisualFlashEnabledKey = "visual_flash_enabled";
        private const string RedLightEnabledKey = "red_light_enabled";
        private const string RedLightOrangeFineKey = "red_light_orange_fine";
        private const string LicenseSuspensionEnabledKey = "license_suspension_enabled";
        private const string WrongWayEnabledKey = "wrong_way_enabled";
        private const string FineAmountModeKey = "fine_amount_mode";
        private const string FineMarginPercentKey = "fine_margin_percent";
        private const string FixedFineAmountKey = "fixed_fine_amount";
        private const string ValueDollarsKey = "betterfines_options_value_dollars";
        private const string ValuePercentKey = "betterfines_options_value_percent";
        private const int DefaultFixedFineAmount = 200;

        private static readonly string[] FineAmountModeChoices =
        {
            "betterfines_options_fine_mode_fixed",
            "betterfines_options_fine_mode_margin_percent"
        };

        private static ModContext _context;
        private static string _configPath;
        private static DateTime _lastConfigWriteUtc = DateTime.MinValue;

        internal static bool EnforceSpeeding { get; private set; } = true;
        internal static bool EnforceRedLights { get; private set; } = true;
        internal static FineAmountMode FineAmountMode { get; private set; } = FineAmountMode.Fixed;
        internal static float FineMarginPercent { get; private set; } = 10f;
        internal static int FixedFineAmount { get; private set; } = DefaultFixedFineAmount;
        internal static float RedLightMinDelaySec { get; private set; } = 5f;
        internal static float RedLightMinSpeedKmh { get; private set; } = 3f;
        internal static bool VisualFlashEnabled { get; private set; } = true;
        internal static bool RedLightOrangeFine { get; private set; }
        internal static bool EnforceWrongWay { get; private set; } = true;
        internal static float WrongWayMinSpeedKmh { get; private set; } = 8f;
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

        /// <summary>Dev-only CSV dumps (default off; never written by Save/BuildJson).</summary>
        internal static bool DumpRoadSpeedLimits { get; private set; }
        internal static bool DumpTrafficApproachZones { get; private set; }
        internal static bool DumpTrafficLightVisuals { get; private set; }

        internal static bool ShouldDrawTrafficZones => DebugTrafficZones || DebugRedLight;

        internal static float GetSpeedingThresholdKmh(float limitKmh) =>
            limitKmh * (1f + SpeedingOverLimitPercent / 100f);

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
            BetterFinesOptionsScheduler.Shutdown();
            if (_context != null)
                OptionsService.RemoveModOptions(_context.ModId);
            _context = null;
            _configPath = null;
            _lastConfigWriteUtc = DateTime.MinValue;
            ResetDefaults();
        }

        internal static void RefreshOptions() => RegisterOptions();

        private static void RegisterOptions()
        {
            if (_context == null)
                return;

            OptionsService.RemoveModOptions(_context.ModId);

            var options = new ModOptions()
                .AddHeader("betterfines_options_header")
                .AddDropdown(FineAmountModeKey, "betterfines_options_fine_mode",
                    FineAmountModeChoices, (int)FineAmountMode, value =>
                {
                    OnFineAmountModeChanged(value);
                });

            if (FineAmountMode == FineAmountMode.Fixed)
            {
                options.AddSlider(FixedFineAmountKey, "betterfines_options_fixed_fine_amount", 25, 1000,
                    FixedFineAmount, value =>
                {
                    FixedFineAmount = Mathf.Clamp(value, 25, 1000);
                    Save();
                }, ValueDollarsKey);
            }
            else
            {
                options.AddSlider(FineMarginPercentKey, "betterfines_options_fine_margin_percent", 1, 100,
                    Mathf.RoundToInt(FineMarginPercent), value =>
                {
                    FineMarginPercent = Mathf.Clamp(value, 1, 100);
                    Save();
                }, ValuePercentKey);
            }

            options
                .AddToggle(VisualFlashEnabledKey, "betterfines_options_visual_flash_enabled", VisualFlashEnabled, value =>
                {
                    VisualFlashEnabled = value;
                    Save();
                })
                .AddToggle(SpeedingEnabledKey, "betterfines_options_speeding_enabled", EnforceSpeeding, value =>
                {
                    EnforceSpeeding = value;
                    Save();
                })
                .AddToggle(WrongWayEnabledKey, "betterfines_options_wrong_way_enabled", EnforceWrongWay, value =>
                {
                    EnforceWrongWay = value;
                    Save();
                })
                .AddToggle(RedLightEnabledKey, "betterfines_options_red_light_enabled", EnforceRedLights, value =>
                {
                    EnforceRedLights = value;
                    Save();
                })
                .AddToggle(RedLightOrangeFineKey, "betterfines_options_red_light_orange", RedLightOrangeFine, value =>
                {
                    RedLightOrangeFine = value;
                    Save();
                })
                .AddToggle(LicenseSuspensionEnabledKey, "betterfines_options_license_suspension", LicenseRevokeEnabled, value =>
                {
                    OnLicenseSuspensionEnabledChanged(value);
                });

            OptionsService.Register(_context.ModId, options);
            ModLog.Info("Mod options registered (" + ConfigFileName + ").");
        }

        private static void OnLicenseSuspensionEnabledChanged(bool enabled)
        {
            if (LicenseRevokeEnabled == enabled)
                return;

            LicenseRevokeEnabled = enabled;
            if (!enabled)
                FineRecordStore.SetLicenseSuspended(false);

            Save();
        }

        private static void OnFineAmountModeChanged(int value)
        {
            var mode = (FineAmountMode)Mathf.Clamp(value, 0, FineAmountModeChoices.Length - 1);
            if (FineAmountMode == mode)
                return;

            FineAmountMode = mode;
            Save();
            BetterFinesOptionsScheduler.RequestRefresh();
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
            var lines = new List<string>
            {
                "  \"fine_amount_mode\": \"" + FineAmountModeToJson(FineAmountMode) + "\""
            };

            lines.Add("  \"fine_margin_percent\": " + FineMarginPercent.ToString(inv));
            if (FixedFineAmount != DefaultFixedFineAmount)
                lines.Add("  \"fixed_fine_amount\": " + FixedFineAmount);

            lines.Add("  \"speeding_enabled\": " + (EnforceSpeeding ? "true" : "false"));
            lines.Add("  \"wrong_way_enabled\": " + (EnforceWrongWay ? "true" : "false"));
            lines.Add("  \"red_light_enabled\": " + (EnforceRedLights ? "true" : "false"));
            lines.Add("  \"visual_flash_enabled\": " + (VisualFlashEnabled ? "true" : "false"));

            if (!Mathf.Approximately(WrongWayMinSpeedKmh, 8f))
                lines.Add("  \"wrong_way_min_speed_kmh\": " + WrongWayMinSpeedKmh.ToString(inv));
            if (!Mathf.Approximately(RedLightMinDelaySec, 5f))
                lines.Add("  \"red_light_min_delay_sec\": " + RedLightMinDelaySec.ToString(inv));
            if (!Mathf.Approximately(RedLightMinSpeedKmh, 3f))
                lines.Add("  \"red_light_min_speed_kmh\": " + RedLightMinSpeedKmh.ToString(inv));
            if (RedLightOrangeFine)
                lines.Add("  \"red_light_orange_fine\": true");
            if (!Mathf.Approximately(RoadLookupMaxM, 40f))
                lines.Add("  \"road_lookup_max_m\": " + RoadLookupMaxM.ToString(inv));
            if (!Mathf.Approximately(RedLightLookupMaxM, 35f))
                lines.Add("  \"red_light_lookup_max_m\": " + RedLightLookupMaxM.ToString(inv));
            if (!RecidivismEnabled)
                lines.Add("  \"recidivism_enabled\": false");
            if (FineLifetimeDays != 5)
                lines.Add("  \"fine_lifetime_days\": " + FineLifetimeDays);
            if (RecidivismTier1Count != 3)
                lines.Add("  \"recidivism_tier1_count\": " + RecidivismTier1Count);
            if (RecidivismTier1Percent != 50)
                lines.Add("  \"recidivism_tier1_percent\": " + RecidivismTier1Percent);
            if (RecidivismTier2Count != 5)
                lines.Add("  \"recidivism_tier2_count\": " + RecidivismTier2Count);
            if (RecidivismTier2Percent != 100)
                lines.Add("  \"recidivism_tier2_percent\": " + RecidivismTier2Percent);
            if (!LicenseRevokeEnabled)
                lines.Add("  \"license_revoke_enabled\": false");
            if (LicenseRevokeCount != 10)
                lines.Add("  \"license_revoke_count\": " + LicenseRevokeCount);
            if (LogEnabled)
                lines.Add("  \"log_enabled\": true");
            if (DebugRedLight)
                lines.Add("  \"debug_red_light\": true");
            if (DebugTrafficZones)
                lines.Add("  \"debug_traffic_zones\": true");

            return "{\n" + string.Join(",\n", lines) + "\n}";
        }

        private static void ResetDefaults()
        {
            EnforceSpeeding = true;
            EnforceRedLights = true;
            FineAmountMode = FineAmountMode.Fixed;
            FineMarginPercent = 10f;
            FixedFineAmount = DefaultFixedFineAmount;
            RedLightMinDelaySec = 5f;
            RedLightMinSpeedKmh = 3f;
            VisualFlashEnabled = true;
            RedLightOrangeFine = false;
            EnforceWrongWay = true;
            WrongWayMinSpeedKmh = 8f;
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
            DumpRoadSpeedLimits = false;
            DumpTrafficApproachZones = false;
            DumpTrafficLightVisuals = false;
        }

        private static void ApplyJson(string json)
        {
            FineAmountMode = ReadFineAmountMode(json, "fine_amount_mode", FineAmountMode);
            FineMarginPercent = Mathf.Clamp(ReadFloat(json, "fine_margin_percent", FineMarginPercent), 1f, 100f);
            FixedFineAmount = Mathf.Max(25, ReadInt(json, "fixed_fine_amount",
                ReadInt(json, "speeding_fine_amount", FixedFineAmount)));
            EnforceSpeeding = ReadBool(json, "speeding_enabled", EnforceSpeeding);
            VisualFlashEnabled = ReadBool(json, "visual_flash_enabled", VisualFlashEnabled);
            EnforceRedLights = ReadBool(json, "red_light_enabled", EnforceRedLights);
            RedLightMinDelaySec = Mathf.Max(5f, ReadFloat(json, "red_light_min_delay_sec", RedLightMinDelaySec));
            RedLightMinSpeedKmh = Mathf.Max(0f, ReadFloat(json, "red_light_min_speed_kmh", RedLightMinSpeedKmh));
            RedLightOrangeFine = ReadBool(json, "red_light_orange_fine", RedLightOrangeFine);
            EnforceWrongWay = ReadBool(json, "wrong_way_enabled", EnforceWrongWay);
            WrongWayMinSpeedKmh = Mathf.Max(0f, ReadFloat(json, "wrong_way_min_speed_kmh", WrongWayMinSpeedKmh));
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
            DumpRoadSpeedLimits = ReadBool(json, "dump_road_speed_limits", DumpRoadSpeedLimits);
            DumpTrafficApproachZones = ReadBool(json, "dump_traffic_approach_zones", DumpTrafficApproachZones);
            DumpTrafficLightVisuals = ReadBool(json, "dump_traffic_light_visuals", DumpTrafficLightVisuals);
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

        private static FineAmountMode ReadFineAmountMode(string json, string key, FineAmountMode fallback)
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
            if (raw is "margin_percent" or "previous_supplier_margin_percent" or "supplier_margin_percent")
                return FineAmountMode.PreviousSupplierMarginPercent;
            if (raw is "fixed" or "fixed_amount")
                return FineAmountMode.Fixed;
            return int.TryParse(raw, out var index) && index == 1
                ? FineAmountMode.PreviousSupplierMarginPercent
                : fallback;
        }

        private static string FineAmountModeToJson(FineAmountMode mode) =>
            mode == FineAmountMode.PreviousSupplierMarginPercent ? "margin_percent" : "fixed";

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
