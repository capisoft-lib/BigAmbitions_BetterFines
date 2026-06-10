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
        private const string StateFilePrefix = "active_fines_";
        private const string StateFileSuffix = ".json";

        private static readonly List<ActiveFineRecord> Records = new List<ActiveFineRecord>();
        private static string _modRootPath;
        private static string _statePath;
        private static string _boundSaveKey;
        private static bool _licenseSuspended;

        internal static IReadOnlyList<ActiveFineRecord> ActiveFines => Records;
        internal static bool LicenseSuspended => _licenseSuspended;

        internal static void Initialize(ModContext context)
        {
            _modRootPath = context != null ? context.ModRootPath : null;
            RebindSaveIfNeeded();
            Load();
        }

        internal static void Shutdown()
        {
            Save();
            Records.Clear();
            _licenseSuspended = false;
            _boundSaveKey = null;
            _statePath = null;
            _modRootPath = null;
        }

        internal static void Tick()
        {
            RebindSaveIfNeeded();
            PurgeExpired();
            if (_licenseSuspended && Records.Count == 0)
                _licenseSuspended = false;
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
            Save();
        }

        internal static void SetLicenseSuspended(bool suspended)
        {
            if (_licenseSuspended == suspended)
                return;

            _licenseSuspended = suspended;
            Save();
        }

        private static void RebindSaveIfNeeded()
        {
            var saveKey = ResolveSaveKey();
            if (saveKey == _boundSaveKey)
                return;

            if (!string.IsNullOrEmpty(_boundSaveKey))
                Save();

            _boundSaveKey = saveKey;
            _statePath = string.IsNullOrEmpty(_modRootPath)
                ? null
                : Path.Combine(_modRootPath, StateFilePrefix + saveKey + StateFileSuffix);
            Records.Clear();
            _licenseSuspended = false;
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
                Save();
        }

        private static string ResolveSaveKey()
        {
            try
            {
                var save = SaveGameManager.Current;
                if (save == null)
                    return "unknown";

                var type = save.GetType();
                foreach (var name in new[] { "SaveName", "saveName", "Name", "FileName", "Id", "GUID" })
                {
                    var prop = type.GetProperty(name);
                    if (prop == null)
                        continue;

                    var value = prop.GetValue(save);
                    if (value == null)
                        continue;

                    var text = value.ToString();
                    if (!string.IsNullOrWhiteSpace(text) && text != "0")
                        return Sanitize(text);
                }
            }
            catch
            {
                // Save metadata not available yet.
            }

            return "default";
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
            _licenseSuspended = false;

            if (string.IsNullOrEmpty(_statePath) || !File.Exists(_statePath))
                return;

            try
            {
                ApplyJson(File.ReadAllText(_statePath));
                ModLog.Info("Loaded active fine records.");
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to read active fine records: " + ex.Message);
            }
        }

        private static void Save()
        {
            if (string.IsNullOrEmpty(_statePath))
                return;

            try
            {
                File.WriteAllText(_statePath, BuildJson(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to write active fine records: " + ex.Message);
            }
        }

        private static string BuildJson()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"save_key\": \"").Append(Escape(_boundSaveKey ?? "default")).Append("\",\n");
            sb.Append("  \"license_suspended\": ").Append(_licenseSuspended ? "true" : "false").Append(",\n");
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
            _licenseSuspended = ReadBool(json, "license_suspended", false);

            var idx = 0;
            while (true)
            {
                var start = json.IndexOf("\"type\"", idx, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                    break;

                var type = ReadString(json, "type", start, "Speeding");
                if (!Enum.TryParse(type, true, out ViolationType violationType))
                    violationType = ViolationType.Speeding;

                Records.Add(new ActiveFineRecord
                {
                    Type = violationType,
                    IssuedDay = ReadInt(json, "issued_day", start, 0),
                    IssuedHour = ReadInt(json, "issued_hour", start, 0),
                    IssuedMinute = ReadInt(json, "issued_minute", start, 0),
                    Amount = ReadInt(json, "amount", start, 0),
                    ExpiresDay = ReadInt(json, "expires_day", start, 0)
                });

                idx = start + 6;
            }
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
