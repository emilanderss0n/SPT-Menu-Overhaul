using DG.Tweening;
using EFT.UI;
using MoxoPixel.MenuOverhaul.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MoxoPixel.MenuOverhaul.Helpers.Buttons
{
    internal static class ButtonHoverAnimator
    {
        private const float NormalHoverDuration = 0.18f;
        private const float NormalIdleDuration = 0.16f;
        private const float FastHoverDuration = 0.14f;
        private const float FastIdleDuration = 0.12f;
        private const float SlowHoverDuration = 0.24f;
        private const float SlowIdleDuration = 0.22f;
        private const float IconPopStartScale = 0.85f;
        private const float HoverIndicatorSlideDistance = 4f;
        private const float HoverIndicatorStartScale = 0.9f;

        public static void ApplyHover(DefaultUIButtonAnimation instance, ref ButtonHoverBaseState baseState, Color labelColor, bool animated, bool iconsEnabled, ButtonHoverIndicatorFactory indicatorFactory)
        {
            float labelSlideDistance = Mathf.Clamp(Settings.HoverLabelSlideDistance.Value, 0f, 40f);
            float iconHoverScaleMultiplier = Mathf.Clamp(Settings.HoverIconScaleMultiplier.Value, 0.5f, 2f);
            float indicatorSizeMultiplier = Mathf.Clamp(Settings.HoverIndicatorSizeMultiplier.Value, 0.1f, 1.5f);
            float indicatorSpacing = Mathf.Clamp(Settings.HoverIndicatorSpacing.Value, 0f, 32f);

            GetAnimationDurations(out float hoverDuration, out _);

            SetLabel(instance, baseState, labelColor, labelSlideDistance, hoverDuration, Ease.OutCubic, animated);
            SetIconSlide(instance, baseState, labelSlideDistance, hoverDuration, Ease.OutCubic, animated);
            SetHoverIndicator(instance, ref baseState, true, hoverDuration, indicatorSizeMultiplier, indicatorSpacing, animated, indicatorFactory);

            if (instance.Icon != null && iconsEnabled)
            {
                Transform iconTransform = instance.Icon.transform;
                Vector3 hoverScale = baseState.IconScale * iconHoverScaleMultiplier;
                if (animated)
                {
                    iconTransform.localScale = baseState.IconScale * IconPopStartScale;
                    iconTransform.DOScale(hoverScale, hoverDuration).SetEase(Ease.OutBack);
                }
                else
                {
                    iconTransform.localScale = hoverScale;
                }
            }
        }

        public static void ApplyIdle(DefaultUIButtonAnimation instance, ref ButtonHoverBaseState baseState, Color labelColor, bool animated, ButtonHoverIndicatorFactory indicatorFactory)
        {
            float indicatorSizeMultiplier = Mathf.Clamp(Settings.HoverIndicatorSizeMultiplier.Value, 0.1f, 1.5f);
            float indicatorSpacing = Mathf.Clamp(Settings.HoverIndicatorSpacing.Value, 0f, 32f);

            GetAnimationDurations(out _, out float idleDuration);

            SetLabel(instance, baseState, labelColor, 0f, idleDuration, Ease.OutQuad, animated);
            SetIconSlide(instance, baseState, 0f, idleDuration, Ease.OutQuad, animated);
            SetHoverIndicator(instance, ref baseState, false, idleDuration, indicatorSizeMultiplier, indicatorSpacing, animated, indicatorFactory);

            if (instance.Icon != null)
            {
                Transform iconTransform = instance.Icon.transform;
                if (animated)
                {
                    iconTransform.DOScale(baseState.IconScale, idleDuration).SetEase(Ease.OutQuad);
                }
                else
                {
                    iconTransform.localScale = baseState.IconScale;
                }
            }
        }

        public static void KillButtonTweens(DefaultUIButtonAnimation instance, ButtonHoverBaseState state)
        {
            if (instance.Label != null) instance.Label.transform.DOKill();
            if (instance.Icon != null) instance.Icon.transform.DOKill();

            if (state.HoverIndicatorImage != null)
            {
                state.HoverIndicatorImage.transform.DOKill();
                state.HoverIndicatorImage.DOKill();
            }
        }

        private static void SetLabel(DefaultUIButtonAnimation instance, ButtonHoverBaseState baseState, Color labelColor, float slideOffset, float duration, Ease ease, bool animated)
        {
            if (instance.Label == null) return;

            Transform labelTransform = instance.Label.transform;
            RectTransform labelRect = labelTransform as RectTransform;
            Vector2 targetPosition = baseState.LabelAnchoredPosition + new Vector2(slideOffset, 0f);

            if (animated)
            {
                DOTween.To(() => instance.Label.color, color => instance.Label.color = color, labelColor, duration)
                    .SetTarget(labelTransform)
                    .SetEase(Ease.OutQuad);

                if (baseState.HasLabelRect && labelRect != null)
                {
                    DOTween.To(() => labelRect.anchoredPosition, position => labelRect.anchoredPosition = position, targetPosition, duration)
                        .SetTarget(labelTransform)
                        .SetEase(ease);
                }
            }
            else
            {
                instance.Label.color = labelColor;
                if (baseState.HasLabelRect && labelRect != null)
                {
                    labelRect.anchoredPosition = targetPosition;
                }
            }
        }

        private static void SetIconSlide(DefaultUIButtonAnimation instance, ButtonHoverBaseState baseState, float slideOffset, float duration, Ease ease, bool animated)
        {
            if (instance.Icon == null || instance.Label == null) return;
            if (!baseState.HasIconRect) return;
            if (instance.Icon.transform.IsChildOf(instance.Label.transform)) return;

            RectTransform iconRect = instance.Icon.transform as RectTransform;
            if (iconRect == null) return;

            Vector2 targetPosition = baseState.IconAnchoredPosition + new Vector2(slideOffset, 0f);
            if (animated)
            {
                DOTween.To(() => iconRect.anchoredPosition, position => iconRect.anchoredPosition = position, targetPosition, duration)
                    .SetTarget(instance.Icon.transform)
                    .SetEase(ease);
            }
            else
            {
                iconRect.anchoredPosition = targetPosition;
            }
        }

        private static void SetHoverIndicator(DefaultUIButtonAnimation instance, ref ButtonHoverBaseState baseState, bool visible, float duration, float indicatorSizeMultiplier, float indicatorSpacing, bool animated, ButtonHoverIndicatorFactory indicatorFactory)
        {
            if (!indicatorFactory.TryGetOrCreate(instance, ref baseState, out Image indicatorImage))
            {
                return;
            }

            RectTransform indicatorRect = indicatorImage.transform as RectTransform;
            if (indicatorRect != null)
            {
                indicatorFactory.ConfigureRect(instance, baseState, indicatorRect, indicatorSizeMultiplier, indicatorSpacing);
            }

            if (visible == baseState.HoverIndicatorVisible)
            {
                if (visible)
                {
                    indicatorImage.gameObject.SetActive(true);
                }
                return;
            }

            indicatorImage.transform.DOKill();
            indicatorImage.DOKill();

            Color visibleColor = Color.white;
            Color hiddenColor = new Color(1f, 1f, 1f, 0f);

            if (visible)
            {
                indicatorImage.gameObject.SetActive(true);
                if (animated)
                {
                    Vector2 targetPosition = indicatorRect != null ? indicatorRect.anchoredPosition : Vector2.zero;
                    if (indicatorRect != null)
                    {
                        indicatorRect.anchoredPosition = targetPosition + new Vector2(-HoverIndicatorSlideDistance, 0f);
                        indicatorRect.localScale = Vector3.one * HoverIndicatorStartScale;

                        DOTween.To(() => indicatorRect.anchoredPosition, position => indicatorRect.anchoredPosition = position, targetPosition, duration * 0.75f)
                            .SetEase(Ease.OutCubic);
                        indicatorRect.DOScale(1f, duration * 0.75f).SetEase(Ease.OutQuad);
                    }

                    indicatorImage.color = new Color(1f, 1f, 1f, indicatorImage.color.a);
                    indicatorImage.DOFade(1f, duration * 0.75f).SetEase(Ease.OutCubic);
                }
                else
                {
                    indicatorImage.color = visibleColor;
                    if (indicatorRect != null)
                    {
                        indicatorRect.localScale = Vector3.one;
                    }
                }
            }
            else
            {
                if (animated)
                {
                    if (indicatorRect != null)
                    {
                        Vector2 hideTarget = indicatorRect.anchoredPosition + new Vector2(-HoverIndicatorSlideDistance * 0.6f, 0f);
                        DOTween.To(() => indicatorRect.anchoredPosition, position => indicatorRect.anchoredPosition = position, hideTarget, duration * 0.6f)
                            .SetEase(Ease.OutQuad);
                        indicatorRect.DOScale(HoverIndicatorStartScale, duration * 0.6f).SetEase(Ease.OutQuad);
                    }

                    indicatorImage.DOFade(0f, duration * 0.6f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            if (indicatorImage != null)
                            {
                                indicatorImage.gameObject.SetActive(false);
                            }
                        });
                }
                else
                {
                    indicatorImage.color = hiddenColor;
                    indicatorImage.gameObject.SetActive(false);
                }
            }

            baseState.HoverIndicatorVisible = visible;
        }

        private static void GetAnimationDurations(out float hoverDuration, out float idleDuration)
        {
            string preset = Settings.HoverAnimationDurationPreset.Value;
            switch (preset)
            {
                case "Fast":
                    hoverDuration = FastHoverDuration;
                    idleDuration = FastIdleDuration;
                    break;
                case "Slow":
                    hoverDuration = SlowHoverDuration;
                    idleDuration = SlowIdleDuration;
                    break;
                default:
                    hoverDuration = NormalHoverDuration;
                    idleDuration = NormalIdleDuration;
                    break;
            }
        }
    }
}
