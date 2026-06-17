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

        private static readonly string[] LegacyGameOptionJsonKeys =
        {
            "fine_amount_mode",
            "fine_margin_percent",
            "fixed_fine_amount",
            "speeding_fine_amount",
            "speeding_enabled",
            "visual_flash_enabled",
            "red_light_enabled",
            "red_light_orange_fine",
            "wrong_way_enabled",
            "license_revoke_enabled"
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

        /// <summary>Dev-only CSV dumps (default off; never written by SaveAdvancedConfig).</summary>
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
            EnsureOptionsRegistered();
        }

        /// <summary>
        /// Called from city load. Options may already be registered at initialization.
        /// </summary>
        internal static void EnsureReadyForRuntime(ModContext context)
        {
            if (_context == null)
            {
                _context = context;
                EnsureConfigPath();
                Load();
                ModLog.Initialize(context);
            }
            else if (string.IsNullOrEmpty(_configPath))
            {
                EnsureConfigPath();
            }

            ReloadIfChanged();
            LoadGameOptions();
            EnsureOptionsRegistered();
        }

        internal static void EnsureOptionsRegistered()
        {
            if (_context == null)
                return;

            RegisterOptions();
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

            try
            {
                ApplyAdvancedJson(File.ReadAllText(_configPath));
                _lastConfigWriteUtc = writeTime;
                ModLog.Info("Reloaded " + ConfigFileName);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to reload " + ConfigFileName + ": " + ex.Message);
            }
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

        internal static void RefreshOptions() => EnsureOptionsRegistered();

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
                }, ValueDollarsKey);
            }
            else
            {
                options.AddSlider(FineMarginPercentKey, "betterfines_options_fine_margin_percent", 1, 100,
                    Mathf.RoundToInt(FineMarginPercent), value =>
                {
                    FineMarginPercent = Mathf.Clamp(value, 1, 100);
                }, ValuePercentKey);
            }

            options
                .AddToggle(VisualFlashEnabledKey, "betterfines_options_visual_flash_enabled", VisualFlashEnabled, value =>
                {
                    VisualFlashEnabled = value;
                })
                .AddToggle(SpeedingEnabledKey, "betterfines_options_speeding_enabled", EnforceSpeeding, value =>
                {
                    EnforceSpeeding = value;
                })
                .AddToggle(WrongWayEnabledKey, "betterfines_options_wrong_way_enabled", EnforceWrongWay, value =>
                {
                    EnforceWrongWay = value;
                })
                .AddToggle(RedLightEnabledKey, "betterfines_options_red_light_enabled", EnforceRedLights, value =>
                {
                    EnforceRedLights = value;
                })
                .AddToggle(RedLightOrangeFineKey, "betterfines_options_red_light_orange", RedLightOrangeFine, value =>
                {
                    RedLightOrangeFine = value;
                })
                .AddToggle(LicenseSuspensionEnabledKey, "betterfines_options_license_suspension", LicenseRevokeEnabled, value =>
                {
                    OnLicenseSuspensionEnabledChanged(value);
                });

            try
            {
                OptionsService.Register(_context.ModId, options);
                ModLog.Info("Mod options registered.");
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to register mod options: " + ex.Message);
            }
        }

        private static void LoadGameOptions()
        {
            if (_context == null)
                return;

            RepairStaleGameOptionPrefs();

            var modId = _context.ModId;
            FineAmountMode = (FineAmountMode)Mathf.Clamp(
                BetterFinesGameOptionPrefs.LoadInt(modId, FineAmountModeKey, (int)FineAmountMode.Fixed),
                0, FineAmountModeChoices.Length - 1);
            FixedFineAmount = Mathf.Clamp(
                BetterFinesGameOptionPrefs.LoadInt(modId, FixedFineAmountKey, DefaultFixedFineAmount), 25, 1000);
            FineMarginPercent = Mathf.Clamp(
                BetterFinesGameOptionPrefs.LoadInt(modId, FineMarginPercentKey, 10), 1, 100);
            VisualFlashEnabled = BetterFinesGameOptionPrefs.LoadToggle(modId, VisualFlashEnabledKey, true);
            EnforceSpeeding = BetterFinesGameOptionPrefs.LoadToggle(modId, SpeedingEnabledKey, true);
            EnforceWrongWay = BetterFinesGameOptionPrefs.LoadToggle(modId, WrongWayEnabledKey, true);
            EnforceRedLights = BetterFinesGameOptionPrefs.LoadToggle(modId, RedLightEnabledKey, true);
            RedLightOrangeFine = BetterFinesGameOptionPrefs.LoadToggle(modId, RedLightOrangeFineKey, false);
            LicenseRevokeEnabled = BetterFinesGameOptionPrefs.LoadToggle(modId, LicenseSuspensionEnabledKey, true);
        }

        /// <summary>
        /// PlayerPrefs are global (not per-save). Stale false values from the old JSON/prefs bug
        /// show all enforcement toggles off; restore intended defaults (all on except orange).
        /// </summary>
        private static void RepairStaleGameOptionPrefs()
        {
            if (_context == null)
                return;

            var modId = _context.ModId;
            var speeding = BetterFinesGameOptionPrefs.LoadToggle(modId, SpeedingEnabledKey, true);
            var wrongWay = BetterFinesGameOptionPrefs.LoadToggle(modId, WrongWayEnabledKey, true);
            var redLight = BetterFinesGameOptionPrefs.LoadToggle(modId, RedLightEnabledKey, true);
            var orange = BetterFinesGameOptionPrefs.LoadToggle(modId, RedLightOrangeFineKey, false);

            if (speeding && wrongWay && redLight)
                return;

            if (!speeding && !wrongWay && !redLight && !orange)
            {
                BetterFinesGameOptionPrefs.SaveToggle(modId, SpeedingEnabledKey, true);
                BetterFinesGameOptionPrefs.SaveToggle(modId, WrongWayEnabledKey, true);
                BetterFinesGameOptionPrefs.SaveToggle(modId, RedLightEnabledKey, true);
                ModLog.Info("Repaired stale mod option prefs (enforcement toggles reset to on).");
                return;
            }

            if (!BetterFinesGameOptionPrefs.HasKey(modId, SpeedingEnabledKey))
            {
                BetterFinesGameOptionPrefs.SaveToggle(modId, SpeedingEnabledKey, true);
                BetterFinesGameOptionPrefs.SaveToggle(modId, WrongWayEnabledKey, true);
                BetterFinesGameOptionPrefs.SaveToggle(modId, RedLightEnabledKey, true);
                BetterFinesGameOptionPrefs.SaveToggle(modId, RedLightOrangeFineKey, false);
                BetterFinesGameOptionPrefs.SaveToggle(modId, VisualFlashEnabledKey, true);
                BetterFinesGameOptionPrefs.SaveToggle(modId, LicenseSuspensionEnabledKey, true);
            }
        }

        private static void MigrateLegacyJsonGameOptions(string json)
        {
            if (_context == null)
                return;

            var modId = _context.ModId;
            MigrateIntIfMissing(modId, FineAmountModeKey,
                (int)ReadFineAmountMode(json, "fine_amount_mode", FineAmountMode.Fixed));
            MigrateIntIfMissing(modId, FineMarginPercentKey,
                Mathf.RoundToInt(Mathf.Clamp(ReadFloat(json, "fine_margin_percent", 10f), 1f, 100f)));
            MigrateIntIfMissing(modId, FixedFineAmountKey,
                Mathf.Clamp(ReadInt(json, "fixed_fine_amount",
                    ReadInt(json, "speeding_fine_amount", DefaultFixedFineAmount)), 25, 1000));
            MigrateToggleIfMissing(modId, VisualFlashEnabledKey,
                ReadBool(json, "visual_flash_enabled", true));
            MigrateToggleIfMissing(modId, SpeedingEnabledKey,
                ReadBool(json, "speeding_enabled", true));
            MigrateToggleIfMissing(modId, WrongWayEnabledKey,
                ReadBool(json, "wrong_way_enabled", true));
            MigrateToggleIfMissing(modId, RedLightEnabledKey,
                ReadBool(json, "red_light_enabled", true));
            MigrateToggleIfMissing(modId, RedLightOrangeFineKey,
                ReadBool(json, "red_light_orange_fine", false));
            MigrateToggleIfMissing(modId, LicenseSuspensionEnabledKey,
                ReadBool(json, "license_revoke_enabled", true));

            ModLog.Info("Migrated in-game options from " + ConfigFileName + " to mod options (PlayerPrefs).");
        }

        private static void MigrateToggleIfMissing(string modId, string optionId, bool value)
        {
            if (!BetterFinesGameOptionPrefs.HasKey(modId, optionId))
                BetterFinesGameOptionPrefs.SaveToggle(modId, optionId, value);
        }

        private static void MigrateIntIfMissing(string modId, string optionId, int value)
        {
            if (!BetterFinesGameOptionPrefs.HasKey(modId, optionId))
                BetterFinesGameOptionPrefs.SaveInt(modId, optionId, value);
        }

        private static bool ContainsLegacyGameOptionKeys(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            foreach (var key in LegacyGameOptionJsonKeys)
            {
                if (json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static void OnLicenseSuspensionEnabledChanged(bool enabled)
        {
            if (LicenseRevokeEnabled == enabled)
                return;

            LicenseRevokeEnabled = enabled;
            if (!enabled)
                FineRecordStore.SetLicenseSuspended(false);
        }

        private static void OnFineAmountModeChanged(int value)
        {
            var mode = (FineAmountMode)Mathf.Clamp(value, 0, FineAmountModeChoices.Length - 1);
            if (FineAmountMode == mode)
                return;

            FineAmountMode = mode;
            BetterFinesOptionsScheduler.RequestRefresh();
        }

        private static void Load()
        {
            ResetDefaults();
            var migrated = false;

            if (!string.IsNullOrEmpty(_configPath) && File.Exists(_configPath))
            {
                try
                {
                    var json = File.ReadAllText(_configPath);
                    if (ContainsLegacyGameOptionKeys(json))
                    {
                        MigrateLegacyJsonGameOptions(json);
                        migrated = true;
                    }

                    ApplyAdvancedJson(json);
                    _lastConfigWriteUtc = File.GetLastWriteTimeUtc(_configPath);
                    ModLog.Info("Loaded " + ConfigFileName);
                }
                catch (Exception ex)
                {
                    ModLog.Warn("Failed to read " + ConfigFileName + ": " + ex.Message);
                }
            }

            LoadGameOptions();

            if (migrated)
                SaveAdvancedConfig();
        }

        private static void SaveAdvancedConfig()
        {
            if (string.IsNullOrEmpty(_configPath))
                return;

            try
            {
                File.WriteAllText(_configPath, BuildAdvancedJson(), Encoding.UTF8);
                _lastConfigWriteUtc = File.GetLastWriteTimeUtc(_configPath);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to write " + ConfigFileName + ": " + ex.Message);
            }
        }

        private static string BuildAdvancedJson()
        {
            var inv = CultureInfo.InvariantCulture;
            var lines = new List<string>();

            if (!Mathf.Approximately(WrongWayMinSpeedKmh, 8f))
                lines.Add("  \"wrong_way_min_speed_kmh\": " + WrongWayMinSpeedKmh.ToString(inv));
            if (!Mathf.Approximately(RedLightMinDelaySec, 5f))
                lines.Add("  \"red_light_min_delay_sec\": " + RedLightMinDelaySec.ToString(inv));
            if (!Mathf.Approximately(RedLightMinSpeedKmh, 3f))
                lines.Add("  \"red_light_min_speed_kmh\": " + RedLightMinSpeedKmh.ToString(inv));
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
            if (LicenseRevokeCount != 10)
                lines.Add("  \"license_revoke_count\": " + LicenseRevokeCount);
            if (LogEnabled)
                lines.Add("  \"log_enabled\": true");
            if (DebugRedLight)
                lines.Add("  \"debug_red_light\": true");
            if (DebugTrafficZones)
                lines.Add("  \"debug_traffic_zones\": true");
            if (DumpRoadSpeedLimits)
                lines.Add("  \"dump_road_speed_limits\": true");
            if (DumpTrafficApproachZones)
                lines.Add("  \"dump_traffic_approach_zones\": true");
            if (DumpTrafficLightVisuals)
                lines.Add("  \"dump_traffic_light_visuals\": true");

            if (lines.Count == 0)
                return "{\n}\n";

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

        private static void ApplyAdvancedJson(string json)
        {
            RedLightMinDelaySec = Mathf.Max(5f, ReadFloat(json, "red_light_min_delay_sec", RedLightMinDelaySec));
            RedLightMinSpeedKmh = Mathf.Max(0f, ReadFloat(json, "red_light_min_speed_kmh", RedLightMinSpeedKmh));
            WrongWayMinSpeedKmh = Mathf.Max(0f, ReadFloat(json, "wrong_way_min_speed_kmh", WrongWayMinSpeedKmh));
            RoadLookupMaxM = Mathf.Max(10f, ReadFloat(json, "road_lookup_max_m", RoadLookupMaxM));
            RedLightLookupMaxM = Mathf.Max(10f, ReadFloat(json, "red_light_lookup_max_m", RedLightLookupMaxM));
            RecidivismEnabled = ReadBool(json, "recidivism_enabled", RecidivismEnabled);
            FineLifetimeDays = Mathf.Clamp(ReadInt(json, "fine_lifetime_days", FineLifetimeDays), 1, 30);
            RecidivismTier1Count = Mathf.Clamp(ReadInt(json, "recidivism_tier1_count", RecidivismTier1Count), 2, 20);
            RecidivismTier1Percent = Mathf.Clamp(ReadInt(json, "recidivism_tier1_percent", RecidivismTier1Percent), 0, 300);
            RecidivismTier2Count = Mathf.Clamp(ReadInt(json, "recidivism_tier2_count", RecidivismTier2Count), 2, 20);
            RecidivismTier2Percent = Mathf.Clamp(ReadInt(json, "recidivism_tier2_percent", RecidivismTier2Percent), 0, 300);
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
