using BepInEx.Logging;
using BepInEx;
using MoxoPixel.MenuOverhaul.Patches;
using MoxoPixel.MenuOverhaul.Utils;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Infrastructure.Reflection;
using MoxoPixel.MenuOverhaul.Infrastructure.Lifecycle;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
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

            EftReflectionMap.Initialize();
            MenuDiagnosticsLogger.Info(LogSubsystem.Reflection, EftReflectionMap.GetStartupSummary());
            if (EftReflectionMap.HasMissingMembers())
            {
                foreach (string missingMember in EftReflectionMap.GetMissingMembers())
                {
                    MenuDiagnosticsLogger.WarningOnce(
                        LogSubsystem.Reflection,
                        "Reflection.StartupMissing." + missingMember,
                        "Missing reflection member at startup: " + missingMember);
                }
            }
            else
            {
                MenuDiagnosticsLogger.Info(LogSubsystem.Reflection, "Integrity check passed: all mapped members resolved.");
            }

            InitializeAndEnablePatches();

            LogSource.LogInfo($"Plugin {Info.Metadata.Name} version {Info.Metadata.Version} loaded.");
        }

        private void InitializeAndEnablePatches()
        {
            _patches.Add(new MainMenuLayoutPatchAdapter());
            _patches.Add(new PlayerProfileViewPatchAdapter());
            _patches.Add(new DefaultUIButtonIdlePatchAdapter());
            _patches.Add(new DefaultUIButtonHoverPatchAdapter());
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
                MenuLifecycleCoordinator.CleanupOnUnload();
                
                // Cleanup static helpers
                MainMenuLayoutRuntime.DisposeResources();
                MainMenuLightingService.Cleanup();
                Utils.GameStateUtility.ResetGameState();
                
                LogSource.LogDebug("MenuOverhaul plugin resources cleaned up.");
            }
            catch (Exception ex)
            {
                LogSource.LogError($"Error during plugin cleanup: {ex}");
            }
        }
    }
}