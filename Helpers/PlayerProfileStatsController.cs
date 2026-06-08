using System.Globalization;
using System;
using EFT;
using SPT.Reflection.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    internal static class PlayerProfileStatsController
    {
        public static void UpdateTextColors(GameObject clonedPlayerModelView)
        {
            Transform bottomFieldTransform = PlayerProfileTransformController.GetBottomFieldTransform(clonedPlayerModelView);
            if (bottomFieldTransform == null)
            {
                return;
            }

            Color accentColor = Settings.AccentColor.Value;

            Transform nicknameTransform = bottomFieldTransform.Find(MenuOverhaulConstants.BottomFieldUi.NicknameText);
            TextMeshProUGUI nicknameTMP = nicknameTransform != null ? nicknameTransform.GetComponent<TextMeshProUGUI>() : null;
            if (nicknameTMP != null)
            {
                nicknameTMP.color = accentColor;
            }

            Transform experienceRow = bottomFieldTransform.Find(MenuOverhaulConstants.BottomFieldUi.ExperienceRow);
            if (experienceRow != null)
            {
                Transform expValueTransform = experienceRow.Find(MenuOverhaulConstants.BottomFieldUi.ExpValue);
                TextMeshProUGUI expValueTMP = expValueTransform != null ? expValueTransform.GetComponent<TextMeshProUGUI>() : null;
                if (expValueTMP != null)
                {
                    expValueTMP.color = accentColor;
                }
            }
        }

        public static void UpdatePlayerStats(GameObject clonedPlayerModelView, Action<Image, int> setLevelIcon)
        {
            Transform bottomFieldTransform = PlayerProfileTransformController.GetBottomFieldTransform(clonedPlayerModelView);
            if (bottomFieldTransform == null)
            {
                Plugin.LogSource.LogWarning("UpdatePlayerStats - bottomFieldTransform is null.");
                return;
            }

            Profile profile = null;
            if (PatchConstants.BackEndSession != null)
            {
                profile = PatchConstants.BackEndSession.Profile;
            }
            if (profile == null)
            {
                Plugin.LogSource.LogWarning("UpdatePlayerStats - BackEndSession.Profile is null. Cannot update stats.");
                return;
            }

            UpdateNicknameDisplay(bottomFieldTransform, profile);
            UpdateExperienceDisplay(bottomFieldTransform, profile);
            UpdateLevelDisplay(bottomFieldTransform, profile, setLevelIcon);
            UpdateTextColors(clonedPlayerModelView);
        }

        private static void UpdateNicknameDisplay(Transform bottomField, Profile profile)
        {
            Transform nicknameTransform = bottomField.Find(MenuOverhaulConstants.BottomFieldUi.NicknameText);
            TextMeshProUGUI nicknameTMP = nicknameTransform != null ? nicknameTransform.GetComponent<TextMeshProUGUI>() : null;
            if (nicknameTMP != null)
            {
                nicknameTMP.text = profile.Nickname;
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateNicknameDisplay - NicknameText TMP component not found in BottomField.");
            }

            Transform originalNicknameAndKarma = bottomField.Find(MenuOverhaulConstants.BottomFieldUi.NicknameAndKarma);
            if (originalNicknameAndKarma != null)
            {
                originalNicknameAndKarma.gameObject.SetActive(false);
            }
        }

        private static void UpdateExperienceDisplay(Transform bottomField, Profile profile)
        {
            Transform experienceRow = bottomField.Find(MenuOverhaulConstants.BottomFieldUi.ExperienceRow);
            if (experienceRow == null)
            {
                Plugin.LogSource.LogWarning("UpdateExperienceDisplay - ExperienceRow not found in BottomField.");
                return;
            }

            Transform expValueTransform = experienceRow.Find(MenuOverhaulConstants.BottomFieldUi.ExpValue);
            TextMeshProUGUI experienceTMP = expValueTransform != null ? expValueTransform.GetComponent<TextMeshProUGUI>() : null;
            if (experienceTMP != null)
            {
                var numberFormat = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
                experienceTMP.text = profile.Experience.ToString("N", numberFormat);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateExperienceDisplay - ExpValue TMP component not found in ExperienceRow.");
            }
        }

        private static void UpdateLevelDisplay(Transform bottomField, Profile profile, Action<Image, int> setLevelIcon)
        {
            Transform levelInfoRow = bottomField.Find(MenuOverhaulConstants.BottomFieldUi.LevelInfoRow);
            if (levelInfoRow == null)
            {
                Plugin.LogSource.LogWarning("UpdateLevelDisplay - LevelInfoRow transform not found in BottomField.");
                return;
            }

            if (profile.Info == null)
            {
                Plugin.LogSource.LogWarning("UpdateLevelDisplay - Profile.Info is null.");
                return;
            }

            Transform levelTransform = levelInfoRow.Find(MenuOverhaulConstants.BottomFieldUi.Level);
            TextMeshProUGUI levelTMP = levelTransform != null ? levelTransform.GetComponent<TextMeshProUGUI>() : null;
            if (levelTMP != null)
            {
                levelTMP.alignment = TextAlignmentOptions.Right;
                levelTMP.margin = Vector4.zero;
                levelTMP.lineSpacing = 0;
                levelTMP.fontSize = 54f;
                levelTMP.enableWordWrapping = false;
                levelTMP.text = profile.Info.Level.ToString();
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateLevelDisplay - Level TMP component not found in LevelInfoRow.");
            }

            Transform iconTransform = levelInfoRow.Find(MenuOverhaulConstants.BottomFieldUi.LevelIcon);
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    if (setLevelIcon != null)
                    {
                        setLevelIcon.Invoke(iconImage, profile.Info.Level);
                    }
                    iconImage.preserveAspect = true;
                }

                RectTransform iconRect = iconTransform.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(56f, 56f);
                    iconRect.offsetMax = new Vector2(-5f, iconRect.offsetMax.y);
                }

                LayoutElement iconLe = iconTransform.GetComponent<LayoutElement>();
                if (iconLe != null)
                {
                    iconLe.minWidth = 56f;
                    iconLe.minHeight = 56f;
                    iconLe.preferredWidth = 56f;
                    iconLe.preferredHeight = 56f;
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateLevelDisplay - Level Icon transform not found in LevelInfoRow.");
            }

            HorizontalLayoutGroup hlg = levelInfoRow.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                if (!hlg.reverseArrangement)
                {
                    hlg.reverseArrangement = true;
                }

                hlg.spacing = 0f;
            }
        }
    }
}
