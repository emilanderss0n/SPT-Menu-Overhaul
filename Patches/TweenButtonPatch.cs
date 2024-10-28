using DG.Tweening;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class TweenButtonPatch : ModulePatch // all patches must inherit ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Target the method_2 method in the DefaultUIButtonAnimation class
            var targetMethod = typeof(DefaultUIButtonAnimation).GetMethod("method_2", BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Plugin.LogSource.LogError("Failed to find method_2 in DefaultUIButtonAnimation.");
            }
            else
            {
                Plugin.LogSource.LogInfo("Successfully found method_2 in DefaultUIButtonAnimation.");
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
            var highlightedIconColor = (Color)typeof(DefaultUIButtonAnimation).GetField("_highlightedIconColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            var highlightedLabelColor = new Color(1f, 0.75f, 0.3f, 1f);
            var highlightedImageColor = (Color)typeof(DefaultUIButtonAnimation).GetField("_highlightedImageColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

            if (__instance.Icon != null)
            {
                __instance.Icon.color = highlightedIconColor;
            }

            if (__instance.Label != null)
            {
                __instance.Label.color = highlightedLabelColor;
            }
            else
            {
                Plugin.LogSource.LogWarning("Label not found.");
            }

            if (!animated)
            {
                __instance.Image.color = highlightedImageColor;
                if (__instance.Icon != null)
                {
                    __instance.Icon.color = highlightedIconColor.SetAlpha(1f); // Ensure the icon color is set correctly
                }
            }
            else
            {
                float num = 0.2f;
                __instance.Image.color = highlightedImageColor.SetAlpha(0f);
                __instance.ProcessMultipleTweens(new Tween[]
                {
                    __instance.Image.DOFade(1f, num)
                });
                num = 0.1f;
                if (__instance.Icon != null)
                {
                    __instance.ProcessTween(__instance.Icon.DOFade(1f, num), Ease.OutQuad); // Set alpha to 1 instead of 0
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