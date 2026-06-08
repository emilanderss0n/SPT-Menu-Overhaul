using DG.Tweening;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    /// <summary>
    /// Adds polished DOTween-driven motion to the main menu buttons on top of the
    /// colour swap performed by the game's DefaultUIButtonAnimation. Hover nudges
    /// the label inward, smoothly fades it to the accent colour and pops the icon
    /// in; idle reverses everything. Resting transform values are captured once per
    /// button so the animations always run against a stable base, even with rapid
    /// pointer movement.
    /// </summary>
    internal static class ButtonHoverEffects
    {
        private const float HoverDuration = 0.18f;
        private const float IdleDuration = 0.16f;
        private const float LabelSlideDistance = 12f;
        private const float IconPopStartScale = 0.85f;
        private const float IconHoverScaleMultiplier = 1.12f;
        private const float HoverIndicatorDefaultSize = 16f;
        private const float HoverIndicatorSizeMultiplier = 0.35f;
        private const float HoverIndicatorSpacing = 8f;
        private const float HoverIndicatorLeftOffsetFallback = -18f;
        private const float HoverIndicatorSlideDistance = 4f;
        private const float HoverIndicatorStartScale = 0.9f;
        private const string HoverIndicatorObjectName = "MenuHoverIndicator";

        private static readonly string PluginResourcesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MoxoPixel.MenuOverhaul", "Resources");
        private static readonly string HoverIndicatorPathPrimary = Path.Combine(PluginResourcesRoot, "icon", "menu_active.png");
        private static readonly string HoverIndicatorPathFallback = Path.Combine(PluginResourcesRoot, "icons", "menu_active.png");

        private static Sprite _hoverIndicatorSprite;

        private struct ButtonBaseState
        {
            public bool HasLabelRect;
            public Vector2 LabelAnchoredPosition;
            public bool HasIconRect;
            public Vector2 IconAnchoredPosition;
            public Vector3 IconScale;
            public Image HoverIndicatorImage;
            public bool HoverIndicatorVisible;
        }

        private static readonly Dictionary<int, ButtonBaseState> BaseStates = new Dictionary<int, ButtonBaseState>();

        public static void ApplyHover(DefaultUIButtonAnimation instance, Color labelColor, bool animated, bool iconsEnabled)
        {
            if (instance == null) return;

            ButtonBaseState baseState = GetOrCaptureBaseState(instance);
            KillButtonTweens(instance);

            SetLabel(instance, baseState, labelColor, LabelSlideDistance, HoverDuration, Ease.OutCubic, animated);
            SetIconSlide(instance, baseState, LabelSlideDistance, HoverDuration, Ease.OutCubic, animated);
            SetHoverIndicator(instance, ref baseState, true, animated);
            BaseStates[instance.GetInstanceID()] = baseState;

            if (instance.Icon != null && iconsEnabled)
            {
                Transform iconTransform = instance.Icon.transform;
                Vector3 hoverScale = baseState.IconScale * IconHoverScaleMultiplier;
                if (animated)
                {
                    iconTransform.localScale = baseState.IconScale * IconPopStartScale;
                    iconTransform.DOScale(hoverScale, HoverDuration).SetEase(Ease.OutBack);
                }
                else
                {
                    iconTransform.localScale = hoverScale;
                }
            }
        }

        public static void ApplyIdle(DefaultUIButtonAnimation instance, Color labelColor, bool animated, bool iconsEnabled)
        {
            if (instance == null) return;

            ButtonBaseState baseState = GetOrCaptureBaseState(instance);
            KillButtonTweens(instance);

            SetLabel(instance, baseState, labelColor, 0f, IdleDuration, Ease.OutQuad, animated);
            SetIconSlide(instance, baseState, 0f, IdleDuration, Ease.OutQuad, animated);
            SetHoverIndicator(instance, ref baseState, false, animated);
            BaseStates[instance.GetInstanceID()] = baseState;

            if (instance.Icon != null)
            {
                Transform iconTransform = instance.Icon.transform;
                if (animated)
                {
                    iconTransform.DOScale(baseState.IconScale, IdleDuration).SetEase(Ease.OutQuad);
                }
                else
                {
                    iconTransform.localScale = baseState.IconScale;
                }
            }
        }

        private static void SetLabel(DefaultUIButtonAnimation instance, ButtonBaseState baseState, Color labelColor, float slideOffset, float duration, Ease ease, bool animated)
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

        private static void SetIconSlide(DefaultUIButtonAnimation instance, ButtonBaseState baseState, float slideOffset, float duration, Ease ease, bool animated)
        {
            // Only slide the icon separately when it is not parented under the
            // label; otherwise it already rides along with the label slide.
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

        private static void KillButtonTweens(DefaultUIButtonAnimation instance)
        {
            if (instance.Label != null) instance.Label.transform.DOKill();
            if (instance.Icon != null) instance.Icon.transform.DOKill();

            int id = instance.GetInstanceID();
            if (BaseStates.TryGetValue(id, out ButtonBaseState state) && state.HoverIndicatorImage != null)
            {
                state.HoverIndicatorImage.transform.DOKill();
                state.HoverIndicatorImage.DOKill();
            }
        }

        private static void SetHoverIndicator(DefaultUIButtonAnimation instance, ref ButtonBaseState baseState, bool visible, bool animated)
        {
            if (!TryGetOrCreateHoverIndicator(instance, ref baseState, out Image indicatorImage))
            {
                return;
            }

            RectTransform indicatorRect = indicatorImage.transform as RectTransform;
            if (indicatorRect != null)
            {
                ConfigureHoverIndicatorRect(instance, baseState, indicatorRect);
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

                        DOTween.To(() => indicatorRect.anchoredPosition, position => indicatorRect.anchoredPosition = position, targetPosition, HoverDuration * 0.75f)
                            .SetEase(Ease.OutCubic);
                        indicatorRect.DOScale(1f, HoverDuration * 0.75f).SetEase(Ease.OutQuad);
                    }

                    indicatorImage.color = new Color(1f, 1f, 1f, indicatorImage.color.a);
                    indicatorImage.DOFade(1f, HoverDuration * 0.75f).SetEase(Ease.OutCubic);
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
                        DOTween.To(() => indicatorRect.anchoredPosition, position => indicatorRect.anchoredPosition = position, hideTarget, IdleDuration * 0.6f)
                            .SetEase(Ease.OutQuad);
                        indicatorRect.DOScale(HoverIndicatorStartScale, IdleDuration * 0.6f).SetEase(Ease.OutQuad);
                    }

                    indicatorImage.DOFade(0f, IdleDuration * 0.6f)
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

        private static bool TryGetOrCreateHoverIndicator(DefaultUIButtonAnimation instance, ref ButtonBaseState baseState, out Image indicatorImage)
        {
            indicatorImage = baseState.HoverIndicatorImage;
            if (indicatorImage != null)
            {
                return true;
            }

            if (instance.Label == null || !(instance.Label.transform is RectTransform labelRect))
            {
                return false;
            }

            RectTransform indicatorHostRect = GetIndicatorHostRect(instance, labelRect);
            if (indicatorHostRect == null)
            {
                return false;
            }

            Transform existingTransform = indicatorHostRect.Find(HoverIndicatorObjectName);
            if (existingTransform != null)
            {
                indicatorImage = existingTransform.GetComponent<Image>();
                if (indicatorImage != null)
                {
                    baseState.HoverIndicatorImage = indicatorImage;
                    return true;
                }
            }

            Sprite indicatorSprite = GetHoverIndicatorSprite();
            if (indicatorSprite == null)
            {
                return false;
            }

            GameObject indicatorObject = new GameObject(HoverIndicatorObjectName, typeof(RectTransform), typeof(Image));
            indicatorObject.transform.SetParent(indicatorHostRect, false);

            RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.sizeDelta = new Vector2(HoverIndicatorDefaultSize, HoverIndicatorDefaultSize);
            indicatorRect.anchoredPosition = new Vector2(HoverIndicatorLeftOffsetFallback, 0f);

            indicatorImage = indicatorObject.GetComponent<Image>();
            indicatorImage.sprite = indicatorSprite;
            indicatorImage.overrideSprite = indicatorSprite;
            indicatorImage.preserveAspect = true;
            indicatorImage.raycastTarget = false;
            indicatorImage.color = new Color(1f, 1f, 1f, 0f);
            indicatorObject.SetActive(false);

            ConfigureHoverIndicatorRect(instance, baseState, indicatorRect);

            baseState.HoverIndicatorImage = indicatorImage;
            return true;
        }

        private static RectTransform GetIndicatorHostRect(DefaultUIButtonAnimation instance, RectTransform fallbackRect)
        {
            if (instance?.Icon != null && instance.Icon.transform.parent is RectTransform iconParentRect)
            {
                return iconParentRect;
            }

            return fallbackRect;
        }

        private static void ConfigureHoverIndicatorRect(DefaultUIButtonAnimation instance, ButtonBaseState baseState, RectTransform indicatorRect)
        {
            if (instance?.Icon == null || indicatorRect == null)
            {
                return;
            }

            RectTransform iconRect = instance.Icon.transform as RectTransform;
            RectTransform parentRect = indicatorRect.parent as RectTransform;
            if (iconRect == null || parentRect == null)
            {
                return;
            }

            float iconWidth = iconRect.rect.width;
            float iconHeight = iconRect.rect.height;

            float indicatorSize = Mathf.Clamp(Mathf.Min(iconWidth, iconHeight) * HoverIndicatorSizeMultiplier, 12f, 20f);

            Vector2 iconCenterInParent = iconRect.parent == parentRect
                ? iconRect.anchoredPosition
                : (Vector2)parentRect.InverseTransformPoint(iconRect.TransformPoint(iconRect.rect.center));

            float stableIconScaleX = Mathf.Abs(baseState.IconScale.x) > 0.0001f ? Mathf.Abs(baseState.IconScale.x) : Mathf.Abs(iconRect.localScale.x);
            float iconHalfWidth = (iconWidth * 0.5f) * stableIconScaleX;
            float indicatorX = iconCenterInParent.x - iconHalfWidth - (indicatorSize * 0.5f) - HoverIndicatorSpacing;

            indicatorRect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
            indicatorRect.anchoredPosition = new Vector2(indicatorX, iconCenterInParent.y);
        }

        private static Sprite GetHoverIndicatorSprite()
        {
            if (_hoverIndicatorSprite != null)
            {
                return _hoverIndicatorSprite;
            }

            string filePath = File.Exists(HoverIndicatorPathPrimary)
                ? HoverIndicatorPathPrimary
                : (File.Exists(HoverIndicatorPathFallback) ? HoverIndicatorPathFallback : null);

            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(fileBytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.name = Path.GetFileNameWithoutExtension(filePath);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                _hoverIndicatorSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                return _hoverIndicatorSprite;
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Failed to load hover indicator sprite from '{filePath}': {ex.Message}");
                return null;
            }
        }

        private static ButtonBaseState GetOrCaptureBaseState(DefaultUIButtonAnimation instance)
        {
            int id = instance.GetInstanceID();
            if (BaseStates.TryGetValue(id, out ButtonBaseState existing))
            {
                return existing;
            }

            ButtonBaseState state = default;

            if (instance.Label != null && instance.Label.transform is RectTransform labelRect)
            {
                state.HasLabelRect = true;
                state.LabelAnchoredPosition = labelRect.anchoredPosition;
            }

            if (instance.Icon != null)
            {
                state.IconScale = instance.Icon.transform.localScale;
                if (instance.Icon.transform is RectTransform iconRect)
                {
                    state.HasIconRect = true;
                    state.IconAnchoredPosition = iconRect.anchoredPosition;
                }
            }
            else
            {
                state.IconScale = Vector3.one;
            }

            BaseStates[id] = state;
            return state;
        }
    }
}
