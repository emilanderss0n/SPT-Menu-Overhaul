using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class OnGameEndedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod(nameof(Player.OnGameSessionEnd), BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            GameStateUtility.SetGameStarted(false);

            if (PlayerProfileViewService.ClonedPlayerModelView != null)
            {
                PlayerProfileViewService.ClonedPlayerModelView.SetActive(true);
                MainMenuLightingService.SetupLights(PlayerProfileViewService.ClonedPlayerModelView);
            }

            MenuDiagnosticsLogger.Debug(LogSubsystem.Lifecycle, "MenuOverhaul: game ended, custom menu re-armed.");
        }
    }
}