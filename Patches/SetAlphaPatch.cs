using DG.Tweening;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class SetAlphaPatch : ModulePatch // all patches must inherit ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Target the method_1 method in the DefaultUIButtonAnimation class
            var targetMethod = typeof(DefaultUIButtonAnimation).GetMethod("method_1", BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Plugin.LogSource.LogError("Failed to find method_1 in DefaultUIButtonAnimation.");
            }
            else
            {
                Plugin.LogSource.LogInfo("Successfully found method_1 in DefaultUIButtonAnimation.");
            }
            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            // Check if the button is part of the MenuScreen
            if (!IsPartOfMenuScreen(__instance))
            {
                return;
            }

            __instance.Stop();

            // Use reflection to access the protected fields
            var normalIconColor = (Color)typeof(DefaultUIButtonAnimation).GetField("_normalIconColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            var normalLabelColor = (Color)typeof(DefaultUIButtonAnimation).GetField("_normalLabelColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            var normalImageColor = (Color)typeof(DefaultUIButtonAnimation).GetField("_normalImageColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            var backgroundNormalStateAlpha = (float)typeof(DefaultUIButtonAnimation).GetField("_backgorundNormalStateAplha", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            if (__instance.Icon != null)
            {
                __instance.Icon.color = normalIconColor.SetAlpha(1f); // Ensure the icon color is set correctly
            }

            if (__instance.Label != null)
            {
                __instance.Label.color = normalLabelColor;
            }
            else
            {
                Plugin.LogSource.LogWarning("Label not found.");
            }

            if (!animated)
            {
                __instance.Image.color = normalImageColor.SetAlpha(backgroundNormalStateAlpha);
            }
            else
            {
                float num = 0.15f;
                __instance.Image.color = normalImageColor.SetAlpha(0f);
                __instance.ProcessMultipleTweens(new Tween[]
                {
                    __instance.Image.DOFade(1f, num)
                });
                if (__instance.Icon != null)
                {
                    __instance.ProcessTween(__instance.Icon.DOFade(1f, num), Ease.OutQuad); // Set alpha to 1
                }
            }
        }

        private static bool IsPartOfMenuScreen(DefaultUIButtonAnimation buttonAnimation)
        {
            Transform currentTransform = buttonAnimation.transform;
            while (currentTransform != null)
            {
                if (currentTransform.name == "MenuScreen")
                {
                    return true;
                }
                currentTransform = currentTransform.parent;
            }
            return false;
        }
    }
}
