using EFT.UI;
using MoxoPixel.MenuOverhaul.Helpers.Buttons;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class ButtonHoverEffects
    {
        private static readonly ButtonHoverStateCache StateCache = new ButtonHoverStateCache();
        private static readonly ButtonHoverIndicatorFactory IndicatorFactory = new ButtonHoverIndicatorFactory();

        public static void ApplyHover(DefaultUIButtonAnimation instance, Color labelColor, bool animated, bool iconsEnabled)
        {
            if (instance == null) return;

            ButtonHoverBaseState baseState = StateCache.GetOrCapture(instance);
            ButtonHoverAnimator.KillButtonTweens(instance, baseState);
            ButtonHoverAnimator.ApplyHover(instance, ref baseState, labelColor, animated, iconsEnabled, IndicatorFactory);
            StateCache.Set(instance, baseState);
        }

        public static void ApplyIdle(DefaultUIButtonAnimation instance, Color labelColor, bool animated, bool iconsEnabled)
        {
            if (instance == null) return;

            ButtonHoverBaseState baseState = StateCache.GetOrCapture(instance);
            ButtonHoverAnimator.KillButtonTweens(instance, baseState);
            ButtonHoverAnimator.ApplyIdle(instance, ref baseState, labelColor, animated, IndicatorFactory);
            StateCache.Set(instance, baseState);
        }
    }
}
