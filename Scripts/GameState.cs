using UI.MiniMenu;
using UI.Smartphone;

namespace BetterFines
{
    internal static class GameState
    {
        internal static bool ShouldEnforceTickets()
        {
            if (!IsWorldReady())
                return false;

            if (IsInsideInterior())
                return false;

            if (IsOverlayBlockingNavigation())
                return false;

            return true;
        }

        internal static bool ShouldShowFinesPanel()
        {
            if (!IsWorldReady())
                return false;

            if (IsInsideInterior())
                return false;

            if (IsOverlayBlockingNavigation())
                return false;

            return true;
        }

        private static bool IsOverlayBlockingNavigation()
        {
            try
            {
                if (CityMap.IsOpen)
                    return true;

                if (FullMenu.IsOpen)
                    return true;

                if (MiniMenu.IsOpen)
                    return true;
            }
            catch
            {
                return true;
            }

            return false;
        }

        private static bool IsWorldReady()
        {
            try
            {
                if (!GameManager.IsInitialized)
                    return false;

                var gm = GameManager.Instance;
                if (gm == null || gm.playerController == null)
                    return false;

                if (IsSceneLoading())
                    return false;

                var save = SaveGameManager.Current;
                if (save == null)
                    return false;

                if (!save.CityInitialized)
                    return false;

                if (save.BuildingRegistrations == null || save.BuildingRegistrations.Count == 0)
                    return false;

                if (!CityManager.IsInitialized)
                    return false;

                if (!BuildingManager.IsInitialized)
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool IsInsideInterior()
        {
            try
            {
                if (BuildingManager.IsInsideBuilding)
                    return true;
            }
            catch
            {
                return true;
            }

            return false;
        }

        private static bool IsSceneLoading()
        {
            try
            {
                var asm = typeof(BuildingManager).Assembly;
                var loadScene = asm.GetType("LoadScene") ?? asm.GetType("UI.Load.LoadScene");
                var field = loadScene?.GetField("isLoading",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.GetValue(null) is bool loading && loading)
                    return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }
}
