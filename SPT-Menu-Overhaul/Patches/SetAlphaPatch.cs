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
        private static FieldInfo _backgroundNormalStateAlphaField;

        protected override MethodBase GetTargetMethod()
        {
            // Cache FieldInfo instances for performance
            _normalIconColorField = typeof(DefaultUIButtonAnimation).GetField("_normalIconColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _normalLabelColorField = typeof(DefaultUIButtonAnimation).GetField("_normalLabelColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _normalImageColorField = typeof(DefaultUIButtonAnimation).GetField("_normalImageColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _backgroundNormalStateAlphaField = typeof(DefaultUIButtonAnimation).GetField("_backgorundNormalStateAplha", BindingFlags.Instance | BindingFlags.NonPublic);

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
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            if (!LayoutHelpers.IsPartOfMenuScreen(__instance))
            {
                return;
            }

            __instance.Stop();

            Color normalIconColor = _normalIconColorField != null ? (Color)_normalIconColorField.GetValue(__instance) : Color.white;
            Color normalLabelColor = _normalLabelColorField != null ? (Color)_normalLabelColorField.GetValue(__instance) : Color.white;
            Color normalImageColor = _normalImageColorField != null ? (Color)_normalImageColorField.GetValue(__instance) : Color.clear;
            float backgroundNormalStateAlpha = _backgroundNormalStateAlphaField != null ? (float)_backgroundNormalStateAlphaField.GetValue(__instance) : 1f;

            if (__instance.Icon != null)
            {
                __instance.Icon.color = normalIconColor.SetAlpha(1f);
            }

            if (__instance.Label != null)
            {
                __instance.Label.color = normalLabelColor;
            }

            if (__instance.Image != null)
            {
                if (!animated)
                {
                    __instance.Image.color = normalImageColor.SetAlpha(backgroundNormalStateAlpha);
                }
                else
                {
                    float duration = 0.15f;
                    __instance.Image.color = normalImageColor.SetAlpha(0f);
                    __instance.ProcessMultipleTweens(new Tween[] { __instance.Image.DOFade(1f, duration) });

                    if (__instance.Icon != null)
                    {
                        __instance.ProcessTween(__instance.Icon.DOFade(1f, duration));
                    }
                }
            }
        }
    }
}
