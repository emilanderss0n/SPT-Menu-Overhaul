using EFT;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

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
            Utility.SetGameStarted(true);
            
            if (PlayerProfileFeaturesPatch.clonedPlayerModelView != null)
            {
                PlayerProfileFeaturesPatch.clonedPlayerModelView.SetActive(false);
            }

            MenuOverhaulPatch menuPatch = new MenuOverhaulPatch();
            PlayerProfileFeaturesPatch profilePatch = new PlayerProfileFeaturesPatch();

            menuPatch.CleanupBeforeDisable();
            profilePatch.CleanupBeforeDisable();
            LayoutHelpers.CleanupGameObjects();
            
            menuPatch.Disable();
            new SetAlphaPatch().Disable();
            new TweenButtonPatch().Disable();
            profilePatch.Disable();
            
            LightHelpers.Cleanup();

            Plugin.LogSource.LogDebug("Menu overhaul patches and GameObjects disabled and cleaned up on game start");
        }
    }
}