using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BAModAPI;
using UnityEngine;

namespace BetterFines
{
    internal static class FineRecordStore
    {
        private const string ModDataKey = "BetterFines.activeFines.v1";
        private const string LegacyStateFilePrefix = "active_fines_";
        private const string LegacyStateFileSuffix = ".json";
        private const int MaxFineLifetimeDaysSanity = 30;

        private static readonly List<ActiveFineRecord> Records = new List<ActiveFineRecord>();
        private static string _modRootPath;
        private static string _boundSaveId;

        internal static IReadOnlyList<ActiveFineRecord> ActiveFines => Records;
        internal static bool LicenseSuspended { get; private set; }

        internal static void Initialize(ModContext context)
        {
            _modRootPath = context != null ? context.ModRootPath : null;
            RebindSaveIfNeeded();
            RemoveLegacyModFolderFiles();
        }

        internal static void Shutdown()
        {
            Persist();
            Records.Clear();
            LicenseSuspended = false;
            _boundSaveId = null;
            _modRootPath = null;
        }

        internal static void Tick()
        {
            RebindSaveIfNeeded();

            var wasSuspended = LicenseSuspended;
            var countBefore = Records.Count;
            PurgeExpired();

            if (!wasSuspended || Records.Count > 0)
                return;

            LicenseSuspended = false;
            Persist();

            if (countBefore > 0)
                FineService.TrySendLicenseRestoredMessage();
        }

        internal static int ActiveCount => Records.Count;

        internal static int TotalActiveAmount()
        {
            var total = 0;
            for (var i = 0; i < Records.Count; i++)
                total += Records[i].Amount;
            return total;
        }

        internal static int MaxDaysRemaining(int currentDay)
        {
            var max = 0;
            for (var i = 0; i < Records.Count; i++)
            {
                var remaining = Records[i].DaysRemaining(currentDay);
                if (remaining > max)
                    max = remaining;
            }
            return max;
        }

        internal static void AddFine(ViolationType type, int amount, int issuedDay, int hour, int minute, int lifetimeDays)
        {
            Records.Add(new ActiveFineRecord
            {
                Type = type,
                IssuedDay = issuedDay,
                IssuedHour = hour,
                IssuedMinute = minute,
                Amount = amount,
                ExpiresDay = issuedDay + Mathf.Max(1, lifetimeDays)
            });
            Persist();
        }

        internal static void SetLicenseSuspended(bool suspended)
        {
            if (LicenseSuspended == suspended)
                return;

            LicenseSuspended = suspended;
            Persist();
        }

        private static void RebindSaveIfNeeded()
        {
            var saveId = ResolveSaveId();
            if (saveId == _boundSaveId)
                return;

            Records.Clear();
            LicenseSuspended = false;
            _boundSaveId = saveId;
            Load();
        }

        private static void PurgeExpired()
        {
            var save = SaveGameManager.Current;
            if (save == null)
                return;

            var currentDay = save.Day;
            var removed = false;
            for (var i = Records.Count - 1; i >= 0; i--)
            {
                if (!Records[i].IsActive(currentDay))
                {
                    Records.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
                Persist();
        }

        private static string ResolveSaveId()
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save == null)
                    return null;

                var characterId = save.characterId;
                var saveName = save.SaveGameName;
                if (string.IsNullOrWhiteSpace(characterId) && string.IsNullOrWhiteSpace(saveName))
                    return null;

                return Sanitize(characterId ?? "character") + "__" + Sanitize(saveName ?? "save");
            }
            catch
            {
                return null;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Trim();
        }

        private static void Load()
        {
            Records.Clear();
            LicenseSuspended = false;

            if (TryLoadFromModData())
            {
                ModLog.Info("Loaded active fine records from save modData.");
                return;
            }

            if (TryMigrateLegacyFile())
            {
                ModLog.Info("Migrated active fine records from legacy mod-folder file into save modData.");
                return;
            }

            ModLog.Info("No active fine records for current save.");
        }

        private static bool TryLoadFromModData()
        {
            var save = SaveGameManager.Current;
            if (save?.modData == null ||
                !save.modData.TryGetValue(ModDataKey, out var json) ||
                string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                ApplyJson(json);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to read active fine records from save modData: " + ex.Message);
                return false;
            }
        }

        private static bool TryMigrateLegacyFile()
        {
            if (string.IsNullOrEmpty(_modRootPath) || string.IsNullOrEmpty(_boundSaveId))
                return false;

            var legacyPath = Path.Combine(_modRootPath, LegacyStateFilePrefix + _boundSaveId + LegacyStateFileSuffix);
            if (!File.Exists(legacyPath))
                return false;

            try
            {
                var json = File.ReadAllText(legacyPath);
                var fileSaveKey = ReadString(json, "save_key", 0, string.Empty);
                if (string.Equals(fileSaveKey, "default", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileSaveKey, "unknown", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(fileSaveKey) &&
                    !string.Equals(fileSaveKey, _boundSaveId, StringComparison.Ordinal))
                    return false;

                ApplyJson(json);
                if (Records.Count == 0 && !LicenseSuspended)
                    return false;

                Persist();
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to migrate legacy active fine records: " + ex.Message);
                return false;
            }
        }

        private static void RemoveLegacyModFolderFiles()
        {
            if (string.IsNullOrEmpty(_modRootPath) || !Directory.Exists(_modRootPath))
                return;

            try
            {
                foreach (var path in Directory.GetFiles(_modRootPath, LegacyStateFilePrefix + "*" + LegacyStateFileSuffix))
                {
                    File.Delete(path);
                    ModLog.Info("Removed legacy mod-folder fine state: " + Path.GetFileName(path));
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to remove legacy mod-folder fine state files: " + ex.Message);
            }
        }

        private static void Persist()
        {
            var save = SaveGameManager.Current;
            if (save == null || string.IsNullOrEmpty(_boundSaveId))
                return;

            try
            {
                save.modData ??= new Dictionary<string, string>();
                save.modData[ModDataKey] = BuildJson();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to write active fine records to save modData: " + ex.Message);
            }
        }

        private static string BuildJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"save_key\": \"").Append(Escape(_boundSaveId ?? string.Empty)).Append("\",\n");
            sb.Append("  \"license_suspended\": ").Append(LicenseSuspended ? "true" : "false").Append(",\n");
            sb.Append("  \"fines\": [\n");
            for (var i = 0; i < Records.Count; i++)
            {
                var fine = Records[i];
                sb.Append("    {\n");
                sb.Append("      \"type\": \"").Append(fine.Type).Append("\",\n");
                sb.Append("      \"issued_day\": ").Append(fine.IssuedDay).Append(",\n");
                sb.Append("      \"issued_hour\": ").Append(fine.IssuedHour).Append(",\n");
                sb.Append("      \"issued_minute\": ").Append(fine.IssuedMinute).Append(",\n");
                sb.Append("      \"amount\": ").Append(fine.Amount).Append(",\n");
                sb.Append("      \"expires_day\": ").Append(fine.ExpiresDay).Append("\n");
                sb.Append("    }");
                if (i < Records.Count - 1)
                    sb.Append(',');
                sb.Append('\n');
            }
            sb.Append("  ]\n}");
            return sb.ToString();
        }

        private static void ApplyJson(string json)
        {
            LicenseSuspended = ReadBool(json, "license_suspended", false);

            var idx = 0;
            while (true)
            {
                var start = json.IndexOf("\"type\"", idx, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                    break;

                var type = ReadString(json, "type", start, "Speeding");
                if (!Enum.TryParse(type, true, out ViolationType violationType))
                    violationType = ViolationType.Speeding;

                var issuedDay = ReadInt(json, "issued_day", start, 0);
                var amount = ReadInt(json, "amount", start, 0);
                var expiresDay = ReadInt(json, "expires_day", start, 0);
                if (!IsValidFineRecord(issuedDay, amount, expiresDay))
                {
                    idx = start + 6;
                    continue;
                }

                Records.Add(new ActiveFineRecord
                {
                    Type = violationType,
                    IssuedDay = issuedDay,
                    IssuedHour = ReadInt(json, "issued_hour", start, 0),
                    IssuedMinute = ReadInt(json, "issued_minute", start, 0),
                    Amount = amount,
                    ExpiresDay = expiresDay
                });

                idx = start + 6;
            }
        }

        private static bool IsValidFineRecord(int issuedDay, int amount, int expiresDay)
        {
            if (amount <= 0)
                return false;

            if (expiresDay <= issuedDay)
                return false;

            return expiresDay - issuedDay <= MaxFineLifetimeDaysSanity;
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"");

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

        private static int ReadInt(string json, string key, int searchFrom, int fallback)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return fallback;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return fallback;

            var end = json.IndexOfAny(new[] { ',', '}', '\n', '\r' }, colon + 1);
            if (end < 0)
                end = json.Length;

            var raw = json.Substring(colon + 1, end - colon - 1).Trim();
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        private static string ReadString(string json, string key, int searchFrom, string fallback)
        {
            var token = "\"" + key + "\"";
            var idx = json.IndexOf(token, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return fallback;

            var colon = json.IndexOf(':', idx);
            if (colon < 0)
                return fallback;

            var firstQuote = json.IndexOf('"', colon + 1);
            if (firstQuote < 0)
                return fallback;

            var secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
                return fallback;

            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }
    }
}
