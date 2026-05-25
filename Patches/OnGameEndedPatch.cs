using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using MoxoPixel.MenuOverhaul.Helpers;
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
            Utility.SetGameStarted(false);

            if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(true);
                LightHelpers.SetupLights(PlayerProfileFeaturesPatch.ClonedPlayerModelView);
            }

            Plugin.LogSource.LogDebug("MenuOverhaul: game ended, custom menu re-armed.");
        }
    }
}