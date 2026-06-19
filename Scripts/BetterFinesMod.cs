using System;
using System.Threading.Tasks;
using BaPlayerLocation.Subscriber;
using BAModAPI;
using Capisoft.Lib.BaUnifiedUI.Fluent;
using UnityEngine;

[assembly: RegisterModClass(typeof(BetterFines.BetterFinesMod))]

namespace BetterFines
{
    [ModEntryOnCityLoad]
    public sealed class BetterFinesMod : IModBigAmbitions
    {
        private IDisposable _locationSubscription;
        private GameObject _driverObject;

        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            ModLog.Info(
                "BetterFines city load | mod_id=" + context.ModId +
                " | required_mod=LIB_BaPlayerLocation");

            BetterFinesConfig.EnsureReadyForRuntime(context);
            BetterFinesOptionsScheduler.EnsureRunning();
            FineRecordStore.Initialize(context);
            DrivingLicenseEnforcer.Initialize();
            SpeedZone80Index.Initialize(context.ModRootPath);
            RoadDirectionIndex.Initialize(context.ModRootPath);
            TrafficDataStore.Initialize(context.ModRootPath);
            BetterFinesState.Reset();

            _locationSubscription = PlayerLocationSubscriber.SubscribeWhenActive(OnPlayerLocationChanged);

            _driverObject = new GameObject("BetterFines_Driver");
            UnityEngine.Object.DontDestroyOnLoad(_driverObject);
            _driverObject.AddComponent<BetterFinesDriver>();
            _driverObject.AddComponent<TrafficDataBootstrap>();
            _driverObject.AddComponent<TrafficLightDebugVisualizer>();

            BaUi.EnsureReady();
            if (BaUi.ShouldRebuildChrome)
                BaUi.MarkRebuildHandled();

            ModLog.Info("BetterFines ready (speeding, red lights, wrong-way on by default).");
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            _locationSubscription?.Dispose();
            _locationSubscription = null;

            if (_driverObject != null)
            {
                UnityEngine.Object.Destroy(_driverObject);
                _driverObject = null;
            }

            BetterFinesState.Reset();
            SpeedZone80Index.Invalidate();
            RoadDirectionIndex.Invalidate();
            TrafficDataStore.Invalidate();
            DrivingLicenseEnforcer.Shutdown();
            FineRecordStore.Shutdown();
            ModLog.Shutdown();
            SpeedWarningBanner.Destroy();
            RedLightCameraFlash.Destroy();
            FinesStatusPanel.Destroy();

            ModLog.Info("BetterFines unloaded.");
            return Task.CompletedTask;
        }

        private static void OnPlayerLocationChanged(PlayerLocationSnapshot snapshot)
        {
            BetterFinesState.ApplySnapshot(snapshot);
        }
    }
}
