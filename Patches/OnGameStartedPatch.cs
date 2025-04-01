using EFT;
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
        private static void PatchPostfix(GameWorld __instance)
        {
            MenuOverhaulPatch.clonedPlayerModelView?.SetActive(false);

            new MenuOverhaulPatch().Disable();
            new SetAlphaPatch().Disable();
            new TweenButtonPatch().Disable();

            Plugin.LogSource.LogInfo("Patches disabled and changes destroyed on game start");
        }
    }
}