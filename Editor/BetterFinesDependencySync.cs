#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetterFines.Editor
{
    /// <summary>
    /// Keeps <c>Dependencies/LIB_BaUnifiedUI.dll</c> in sync for Unity Mod Builder packaging.
    /// Official ModPackager copies <c>Assets/Mods/BetterFines/Dependencies/*.dll</c> only.
    /// </summary>
    [InitializeOnLoad]
    public static class BetterFinesDependencySync
    {
        private const string DestAssetPath = "Assets/Mods/BetterFines/Dependencies/LIB_BaUnifiedUI.dll";

        static BetterFinesDependencySync()
        {
            EditorApplication.delayCall += OnDelayCall;
        }

        private static void OnDelayCall() => TrySyncFromOutput();

        [MenuItem("Big Ambitions/Mods/Better Fines/Sync bundled dependencies")]
        public static void SyncFromMenu()
        {
            if (TrySyncFromOutput(forceLog: true))
                AssetDatabase.Refresh();
            else
                Debug.LogWarning(
                    "[BetterFines] LIB_BaUnifiedUI.dll not found. Build LIB_BaUnifiedUI in Mod Builder first, " +
                    "or run tools/sync-dependencies.ps1.");
        }

        private static bool TrySyncFromOutput(bool forceLog = false)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            var candidates = new[]
            {
                Path.Combine(projectRoot, "Output", "LIB_BaUnifiedUI", "LIB_BaUnifiedUI.dll"),
                Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Hovgaard Games", "Big Ambitions", "ModsLocal",
                    "LIB_BaUnifiedUI", "LIB_BaUnifiedUI.dll")
            };

            string source = null;
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    source = path;
                    break;
                }
            }

            if (source == null)
                return false;

            var destAbsolute = Path.Combine(projectRoot, "Assets", "Mods", "BetterFines", "Dependencies", "LIB_BaUnifiedUI.dll");
            var destDir = Path.GetDirectoryName(destAbsolute);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            if (File.Exists(destAbsolute))
            {
                var srcTime = File.GetLastWriteTimeUtc(source);
                var dstTime = File.GetLastWriteTimeUtc(destAbsolute);
                if (srcTime <= dstTime)
                    return true;
            }

            File.Copy(source, destAbsolute, overwrite: true);
            AssetDatabase.ImportAsset(DestAssetPath, ImportAssetOptions.ForceUpdate);

            if (forceLog)
                Debug.Log("[BetterFines] Synced LIB_BaUnifiedUI.dll into Dependencies for Mod Builder.");

            return true;
        }
    }
}
#endif
