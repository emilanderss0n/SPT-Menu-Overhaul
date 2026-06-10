using EFT.UI;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace MoxoPixel.MenuOverhaul.Helpers.Buttons
{
    internal sealed class ButtonHoverIndicatorFactory
    {
        private const float HoverIndicatorDefaultSize = 16f;
        private const float HoverIndicatorLeftOffsetFallback = -18f;
        private const float MinIndicatorSize = 12f;
        private const float MaxIndicatorSize = 20f;
        private const string HoverIndicatorObjectName = "MenuHoverIndicator";

        private static readonly string PluginResourcesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MoxoPixel.MenuOverhaul", "Resources");
        private static readonly string HoverIndicatorDirectoryPrimary = Path.Combine(PluginResourcesRoot, "icon");
        private static readonly string HoverIndicatorDirectoryFallback = Path.Combine(PluginResourcesRoot, "icons");

        public bool TryGetOrCreate(DefaultUIButtonAnimation instance, ref ButtonHoverBaseState baseState, out Image indicatorImage)
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

            baseState.HoverIndicatorImage = indicatorImage;
            return true;
        }

        public void ConfigureRect(DefaultUIButtonAnimation instance, ButtonHoverBaseState baseState, RectTransform indicatorRect, float indicatorSizeMultiplier, float indicatorSpacing)
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
            float indicatorSize = Mathf.Clamp(Mathf.Min(iconWidth, iconHeight) * indicatorSizeMultiplier, MinIndicatorSize, MaxIndicatorSize);

            Vector2 iconCenterInParent = iconRect.parent == parentRect
                ? iconRect.anchoredPosition
                : (Vector2)parentRect.InverseTransformPoint(iconRect.TransformPoint(iconRect.rect.center));

            float stableIconScaleX = Mathf.Abs(baseState.IconScale.x) > 0.0001f ? Mathf.Abs(baseState.IconScale.x) : Mathf.Abs(iconRect.localScale.x);
            float iconHalfWidth = (iconWidth * 0.5f) * stableIconScaleX;
            float indicatorX = iconCenterInParent.x - iconHalfWidth - (indicatorSize * 0.5f) - indicatorSpacing;

            indicatorRect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
            indicatorRect.anchoredPosition = new Vector2(indicatorX, iconCenterInParent.y);
        }

        private static RectTransform GetIndicatorHostRect(DefaultUIButtonAnimation instance, RectTransform fallbackRect)
        {
            if (instance?.Icon != null && instance.Icon.transform.parent is RectTransform iconParentRect)
            {
                return iconParentRect;
            }

            return fallbackRect;
        }

        private static Sprite GetHoverIndicatorSprite()
        {
            Sprite sprite = UIAssetLoader.LoadSpriteFromDirectory(HoverIndicatorDirectoryPrimary, "menu_active");
            if (sprite != null)
            {
                return sprite;
            }

            return UIAssetLoader.LoadSpriteFromDirectory(HoverIndicatorDirectoryFallback, "menu_active");
        }
    }
}
