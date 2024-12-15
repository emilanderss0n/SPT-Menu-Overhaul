using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using UnityEngine;

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
            new MenuOverhaulPatch().Enable();
            new SetAlphaPatch().Enable();
            new TweenButtonPatch().Enable();

            MenuScreen menuScreenInstance = GameObject.FindObjectOfType<MenuScreen>();
            if (menuScreenInstance != null)
            {
                MenuOverhaulPatch.ReapplyChanges(menuScreenInstance);
            }
            else
            {
                Plugin.LogSource.LogInfo("MenuScreen instance not found - skipping");
            }

            Plugin.LogSource.LogInfo("Patches re-enabled and changes reapplied after game session end");
        }
    }
}