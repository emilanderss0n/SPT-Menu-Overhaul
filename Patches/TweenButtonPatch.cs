using DG.Tweening;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class TweenButtonPatch : ModulePatch
    {
        private static FieldInfo _highlightedIconColorField;
        private static FieldInfo _highlightedImageColorField;
        // _highlightedLabelColor is hardcoded in Postfix, so no FieldInfo needed for it.

        protected override MethodBase GetTargetMethod()
        {
            // Cache FieldInfo instances for performance
            _highlightedIconColorField = typeof(DefaultUIButtonAnimation).GetField("_highlightedIconColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _highlightedImageColorField = typeof(DefaultUIButtonAnimation).GetField("_highlightedImageColor", BindingFlags.Instance | BindingFlags.NonPublic);

            if (_highlightedIconColorField == null || _highlightedImageColorField == null)
            {
                Plugin.LogSource.LogError("TweenButtonPatch: Failed to find one or more private fields (_highlightedIconColor, _highlightedImageColor) in DefaultUIButtonAnimation. Patch may not work as expected.");
            }

            var targetMethod = typeof(DefaultUIButtonAnimation).GetMethod("method_2", BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Plugin.LogSource.LogError("TweenButtonPatch: Failed to find target method 'method_2' in DefaultUIButtonAnimation.");
            }
            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            if (!LayoutHelpers.IsPartOfMenuScreen(__instance)) // Ensure this helper is robust
            {
                return;
            }

            __instance.Stop(); // Stop any ongoing animations

            // Retrieve values using cached FieldInfo, with defaults
            Color highlightedIconColor = _highlightedIconColorField != null ? (Color)_highlightedIconColorField.GetValue(__instance) : Color.white;
            Color highlightedImageColor = _highlightedImageColorField != null ? (Color)_highlightedImageColorField.GetValue(__instance) : Color.white;
            Color highlightedLabelColor = new Color(1f, 0.75f, 0.3f, 1f); // Hardcoded as per original logic

            if (__instance.Icon != null)
            {
                __instance.Icon.color = highlightedIconColor;
            }

            if (__instance.Label != null)
            {
                __instance.Label.color = highlightedLabelColor;
            }
            // else { Plugin.LogSource.LogDebug("TweenButtonPatch: Label not found on button."); }

            if (__instance.Image != null)
            {
                if (!animated)
                {
                    __instance.Image.color = highlightedImageColor;
                    if (__instance.Icon != null) // Icon color might also need alpha adjustment here
                    {
                        __instance.Icon.color = highlightedIconColor.SetAlpha(1f); // Ensure full alpha
                    }
                }
                else
                {
                    float imageFadeDuration = 0.2f;
                    float iconFadeDuration = 0.1f;

                    __instance.Image.color = highlightedImageColor.SetAlpha(0f); // Start transparent
                    __instance.ProcessMultipleTweens(new Tween[]
                    {
                        __instance.Image.DOFade(1f, imageFadeDuration)
                    });

                    if (__instance.Icon != null)
                    {
                        // Ensure icon starts transparent if it's meant to fade in with the image, or handle its initial state appropriately.
                        // If Icon should also start transparent:
                        // __instance.Icon.color = highlightedIconColor.SetAlpha(0f);
                        __instance.ProcessTween(__instance.Icon.DOFade(1f, iconFadeDuration), Ease.OutQuad);
                    }
                }
            }
            // else { Plugin.LogSource.LogDebug("TweenButtonPatch: Image not found on button."); }
        }
    }
}