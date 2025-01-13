using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using Comfort.Common;
using EFT.UI;
using UnityEngine;
using System.Threading.Tasks;

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

            MenuOverhaulPatch.clonedPlayerModelView?.SetActive(true);

            Plugin.LogSource.LogInfo("Patches re-enabled after game session end");
        }
    }
}