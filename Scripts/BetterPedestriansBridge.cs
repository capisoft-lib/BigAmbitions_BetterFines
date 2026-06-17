using System;
using System.Reflection;
using UnityEngine;

namespace BetterFines
{
    /// <summary>Optional runtime bridge to BetterPedestrians — no compile-time assembly reference.</summary>
    internal static class BetterPedestriansBridge
    {
        private static bool _probed;
        private static bool _isPresent;
        private static PropertyInfo _pedestrianFinesEnabledProperty;

        internal static bool CanChargePedestrianFine()
        {
            if (!EnsureProbed() || !_isPresent)
                return false;

            if (_pedestrianFinesEnabledProperty == null)
                return false;

            try
            {
                return (bool)_pedestrianFinesEnabledProperty.GetValue(null);
            }
            catch (Exception ex)
            {
                ModLog.Warn("BetterPedestrians fines probe failed: " + ex.Message);
                return false;
            }
        }

        private static bool EnsureProbed()
        {
            if (_probed)
                return _isPresent;

            _probed = true;
            try
            {
                var assembly = FindBetterPedestriansAssembly();
                if (assembly == null)
                    return false;

                var configType = assembly.GetType("BetterPedestrians.BetterPedestriansConfig", throwOnError: false);
                _pedestrianFinesEnabledProperty = configType?.GetProperty(
                    "PedestrianFinesEnabled",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                _isPresent = _pedestrianFinesEnabledProperty != null;
                if (_isPresent)
                    ModLog.Info("BetterPedestrians bridge ready for pedestrian hit fines.");
            }
            catch (Exception ex)
            {
                ModLog.Warn("BetterPedestrians bridge probe failed: " + ex.Message);
                _isPresent = false;
            }

            return _isPresent;
        }

        private static Assembly FindBetterPedestriansAssembly()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var name = assemblies[i].GetName().Name;
                if (name == "BetterPedestrians")
                    return assemblies[i];
            }

            return null;
        }
    }
}
