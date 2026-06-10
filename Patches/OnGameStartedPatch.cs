using EFT;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Utils;
using SPT.Reflection.Patching;
using System.Reflection;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class OnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            GameStateUtility.SetGameStarted(true);

            if (PlayerProfileViewService.ClonedPlayerModelView != null)
            {
                PlayerProfileViewService.ClonedPlayerModelView.SetActive(false);
            }

            // Hide the decal plane explicitly while in a raid.
            GameStateUtility.ConfigureDecalPlane(false);

            MenuDiagnosticsLogger.Debug(LogSubsystem.Lifecycle, "MenuOverhaul: game started, custom menu suspended.");
        }
    }
}