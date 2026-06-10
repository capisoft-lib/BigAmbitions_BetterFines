using System;
using System.Threading.Tasks;
using BAModAPI;

[assembly: RegisterModClass(typeof(BetterFines.BetterFinesOptionsMod))]

namespace BetterFines
{
    /// <summary>
    /// Registers mod options at game initialization so they stay visible in
    /// ESC &gt; Options &gt; Mods (main menu and in-city) and survive city unload.
    /// </summary>
    [ModEntryOnInitializationLoad]
    public sealed class BetterFinesOptionsMod : IModBigAmbitions
    {
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            ModLog.Info("BetterFines options init | mod_id=" + context.ModId);
            BetterFinesConfig.Initialize(context);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            BetterFinesConfig.Shutdown();
            ModLog.Info("BetterFines options unloaded.");
            return Task.CompletedTask;
        }
    }
}
