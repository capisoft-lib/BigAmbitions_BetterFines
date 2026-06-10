using System;
using System.IO;
using BAModAPI;
using UnityEngine;

namespace BetterFines
{
    internal static class ModLog
    {
        private const string Prefix = "[BetterFines]";
        private const string LogFileName = "better_fines.log";
        private const string LogsFolderName = "Logs";

        private static string _logsDir;

        internal static void Initialize(ModContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.ModRootPath))
                return;

            _logsDir = Path.Combine(context.ModRootPath, LogsFolderName);
            try
            {
                Directory.CreateDirectory(_logsDir);
                if (BetterFinesConfig.LogEnabled)
                    Info("Log file: " + Path.Combine(LogsFolderName, LogFileName));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Prefix + " Failed to create Logs folder: " + ex.Message);
            }
        }

        internal static void Shutdown() => _logsDir = null;

        internal static void Info(string message)
        {
            Debug.Log(Prefix + " " + message);
            WriteFile("INFO", message);
        }

        internal static void Warn(string message)
        {
            Debug.LogWarning(Prefix + " " + message);
            WriteFile("WARN", message);
        }

        internal static void DebugRedLight(string message)
        {
            if (!BetterFinesConfig.DebugRedLight)
                return;

            var line = "[red-light] " + message;
            Debug.Log(Prefix + " " + line);
            WriteFile("DEBUG", line);
        }

        private static void WriteFile(string level, string message)
        {
            if (!BetterFinesConfig.LogEnabled || string.IsNullOrEmpty(_logsDir))
                return;

            try
            {
                var path = Path.Combine(_logsDir, LogFileName);
                File.AppendAllText(
                    path,
                    DateTime.UtcNow.ToString("o") + " [" + level + "] " + message + Environment.NewLine);
            }
            catch
            {
                // ignore file write failures
            }
        }
    }
}
