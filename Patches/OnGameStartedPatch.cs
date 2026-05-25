using EFT;
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
            Utility.SetGameStarted(true);

            if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(false);
            }

            // Hide the decal plane explicitly while in a raid.
            Utility.ConfigureDecalPlane(false);

            Plugin.LogSource.LogDebug("MenuOverhaul: game started, custom menu suspended.");
        }
    }
}