using UnityEngine;

namespace BetterFines
{
    /// <summary>
    /// Same PlayerPrefs keys as Big Ambitions <c>ModOptionsToggleControl</c> (m:{modId}:{optionId}).
    /// In-game options are loaded from prefs; JSON holds advanced tuning only.
    /// </summary>
    internal static class BetterFinesGameOptionPrefs
    {
        internal static bool LoadToggle(string modId, string optionId, bool defaultValue)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return defaultValue;

            if (UnityEngine.PlayerPrefs.HasKey(key))
                return UnityEngine.PlayerPrefs.GetInt(key) != 0;

            return defaultValue;
        }

        internal static int LoadInt(string modId, string optionId, int defaultValue)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return defaultValue;

            if (UnityEngine.PlayerPrefs.HasKey(key))
                return UnityEngine.PlayerPrefs.GetInt(key);

            return defaultValue;
        }

        internal static void SaveToggle(string modId, string optionId, bool value)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return;

            UnityEngine.PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        internal static void SaveInt(string modId, string optionId, int value)
        {
            var key = BuildKey(modId, optionId);
            if (key == null)
                return;

            UnityEngine.PlayerPrefs.SetInt(key, value);
        }

        internal static bool HasKey(string modId, string optionId)
        {
            var key = BuildKey(modId, optionId);
            return key != null && UnityEngine.PlayerPrefs.HasKey(key);
        }

        private static string BuildKey(string modId, string optionId)
        {
            if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(optionId))
                return null;

            return "m:" + modId + ":" + optionId;
        }
    }
}
