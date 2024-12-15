using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using Comfort.Common;

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
            Plugin.LogSource.LogInfo("PatchPostfix method called");

            new MenuOverhaulPatch().Disable();
            new SetAlphaPatch().Disable();
            new TweenButtonPatch().Disable();

            MenuOverhaulPatch.DestroyChanges();

            Plugin.LogSource.LogInfo("Patches disabled and changes destroyed on game start");
        }
    }
}