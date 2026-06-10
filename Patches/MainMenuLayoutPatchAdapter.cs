using EFT;
using EFT.UI;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class MainMenuLayoutPatchAdapter : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return MenuGuardHelpers.GetMenuScreenShowTargetMethod();
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance, Profile profile, MatchmakerPlayerControllerClass matchmaker)
        {
            try
            {
                if (!MenuGuardHelpers.IsMainMenuShowContext(__instance, profile, matchmaker, "Layout"))
                {
                    return;
                }

                await MainMenuLayoutService.ApplyMainMenuLayoutAsync(__instance).ConfigureAwait(false);
                MainMenuButtonsService.ApplyMainMenuButtons(__instance);
            }
            catch (Exception e)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Layout, e.ToString());
            }
        }
    }
}
