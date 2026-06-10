using DG.Tweening;
using EFT.UI;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Infrastructure.Reflection;
using MoxoPixel.MenuOverhaul.Utils;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Helpers.Services
{
    internal static class MainMenuButtonAnimationService
    {
        public static void ApplyIdle(DefaultUIButtonAnimation instance, bool animated)
        {
            if (!MenuGuardHelpers.CanApplyButtonAnimation(instance))
            {
                return;
            }

            EnsureReflectionInitialized();

            instance.Stop();

            Color normalIconColor = ReadColor(EftReflectionMap.NormalIconColorField, EftReflectionMap.NormalIconColorFieldKey, instance, Color.white);
            Color normalLabelColor = ReadColor(EftReflectionMap.NormalLabelColorField, EftReflectionMap.NormalLabelColorFieldKey, instance, Color.white);
            Color normalImageColor = ReadColor(EftReflectionMap.NormalImageColorField, EftReflectionMap.NormalImageColorFieldKey, instance, Color.clear);
            float backgroundNormalStateAlpha = ReadFloat(EftReflectionMap.BackgroundNormalStateAlphaField, EftReflectionMap.BackgroundNormalStateAlphaFieldKey, instance, 1f);

            bool iconsEnabled = Settings.EnableMenuButtonIcons.Value;
            if (instance.Icon != null)
            {
                instance.Icon.gameObject.SetActive(iconsEnabled);
                if (iconsEnabled)
                {
                    instance.Icon.color = WithAlpha(normalIconColor, 1f);
                }
            }

            ButtonHoverEffects.ApplyIdle(instance, normalLabelColor, animated, iconsEnabled);

            if (instance.Image == null)
            {
                return;
            }

            if (!animated)
            {
                instance.Image.color = WithAlpha(normalImageColor, backgroundNormalStateAlpha);
                return;
            }

            const float duration = 0.15f;
            instance.Image.color = WithAlpha(normalImageColor, 0f);
            instance.ProcessMultipleTweens(new Tween[] { instance.Image.DOFade(1f, duration) });

            if (instance.Icon != null && iconsEnabled)
            {
                instance.ProcessTween(instance.Icon.DOFade(1f, duration));
            }
        }

        public static void ApplyHover(DefaultUIButtonAnimation instance, bool animated)
        {
            if (!MenuGuardHelpers.CanApplyButtonAnimation(instance))
            {
                return;
            }

            EnsureReflectionInitialized();

            instance.Stop();

            Color highlightedIconColor = ReadColor(EftReflectionMap.HighlightedIconColorField, EftReflectionMap.HighlightedIconColorFieldKey, instance, Color.white);
            Color highlightedImageColor = ReadColor(EftReflectionMap.HighlightedImageColorField, EftReflectionMap.HighlightedImageColorFieldKey, instance, Color.white);
            Color highlightedLabelColor = Settings.AccentColor.Value;

            bool iconsEnabled = Settings.EnableMenuButtonIcons.Value;
            if (instance.Icon != null)
            {
                instance.Icon.gameObject.SetActive(iconsEnabled);
                if (iconsEnabled)
                {
                    instance.Icon.color = highlightedIconColor;
                }
            }

            ButtonHoverEffects.ApplyHover(instance, highlightedLabelColor, animated, iconsEnabled);

            if (instance.Image == null)
            {
                return;
            }

            if (!animated)
            {
                instance.Image.color = highlightedImageColor;
                if (instance.Icon != null && iconsEnabled)
                {
                    instance.Icon.color = WithAlpha(highlightedIconColor, 1f);
                }
                return;
            }

            const float imageFadeDuration = 0.2f;
            const float iconFadeDuration = 0.1f;

            instance.Image.color = WithAlpha(highlightedImageColor, 0f);
            instance.ProcessMultipleTweens(new Tween[] { instance.Image.DOFade(1f, imageFadeDuration) });

            if (instance.Icon != null && iconsEnabled)
            {
                instance.ProcessTween(instance.Icon.DOFade(1f, iconFadeDuration));
            }
        }

        private static void EnsureReflectionInitialized()
        {
            if (!EftReflectionMap.IsInitialized)
            {
                EftReflectionMap.Initialize();
            }
        }

        private static Color ReadColor(System.Reflection.FieldInfo field, string key, DefaultUIButtonAnimation instance, Color fallback)
        {
            EftReflectionMap.WarnIfMissing(field, key);
            return field != null ? (Color)field.GetValue(instance) : fallback;
        }

        private static float ReadFloat(System.Reflection.FieldInfo field, string key, DefaultUIButtonAnimation instance, float fallback)
        {
            EftReflectionMap.WarnIfMissing(field, key);
            return field != null ? (float)field.GetValue(instance) : fallback;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
