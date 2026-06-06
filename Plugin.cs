using BepInEx.Logging;
using BepInEx;
using MoxoPixel.MenuOverhaul.Patches;
using MoxoPixel.MenuOverhaul.Utils;
using MoxoPixel.MenuOverhaul.Helpers;
using System.Collections.Generic;
using System;
using SPT.Reflection.Patching;

namespace MoxoPixel.MenuOverhaul
{
    [BepInPlugin(MenuOverhaulConstants.Plugin.Guid, MenuOverhaulConstants.Plugin.Name, MenuOverhaulConstants.Plugin.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource { get; private set; }

        private readonly List<ModulePatch> _patches = new List<ModulePatch>();

        private void Awake()
        {
            LogSource = Logger;
            Settings.Init(Config);

            InitializeAndEnablePatches();

            LogSource.LogInfo($"Plugin {Info.Metadata.Name} version {Info.Metadata.Version} loaded.");
        }

        private void InitializeAndEnablePatches()
        {
            _patches.Add(new MenuOverhaulPatch());
            _patches.Add(new PlayerProfileFeaturesPatch());
            _patches.Add(new SetAlphaPatch());
            _patches.Add(new TweenButtonPatch());
            _patches.Add(new OnGameStartedPatch());
            _patches.Add(new OnGameEndedPatch());

            foreach (var patch in _patches)
            {
                try
                {
                    patch.Enable();
                }
                catch (Exception ex)
                {
                    LogSource.LogError($"Failed to enable patch {patch.GetType().Name}: {ex}");
                }
            }
        }
        
        private void OnDestroy()
        {
            CleanupResources();
        }
        
        private void CleanupResources()
        {
            try
            {
                // Unsubscribe from events
                foreach (var patch in _patches)
                {
                    ICleanupPatch cleanupPatch = patch as ICleanupPatch;
                    if (cleanupPatch != null)
                    {
                        cleanupPatch.CleanupBeforeDisable();
                    }
                }
                
                // Unsubscribe screen-change listener
                MenuVisibilityController.Unsubscribe();

                // Cleanup static helpers
                LayoutHelpers.DisposeResources();
                LightHelpers.Cleanup();
                Utils.Utility.ResetGameState();
                
                LogSource.LogDebug("MenuOverhaul plugin resources cleaned up.");
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Error during plugin cleanup: {ex}");
            }
        }
    }
}