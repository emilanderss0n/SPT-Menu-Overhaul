using DG.Tweening;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class SetAlphaPatch : ModulePatch
    {
        private static FieldInfo _normalIconColorField;
        private static FieldInfo _normalLabelColorField;
        private static FieldInfo _normalImageColorField;
        private static FieldInfo _backgroundNormalStateAlphaField; // Corrected spelling

        protected override MethodBase GetTargetMethod()
        {
            // Cache FieldInfo instances for performance
            _normalIconColorField = typeof(DefaultUIButtonAnimation).GetField("_normalIconColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _normalLabelColorField = typeof(DefaultUIButtonAnimation).GetField("_normalLabelColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _normalImageColorField = typeof(DefaultUIButtonAnimation).GetField("_normalImageColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _backgroundNormalStateAlphaField = typeof(DefaultUIButtonAnimation).GetField("_backgorundNormalStateAplha", BindingFlags.Instance | BindingFlags.NonPublic); // Original spelling

            if (_normalIconColorField == null || _normalLabelColorField == null || _normalImageColorField == null || _backgroundNormalStateAlphaField == null)
            {
                Plugin.LogSource.LogError("SetAlphaPatch: Failed to find one or more private fields in DefaultUIButtonAnimation via reflection. Patch may not work as expected.");
            }
            
            var targetMethod = typeof(DefaultUIButtonAnimation).GetMethod("method_1", BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Plugin.LogSource.LogError("SetAlphaPatch: Failed to find target method 'method_1' in DefaultUIButtonAnimation.");
            }
            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated) // Parameter name __instance is conventional for Harmony patches
        {
            if (!LayoutHelpers.IsPartOfMenuScreen(__instance)) // Assuming LayoutHelpers.IsPartOfMenuScreen is reliable
            {
                return;
            }

            __instance.Stop(); // Stop any ongoing animations

            // Retrieve values using cached FieldInfo
            Color normalIconColor = _normalIconColorField != null ? (Color)_normalIconColorField.GetValue(__instance) : Color.white; // Default if field not found
            Color normalLabelColor = _normalLabelColorField != null ? (Color)_normalLabelColorField.GetValue(__instance) : Color.white;
            Color normalImageColor = _normalImageColorField != null ? (Color)_normalImageColorField.GetValue(__instance) : Color.clear;
            float backgroundNormalStateAlpha = _backgroundNormalStateAlphaField != null ? (float)_backgroundNormalStateAlphaField.GetValue(__instance) : 1f;

            if (__instance.Icon != null)
            {
                __instance.Icon.color = normalIconColor.SetAlpha(1f); // Ensure full alpha for icon
            }

            if (__instance.Label != null)
            {
                __instance.Label.color = normalLabelColor;
            }
            // else { Plugin.LogSource.LogDebug("SetAlphaPatch: Label not found on button."); } // Debug level might be more appropriate

            if (__instance.Image != null) // Ensure Image exists before trying to modify it
            {
                if (!animated)
                {
                    __instance.Image.color = normalImageColor.SetAlpha(backgroundNormalStateAlpha);
                }
                else
                {
                    float duration = 0.15f;
                    __instance.Image.color = normalImageColor.SetAlpha(0f); // Start transparent
                    // Use a new Tween sequence for clarity if multiple tweens are complex
                    // For a single tween, ProcessTween is fine if it exists and works as expected.
                    // If ProcessMultipleTweens is standard, ensure it handles single tweens correctly.
                    __instance.ProcessMultipleTweens(new Tween[] { __instance.Image.DOFade(1f, duration) }); 

                    if (__instance.Icon != null)
                    {
                        // Assuming ProcessTween is a helper in DefaultUIButtonAnimation or its base
                        // If not, __instance.Icon.DOFade(1f, duration).SetEase(Ease.OutQuad); might be more direct
                        __instance.ProcessTween(__instance.Icon.DOFade(1f, duration), Ease.OutQuad); 
                    }
                }
            }
            // else { Plugin.LogSource.LogDebug("SetAlphaPatch: Image not found on button."); }
        }
    }
}
