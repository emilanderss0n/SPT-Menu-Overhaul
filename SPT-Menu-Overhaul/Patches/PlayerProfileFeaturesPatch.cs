using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;
using EFT;
using SPT.Reflection.Utils;
using Object = UnityEngine.Object;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class PlayerProfileFeaturesPatch : ModulePatch
    {
        private static bool menuPlayerCreated;
        public static GameObject ClonedPlayerModelView;

        private static bool _profileSettingsSubscribed;
        private static bool _experienceEventsSubscribed;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("Show", [typeof(Profile), typeof(MatchmakerPlayerControllerClass), typeof(ESessionMode)
            ]);
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance)
        {
            try
            {
                if (__instance == null)
                {
                    Plugin.LogSource.LogWarning("MenuScreen instance is null.");
                    return;
                }

                await AddPlayerModel().ConfigureAwait(false);
                SubscribeToProfileSettingsChanges();
                SubscribeToCharacterLevelUpEvent();

                if (ClonedPlayerModelView != null)
                {
                    UpdatePlayerModelPosition();
                    UpdatePlayerModelRotation();
                    UpdateCameraPosition();
                    BottomFieldPositionChanged();
                    UpdateTextColors();
                }
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError(e.ToString());
            }
        }

        private static void SubscribeToProfileSettingsChanges()
        {
            if (_profileSettingsSubscribed) return;

            Settings.PositionPlayerModelHorizontal.SettingChanged += OnPlayerModelPositionChanged;
            Settings.PositionBottomFieldHorizontal.SettingChanged += OnBottomFieldPositionChanged;
            Settings.PositionBottomFieldVertical.SettingChanged += OnBottomFieldPositionChanged;
            Settings.RotationPlayerModelHorizontal.SettingChanged += OnPlayerModelRotationChanged;
            Settings.AccentColor.SettingChanged += OnAccentColorChanged;
            Settings.EnableLargerPlayerModel.SettingChanged += OnLargerPlayerModelChanged;

            _profileSettingsSubscribed = true;
        }

        private static void UnsubscribeFromProfileSettingsChanges()
        {
            if (!_profileSettingsSubscribed) return;

            Settings.PositionPlayerModelHorizontal.SettingChanged -= OnPlayerModelPositionChanged;
            Settings.PositionBottomFieldHorizontal.SettingChanged -= OnBottomFieldPositionChanged;
            Settings.PositionBottomFieldVertical.SettingChanged -= OnBottomFieldPositionChanged;
            Settings.RotationPlayerModelHorizontal.SettingChanged -= OnPlayerModelRotationChanged;
            Settings.AccentColor.SettingChanged -= OnAccentColorChanged;
            Settings.EnableLargerPlayerModel.SettingChanged -= OnLargerPlayerModelChanged;

            _profileSettingsSubscribed = false;
        }

        private static void OnPlayerModelPositionChanged(object sender, EventArgs e) => UpdatePlayerModelPosition();
        private static void OnPlayerModelRotationChanged(object sender, EventArgs e) => UpdatePlayerModelRotation();
        private static void OnBottomFieldPositionChanged(object sender, EventArgs e) => BottomFieldPositionChanged();
        private static void OnAccentColorChanged(object sender, EventArgs e)
        {
            UpdateTextColors();
            LightHelpers.UpdateAccentLightColor();
        }
        private static void OnLargerPlayerModelChanged(object sender, EventArgs e)
        {
            UpdatePlayerModelPosition();
            UpdateCameraPosition();
        }

        private static void UpdateTextColors()
        {
            Transform bottomFieldTransform = GetBottomFieldTransform();
            if (bottomFieldTransform == null) return;

            Color accentColor = Settings.AccentColor.Value;

            TextMeshProUGUI nicknameTMP = bottomFieldTransform.Find("NicknameText")?.GetComponent<TextMeshProUGUI>();
            if (nicknameTMP != null)
            {
                nicknameTMP.color = accentColor;
            }

            Transform experienceRow = bottomFieldTransform.Find("ExperienceRow");
            if (experienceRow != null)
            {
                TextMeshProUGUI expValueTMP = experienceRow.Find("ExpValue")?.GetComponent<TextMeshProUGUI>();
                if (expValueTMP != null)
                {
                    expValueTMP.color = accentColor;
                }
            }
        }

        private static void UpdatePlayerModelPosition()
        {
            if (ClonedPlayerModelView != null)
            {
                ClonedPlayerModelView.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -150f, 0f);
                
                if (Settings.EnableLargerPlayerModel.Value)
                {
                    RectTransform rectTransform = ClonedPlayerModelView.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float horizontalPos = Settings.PositionPlayerModelHorizontal.Value;
                        rectTransform.anchoredPosition = new Vector2(horizontalPos, 0f);
                        rectTransform.offsetMax = new Vector2(horizontalPos, 0f);
                        rectTransform.offsetMin = new Vector2(horizontalPos, 0f);
                        rectTransform.localPosition = new Vector3(horizontalPos, 0f, 0f);
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdatePlayerModelPosition - clonedPlayerModelView is null.");
            }
        }

        private static void UpdatePlayerModelRotation()
        {
            if (ClonedPlayerModelView != null)
            {
                Transform playerModelTransform = ClonedPlayerModelView.transform.Find("PlayerMVObject/MenuPlayer");
                if (playerModelTransform != null)
                {
                    playerModelTransform.localRotation = Quaternion.Euler(0, Settings.RotationPlayerModelHorizontal.Value, 0);
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdatePlayerModelRotation - clonedPlayerModelView is null.");
            }
        }

        private static void UpdateCameraPosition()
        {
            if (ClonedPlayerModelView != null)
            {
                Transform cameraTransform = ClonedPlayerModelView.transform.Find("PlayerMVObject/Camera_inventory");
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = Settings.EnableLargerPlayerModel.Value ? new Vector3(0f, 0.2f, 1.5f) :
                        // Reset to default position when disabled
                        new Vector3(0f, 0f, 1f);
                }
                else
                {
                    Plugin.LogSource.LogWarning("UpdateCameraPosition - Camera_inventory not found.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateCameraPosition - clonedPlayerModelView is null.");
            }
        }

        private static void BottomFieldPositionChanged()
        {
            Transform bottomFieldTransform = GetBottomFieldTransform();
            if (bottomFieldTransform != null)
            {
                bottomFieldTransform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);
            }
            else
            {
                Plugin.LogSource.LogWarning("BottomFieldPositionChanged - BottomField transform not found.");
            }
        }

        private static void SubscribeToCharacterLevelUpEvent()
        {
            if (_experienceEventsSubscribed) return;
            if (PatchConstants.BackEndSession?.Profile?.Info != null)
            {
                PatchConstants.BackEndSession.Profile.Info.OnExperienceChanged += OnExperienceChanged;
                _experienceEventsSubscribed = true;
            }
            else
            {
                Plugin.LogSource.LogWarning("SubscribeToCharacterLevelUpEvent - BackEndSession.Profile.Info is null.");
            }
        }

        private static void UnsubscribeFromCharacterLevelUpEvent()
        {
            if (!_experienceEventsSubscribed) return;
            if (PatchConstants.BackEndSession?.Profile?.Info != null)
            {
                PatchConstants.BackEndSession.Profile.Info.OnExperienceChanged -= OnExperienceChanged;
                _experienceEventsSubscribed = false;
            }
        }

        private static void OnExperienceChanged(int oldExperience, int newExperience)
        {
            Transform bottomFieldTransform = GetBottomFieldTransform();
            if (bottomFieldTransform != null)
            {
                UpdatePlayerStats(bottomFieldTransform);
            }
            else
            {
                Plugin.LogSource.LogWarning("OnExperienceChanged - BottomField transform not found, cannot update player stats.");
            }
        }

        private static Transform GetBottomFieldTransform()
        {
            if (ClonedPlayerModelView != null)
            {
                return ClonedPlayerModelView.transform.Find("BottomField");
            }
            return null;
        }

        private static async Task AddPlayerModel()
        {
            if (menuPlayerCreated && ClonedPlayerModelView != null)
            {
                await RefreshPlayerModel().ConfigureAwait(false);
                return;
            }
            if (menuPlayerCreated)
            {
                Plugin.LogSource.LogWarning("AddPlayerModel - MenuPlayerCreated is true, but clonedPlayerModelView is null. Attempting to recreate.");
                menuPlayerCreated = false;
            }

            GameObject playerModelViewPrefab = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView");
            GameObject menuScreenParent = GameObject.Find("Common UI/Common UI/MenuScreen");

            if (playerModelViewPrefab == null || menuScreenParent == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - PlayerModelView prefab or MenuScreen parent not found. Cannot create player model.");
                return;
            }

            ClonedPlayerModelView = CreateClonedPlayerModelView(menuScreenParent, playerModelViewPrefab);
            if (ClonedPlayerModelView == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - Failed to create clonedPlayerModelView.");
                return;
            }

            menuPlayerCreated = true;

            PlayerModelView playerModelViewScript = ClonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
            if (playerModelViewScript == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - PlayerModelView script not found on the cloned PlayerModelViewObject.");
                Object.Destroy(ClonedPlayerModelView);
                menuPlayerCreated = false;
                ClonedPlayerModelView = null;
                return;
            }

            ConfigurePlayerModelVisuals(ClonedPlayerModelView);

            GameObject playerLevelViewPrefab = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level");
            GameObject playerLevelIconViewPrefab = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level Icon");
            if (playerLevelViewPrefab == null || playerLevelIconViewPrefab == null)
            {
                Plugin.LogSource.LogWarning("AddPlayerModel - PlayerLevelViewPrefab or PlayerLevelIconViewPrefab not found. BottomField setup might be incomplete.");
            }
            SetupBottomField(ClonedPlayerModelView, playerLevelViewPrefab, playerLevelIconViewPrefab);

            HidePlayerModelExtraElements(ClonedPlayerModelView);

            if (PatchConstants.BackEndSession?.Profile != null)
            {
                await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, false).ConfigureAwait(false);
                AdjustInnerPlayerModelPosition(ClonedPlayerModelView);
            }
            else
            {
                Plugin.LogSource.LogError("AddPlayerModel - BackEndSession.Profile is null. Cannot show player model view script.");
            }
        }

        private static GameObject CreateClonedPlayerModelView(GameObject parent, GameObject prefab)
        {
            if (parent == null || prefab == null)
            {
                Plugin.LogSource.LogError("CreateClonedPlayerModelView - Parent or Prefab is null.");
                return null;
            }
            GameObject instance = Object.Instantiate(prefab, parent.transform);
            instance.name = "MainMenuPlayerModelView";
            instance.SetActive(true);
            return instance;
        }

        private static void ConfigurePlayerModelVisuals(GameObject modelInstance)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - modelInstance is null."); return; }

            modelInstance.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -150f, 0f);
            modelInstance.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            Transform cameraTransform = modelInstance.transform.Find("PlayerMVObject/Camera_inventory");
            if (cameraTransform != null)
            {
                // Set camera position based on larger model setting
                cameraTransform.localPosition = Settings.EnableLargerPlayerModel.Value ? new Vector3(0f, 0.2f, 1.5f) :
                    // Set default position when disabled
                    new Vector3(0f, 0f, 1f);

                PrismEffects prismEffects = cameraTransform.GetComponent<PrismEffects>();
                if (prismEffects != null)
                {
                    prismEffects.toneValues = new Vector3(7f, 1.25f, 1.25f);
                    prismEffects.exposureUpperLimit = 0.55f;
                    prismEffects.useExposure = true;
                }
                else { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - PrismEffects component not found on Camera_inventory."); }
            }
            else { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - Camera_inventory not found in player model instance."); }

            LightHelpers.SetupLights(modelInstance);
            UpdatePlayerModelRotation();
        }

        private static void SetupBottomField(GameObject modelInstance, GameObject playerLevelViewPrefab, GameObject playerLevelIconViewPrefab)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("SetupBottomField - modelInstance is null."); return; }

            Transform bottomFieldTransform = modelInstance.transform.Find("BottomField");
            if (bottomFieldTransform == null)
            {
                Plugin.LogSource.LogError("SetupBottomField - BottomField transform not found in modelInstance.");
                return;
            }

            RectTransform bftRect = bottomFieldTransform.GetComponent<RectTransform>();
            if (bftRect == null)
            {
                Plugin.LogSource.LogError("SetupBottomField - RectTransform not found on BottomField. Cannot configure layout.");
                return;
            }
            bftRect.anchorMin = new Vector2(0, 1);
            bftRect.anchorMax = new Vector2(0, 1);
            bftRect.pivot = new Vector2(0, 1);
            bftRect.sizeDelta = new Vector2(0, 0);

            // Add ContentSizeFitter to BottomField itself
            ContentSizeFitter bftCsf = bottomFieldTransform.GetComponent<ContentSizeFitter>();
            if (bftCsf == null) bftCsf = bottomFieldTransform.gameObject.AddComponent<ContentSizeFitter>();
            bftCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bftCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup bftVlg = bottomFieldTransform.GetComponent<VerticalLayoutGroup>();
            if (bftVlg != null)
            {
                bftVlg.childAlignment = TextAnchor.UpperRight;
                bftVlg.spacing = 15f;
                bftVlg.padding = new RectOffset(0, 0, 0, 0);
                bftVlg.childForceExpandHeight = false;
                bftVlg.childControlHeight = true;
                bftVlg.childForceExpandWidth = false;
                bftVlg.childControlWidth = true;
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - VerticalLayoutGroup component not found on BottomField. Row layout might be incorrect."); }

            bottomFieldTransform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            bottomFieldTransform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);

            // Destroy existing dynamic elements to prevent duplication
            void DestroyChildIfExists(string name)
            {
                Transform child = bottomFieldTransform.Find(name);
                if (child != null)
                {
                    Object.Destroy(child.gameObject);
                }
            }

            DestroyChildIfExists("LevelGroup");
            DestroyChildIfExists("LevelInfoRow");
            DestroyChildIfExists("NicknameText");
            DestroyChildIfExists("ExperienceRow");
            DestroyChildIfExists("Spacer_LevelInfo_Nickname");
            DestroyChildIfExists("Spacer_Nickname_Experience");

            // Clean up the existing Experience panel if it exists
            Transform existingExperience = bottomFieldTransform.Find("Experience");
            if (existingExperience != null)
            {
                Object.Destroy(existingExperience.gameObject);
            }

            GameObject levelInfoRow = new GameObject("LevelInfoRow");
            levelInfoRow.transform.SetParent(bottomFieldTransform, false);
            RectTransform levelInfoRect = levelInfoRow.AddComponent<RectTransform>();
            levelInfoRect.anchorMin = new Vector2(1, 1);
            levelInfoRect.anchorMax = new Vector2(1, 1);
            levelInfoRect.pivot = new Vector2(1, 1);

            LayoutElement levelInfoRowLe = levelInfoRow.AddComponent<LayoutElement>();
            levelInfoRowLe.minHeight = 56f;
            levelInfoRowLe.preferredHeight = 56f;
            levelInfoRowLe.flexibleHeight = 0f;

            HorizontalLayoutGroup levelInfoHlg = levelInfoRow.AddComponent<HorizontalLayoutGroup>();
            levelInfoHlg.padding = new RectOffset(0, 0, 0, 0);
            levelInfoHlg.childAlignment = TextAnchor.MiddleRight;
            levelInfoHlg.spacing = 0f;
            levelInfoHlg.childForceExpandWidth = false;
            levelInfoHlg.childForceExpandHeight = false;
            levelInfoHlg.childControlWidth = true;
            levelInfoHlg.childControlHeight = true;
            levelInfoHlg.reverseArrangement = true;

            ContentSizeFitter levelInfoCsf = levelInfoRow.AddComponent<ContentSizeFitter>();
            levelInfoCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            levelInfoCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add level text first (will appear on the right due to reverseArrangement)
            if (playerLevelViewPrefab != null)
            {
                var clonedLevel = Object.Instantiate(playerLevelViewPrefab, levelInfoRow.transform);
                clonedLevel.name = "Level";
                clonedLevel.SetActive(true);

                RectTransform levelRect = clonedLevel.GetComponent<RectTransform>();
                if (levelRect != null)
                {
                    levelRect.anchorMin = new Vector2(1, 0.5f);
                    levelRect.anchorMax = new Vector2(1, 0.5f);
                    levelRect.pivot = new Vector2(1, 0.5f);
                }

                TextMeshProUGUI levelTMP = clonedLevel.GetComponent<TextMeshProUGUI>();
                if (levelTMP != null)
                {
                    levelTMP.alignment = TextAlignmentOptions.Right;
                    levelTMP.fontSize = 54f;
                    levelTMP.enableWordWrapping = false;
                    levelTMP.margin = Vector4.zero;
                }

                LayoutElement levelTextLe = clonedLevel.GetComponent<LayoutElement>();
                if (levelTextLe == null) levelTextLe = clonedLevel.AddComponent<LayoutElement>();
                levelTextLe.minHeight = 56f;
                levelTextLe.preferredHeight = 56f;
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - playerLevelViewPrefab is null. Level text will be missing."); }

            // Add level icon (will appear on the left of text due to reverseArrangement)
            if (playerLevelIconViewPrefab != null)
            {
                GameObject clonedIcon = Object.Instantiate(playerLevelIconViewPrefab, levelInfoRow.transform);
                clonedIcon.name = "Level Icon";
                clonedIcon.SetActive(true);

                RectTransform iconRect = clonedIcon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(1, 0.5f);
                    iconRect.anchorMax = new Vector2(1, 0.5f);
                    iconRect.pivot = new Vector2(1, 0.5f);
                    iconRect.sizeDelta = new Vector2(56f, 56f);
                    iconRect.offsetMax = new Vector2(-5f, iconRect.offsetMax.y);
                }

                LayoutElement iconLe = clonedIcon.GetComponent<LayoutElement>();
                if (iconLe == null) iconLe = clonedIcon.AddComponent<LayoutElement>();
                iconLe.minHeight = 56f;
                iconLe.minWidth = 56f;
                iconLe.preferredHeight = 56f;
                iconLe.preferredWidth = 56f;

                // Ensure image stretches to fill the space
                Image iconImage = clonedIcon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.preserveAspect = true;
                }
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - playerLevelIconViewPrefab is null. Level Icon will be missing."); }

            GameObject nicknameTextGo = new GameObject("NicknameText");
            nicknameTextGo.transform.SetParent(bottomFieldTransform, false);
            RectTransform nicknameRect = nicknameTextGo.AddComponent<RectTransform>();
            nicknameRect.anchorMin = new Vector2(1, 1);
            nicknameRect.anchorMax = new Vector2(1, 1);
            nicknameRect.pivot = new Vector2(1, 1);

            LayoutElement nicknameTextGoLe = nicknameTextGo.AddComponent<LayoutElement>();
            nicknameTextGoLe.minHeight = 40f;
            nicknameTextGoLe.preferredHeight = 40f;
            nicknameTextGoLe.flexibleHeight = 0f;

            TextMeshProUGUI nicknameTMP = nicknameTextGo.AddComponent<TextMeshProUGUI>();
            nicknameTMP.fontSize = 36f * 1.6f;
            nicknameTMP.color = Settings.AccentColor.Value;
            nicknameTMP.margin = Vector4.zero;
            nicknameTMP.lineSpacing = 0;
            nicknameTMP.alignment = TextAlignmentOptions.Right;
            nicknameTMP.enableWordWrapping = false;

            Transform nicknameAndKarmaSourceOriginal = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/NicknameAndKarma")?.transform;
            if (nicknameAndKarmaSourceOriginal != null)
            {
                TextMeshProUGUI originalNicknameTMP = nicknameAndKarmaSourceOriginal.GetComponentInChildren<TextMeshProUGUI>();
                if (originalNicknameTMP != null)
                {
                    if (originalNicknameTMP.font != null)
                    {
                        nicknameTMP.font = originalNicknameTMP.font;
                    }
                    nicknameTMP.fontSize = originalNicknameTMP.fontSize * 1.6f;
                }
            }

            ContentSizeFitter nicknameCsf = nicknameTextGo.AddComponent<ContentSizeFitter>();
            nicknameCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nicknameCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject experienceRow = new GameObject("ExperienceRow");
            experienceRow.transform.SetParent(bottomFieldTransform, false);
            RectTransform expRect = experienceRow.AddComponent<RectTransform>();
            expRect.anchorMin = new Vector2(1, 1);
            expRect.anchorMax = new Vector2(1, 1);
            expRect.pivot = new Vector2(1, 1);

            LayoutElement expRowLe = experienceRow.AddComponent<LayoutElement>();
            expRowLe.minHeight = 35f;
            expRowLe.preferredHeight = 35f;
            expRowLe.flexibleHeight = 0f;

            HorizontalLayoutGroup expHlg = experienceRow.AddComponent<HorizontalLayoutGroup>();
            expHlg.padding = new RectOffset(0, 0, 0, 0);
            expHlg.childAlignment = TextAnchor.MiddleRight;
            expHlg.spacing = 5f;
            expHlg.childForceExpandWidth = false;
            expHlg.childForceExpandHeight = false;
            expHlg.childControlWidth = true;
            expHlg.childControlHeight = true;

            ContentSizeFitter expCsf = experienceRow.AddComponent<ContentSizeFitter>();
            expCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            expCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject expLabelGo = new GameObject("ExpLabel");
            expLabelGo.transform.SetParent(experienceRow.transform, false);

            TextMeshProUGUI expLabelTMP = expLabelGo.AddComponent<TextMeshProUGUI>();
            expLabelTMP.text = "EXP:";
            expLabelTMP.fontSize = 24f;
            expLabelTMP.color = Color.white;
            expLabelTMP.alignment = TextAlignmentOptions.Right;
            expLabelTMP.margin = Vector4.zero;
            expLabelTMP.enableWordWrapping = false;

            GameObject expValueGo = new GameObject("ExpValue");
            expValueGo.transform.SetParent(experienceRow.transform, false);

            TextMeshProUGUI expValueTMP = expValueGo.AddComponent<TextMeshProUGUI>();
            expValueTMP.fontSize = 24f;
            expValueTMP.color = Settings.AccentColor.Value;
            expValueTMP.alignment = TextAlignmentOptions.Right;
            expValueTMP.margin = Vector4.zero;
            expValueTMP.enableWordWrapping = false;

            Transform experienceOriginal = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/Experience")?.transform;
            if (experienceOriginal != null)
            {
                TextMeshProUGUI originalExpTMP = experienceOriginal.GetComponentInChildren<TextMeshProUGUI>();
                if (originalExpTMP != null && originalExpTMP.font != null)
                {
                    expLabelTMP.font = originalExpTMP.font;
                    expValueTMP.font = originalExpTMP.font;
                }
            }

            Transform originalNicknameAndKarma = bottomFieldTransform.Find("NicknameAndKarma");
            if (originalNicknameAndKarma != null)
            {
                originalNicknameAndKarma.gameObject.SetActive(false);
            }

            levelInfoRow.transform.SetAsFirstSibling();
            nicknameTextGo.transform.SetSiblingIndex(1);
            experienceRow.transform.SetSiblingIndex(2);

            UpdatePlayerStats(bottomFieldTransform);
        }

        private static void HidePlayerModelExtraElements(GameObject modelInstance)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("HidePlayerModelExtraElements - modelInstance is null."); return; }
            LayoutHelpers.SetChildActive(modelInstance, "IconsContainer", false);
            LayoutHelpers.SetChildActive(modelInstance, "DragTrigger", false);
        }

        private static void AdjustInnerPlayerModelPosition(GameObject modelInstance)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("AdjustInnerPlayerModelPosition - modelInstance is null."); return; }
            Transform playerMvObject = modelInstance.transform.Find("PlayerMVObject");
            if (playerMvObject != null)
            {
                Transform innerModelTransform = playerMvObject.Find("MenuPlayer");
                if (innerModelTransform != null)
                {
                    innerModelTransform.localPosition = new Vector3(0f, -1.1f, 5f);
                }
                else { Plugin.LogSource.LogWarning("AdjustInnerPlayerModelPosition - MenuPlayer not found in PlayerMVObject."); }
            }
            else { Plugin.LogSource.LogWarning("AdjustInnerPlayerModelPosition - PlayerMVObject not found in modelInstance."); }
        }

        private static async Task RefreshPlayerModel()
        {
            if (ClonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("RefreshPlayerModel - clonedPlayerModelView is null. Cannot refresh.");
                return;
            }

            PlayerModelView playerModelViewScript = ClonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
            if (playerModelViewScript != null)
            {
                playerModelViewScript.Close();
                if (PatchConstants.BackEndSession?.Profile != null)
                {
                    await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, false).ConfigureAwait(false);
                    AdjustInnerPlayerModelPosition(ClonedPlayerModelView);
                    UpdateCameraPosition();
                }
                else { Plugin.LogSource.LogWarning("RefreshPlayerModel - BackEndSession.Profile is null. Cannot show player model."); }
            }
            else { Plugin.LogSource.LogWarning("RefreshPlayerModel - PlayerModelView script not found on clonedPlayerModelView."); }
        }

        private static void UpdatePlayerStats(Transform bottomFieldTransform)
        {
            if (bottomFieldTransform == null) { Plugin.LogSource.LogWarning("UpdatePlayerStats - bottomFieldTransform is null."); return; }

            Profile profile = PatchConstants.BackEndSession?.Profile;
            if (profile == null)
            {
                Plugin.LogSource.LogWarning("UpdatePlayerStats - BackEndSession.Profile is null. Cannot update stats.");
                return;
            }
            UpdateNicknameDisplay(bottomFieldTransform, profile);
            UpdateExperienceDisplay(bottomFieldTransform, profile);
            UpdateLevelDisplay(bottomFieldTransform, profile);
            UpdateTextColors();
        }

        private static void UpdateNicknameDisplay(Transform bottomField, Profile profile)
        {
            TextMeshProUGUI nicknameTMP = bottomField.Find("NicknameText")?.GetComponent<TextMeshProUGUI>();
            if (nicknameTMP != null)
            {
                nicknameTMP.text = profile.Nickname;
            }
            else { Plugin.LogSource.LogWarning("UpdateNicknameDisplay - NicknameText TMP component not found in BottomField."); }

            Transform originalNicknameAndKarma = bottomField.Find("NicknameAndKarma");
            if (originalNicknameAndKarma != null)
            {
                originalNicknameAndKarma.gameObject.SetActive(false);
            }
        }

        private static void UpdateExperienceDisplay(Transform bottomField, Profile profile)
        {
            Transform experienceRow = bottomField.Find("ExperienceRow");
            if (experienceRow != null)
            {
                TextMeshProUGUI experienceTMP = experienceRow.Find("ExpValue")?.GetComponent<TextMeshProUGUI>();
                if (experienceTMP != null)
                {
                    var numberFormat = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
                    experienceTMP.text = profile.Experience.ToString("N", numberFormat);
                }
                else { Plugin.LogSource.LogWarning("UpdateExperienceDisplay - ExpValue TMP component not found in ExperienceRow."); }
            }
            else
            {
                // Check for original Experience panel for backward compatibility
                Transform experienceTransform = bottomField.Find("Experience");
                if (experienceTransform != null)
                {
                    TextMeshProUGUI experienceTMP = experienceTransform.Find("ExpValue")?.GetComponent<TextMeshProUGUI>();
                    if (experienceTMP != null)
                    {
                        experienceTMP.alignment = TextAlignmentOptions.Right;
                        experienceTMP.margin = Vector4.zero;
                        experienceTMP.lineSpacing = 0;
                        var numberFormat = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
                        experienceTMP.text = profile.Experience.ToString("N", numberFormat);
                    }
                    else { Plugin.LogSource.LogWarning("UpdateExperienceDisplay - Experience TMP component (ExpValue) not found."); }
                }
                else { Plugin.LogSource.LogWarning("UpdateExperienceDisplay - Neither ExperienceRow nor Experience transform found in BottomField."); }
            }
        }

        private static void UpdateLevelDisplay(Transform bottomField, Profile profile)
        {
            Transform levelInfoRow = bottomField.Find("LevelInfoRow");
            if (levelInfoRow != null)
            {
                if (profile.Info == null)
                {
                    Plugin.LogSource.LogWarning("UpdateLevelDisplay - Profile.Info is null.");
                    return;
                }

                TextMeshProUGUI levelTMP = levelInfoRow.Find("Level")?.GetComponent<TextMeshProUGUI>();
                if (levelTMP != null)
                {
                    levelTMP.alignment = TextAlignmentOptions.Right;
                    levelTMP.margin = Vector4.zero;
                    levelTMP.lineSpacing = 0;
                    levelTMP.fontSize = 54f;
                    levelTMP.enableWordWrapping = false;
                    levelTMP.text = profile.Info.Level.ToString();
                }
                else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - Level TMP component not found in LevelInfoRow."); }

                Transform iconTransform = levelInfoRow.Find("Level Icon");
                if (iconTransform != null)
                {
                    Image iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        PlayerLevelPanel.SetLevelIcon(iconImage, profile.Info.Level);
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
                else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - Level Icon transform not found in LevelInfoRow."); }

                // Make sure the HorizontalLayoutGroup has reverseArrangement set and zero spacing
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
            else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - LevelInfoRow transform not found in BottomField."); }
        }

        public static void CleanupClonedPlayerModel()
        {
            if (ClonedPlayerModelView != null)
            {
                Object.Destroy(ClonedPlayerModelView);
                ClonedPlayerModelView = null;
                menuPlayerCreated = false;
            }
        }

        public void CleanupBeforeDisable()
        {
            UnsubscribeFromProfileSettingsChanges();
            UnsubscribeFromCharacterLevelUpEvent();
        }
    }
}