using UnityEngine;

namespace BetterFines
{
    /// <summary>
    /// Defers mod-options re-registration to the next frame so we never tear down
    /// OptionsService while the game's ModOptions UI is still initializing.
    /// </summary>
    internal sealed class BetterFinesOptionsScheduler : MonoBehaviour
    {
        private static BetterFinesOptionsScheduler _instance;
        private bool _refreshPending;

        internal static void RequestRefresh()
        {
            EnsureRunning();
            _instance._refreshPending = true;
        }

        internal static void Shutdown()
        {
            if (_instance == null)
                return;

            var host = _instance.gameObject;
            _instance = null;
            Object.Destroy(host);
        }

        internal static void EnsureRunning()
        {
            if (_instance != null)
                return;

            var host = new GameObject("BetterFines_OptionsScheduler");
            host.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(host);
            _instance = host.AddComponent<BetterFinesOptionsScheduler>();
        }

        private void Update()
        {
            if (!_refreshPending)
                return;

            _refreshPending = false;
            BetterFinesConfig.RefreshOptions();
        }
    }
}
