using EFT;
using EFT.UI;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class PlayerProfileViewPatchAdapter : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return PlayerProfileViewService.GetMenuScreenShowTargetMethod();
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance, Profile profile, MatchmakerPlayerControllerClass matchmaker)
        {
            try
            {
                if (!MenuGuardHelpers.IsMainMenuShowContext(__instance, profile, matchmaker, "Profile"))
                {
                    return;
                }

                await PlayerProfileViewService.ApplyMainMenuProfileViewAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Profile, e.ToString());
            }
        }
    }
}
