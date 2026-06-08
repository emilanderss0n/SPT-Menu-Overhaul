using DG.Tweening;
using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class TweenButtonPatch : ModulePatch
    {
        private static FieldInfo _highlightedIconColorField;
        private static FieldInfo _highlightedImageColorField;

        protected override MethodBase GetTargetMethod()
        {
            _highlightedIconColorField = typeof(DefaultUIButtonAnimation).GetField("_highlightedIconColor", BindingFlags.Instance | BindingFlags.NonPublic);
            _highlightedImageColorField = typeof(DefaultUIButtonAnimation).GetField("_highlightedImageColor", BindingFlags.Instance | BindingFlags.NonPublic);

            if (_highlightedIconColorField == null || _highlightedImageColorField == null)
            {
                Plugin.LogSource.LogError("TweenButtonPatch: Failed to find one or more private fields (_highlightedIconColor, _highlightedImageColor) in DefaultUIButtonAnimation. Patch may not work as expected.");
            }

            var targetMethod = typeof(DefaultUIButtonAnimation).GetMethod(MenuOverhaulConstants.Reflection.DefaultButtonHighlightedMethod, BindingFlags.Instance | BindingFlags.Public);
            if (targetMethod == null)
            {
                Plugin.LogSource.LogError($"TweenButtonPatch: Failed to find target method '{MenuOverhaulConstants.Reflection.DefaultButtonHighlightedMethod}' in DefaultUIButtonAnimation.");
            }
            return targetMethod;
        }

        [PatchPostfix]
        private static void Postfix(DefaultUIButtonAnimation __instance, bool animated)
        {
            // See SetAlphaPatch: the MenuScreen GameObject is reused by the
            // in-raid ESC menu (GClass3880) and reconnect menu (GClass3879),
            // so we must also gate on the active EFT screen identity.
            if (!MenuVisibilityController.IsMainMenuActive)
            {
                return;
            }

            if (!LayoutHelpers.IsPartOfMenuScreen(__instance))
            {
                return;
            }

            __instance.Stop();

            Color highlightedIconColor = _highlightedIconColorField != null ? (Color)_highlightedIconColorField.GetValue(__instance) : Color.white;
            Color highlightedImageColor = _highlightedImageColorField != null ? (Color)_highlightedImageColorField.GetValue(__instance) : Color.white;
            // Use the shared accent color from settings
            Color highlightedLabelColor = Settings.AccentColor.Value;

            if (__instance.Icon != null)
            {
                bool showIcons = Settings.EnableMenuButtonIcons.Value;
                __instance.Icon.gameObject.SetActive(showIcons);
                if (showIcons)
                {
                    __instance.Icon.color = highlightedIconColor;
                }
            }

            // Smoothly fade/slide the label to the accent colour and pop the icon
            // in, instead of an instant colour swap.
            ButtonHoverEffects.ApplyHover(__instance, highlightedLabelColor, animated, Settings.EnableMenuButtonIcons.Value);

            if (__instance.Image != null)
            {
                if (!animated)
                {
                    __instance.Image.color = highlightedImageColor;
                    if (__instance.Icon != null && Settings.EnableMenuButtonIcons.Value)
                    {
                        __instance.Icon.color = highlightedIconColor.SetAlpha(1f);
                    }
                }
                else
                {
                    float imageFadeDuration = 0.2f;
                    float iconFadeDuration = 0.1f;

                    __instance.Image.color = highlightedImageColor.SetAlpha(0f);
                    __instance.ProcessMultipleTweens(new Tween[]
                    {
                        __instance.Image.DOFade(1f, imageFadeDuration)
                    });

                    if (__instance.Icon != null && Settings.EnableMenuButtonIcons.Value)
                    {
                        __instance.ProcessTween(__instance.Icon.DOFade(1f, iconFadeDuration));
                    }
                }
            }
        }
    }
}