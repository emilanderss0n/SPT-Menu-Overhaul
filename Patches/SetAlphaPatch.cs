using DG.Tweening;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;

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
            
            var targetMethod = typeof(DefaultUIButtonAnimation).GetMethod(MenuOverhaulConstants.Reflection.DefaultButtonIdleMethod, BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Plugin.LogSource.LogError($"SetAlphaPatch: Failed to find target method '{MenuOverhaulConstants.Reflection.DefaultButtonIdleMethod}' in DefaultUIButtonAnimation.");
            }
            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            // Only restyle buttons on the modded menu. The same MenuScreen
            // GameObject is reused for the in-raid ESC menu (GClass3880) and the
            // reconnect menu (GClass3879), so IsPartOfMenuScreen alone matches
            // those too.
            if (!MenuVisibilityController.IsMainMenuActive)
            {
                return;
            }

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
                bool showIcons = MoxoPixel.MenuOverhaul.Utils.Settings.EnableMenuButtonIcons.Value;
                __instance.Icon.gameObject.SetActive(showIcons);
                if (showIcons)
                {
                    __instance.Icon.color = normalIconColor.SetAlpha(1f);
                }
            }

            // Reverse the hover motion (label colour/slide + icon pop) so the
            // sophisticated hover effect cleanly settles back to rest.
            ButtonHoverEffects.ApplyIdle(__instance, normalLabelColor, animated, MoxoPixel.MenuOverhaul.Utils.Settings.EnableMenuButtonIcons.Value);

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

                    if (__instance.Icon != null && MoxoPixel.MenuOverhaul.Utils.Settings.EnableMenuButtonIcons.Value)
                    {
                        __instance.ProcessTween(__instance.Icon.DOFade(1f, duration));
                    }
                }
            }
        }
    }
}
