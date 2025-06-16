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

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class PlayerProfileFeaturesPatch : ModulePatch
    {
        public static bool MenuPlayerCreated = false;
        public static GameObject clonedPlayerModelView;

        private static bool _profileSettingsSubscribed = false;
        private static bool _experienceEventsSubscribed = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("Show", new[] { typeof(Profile), typeof(MatchmakerPlayerControllerClass), typeof(ESessionMode) });
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance)
        {
            if (__instance == null)
            {
                Plugin.LogSource.LogWarning("PlayerProfileFeaturesPatch.Postfix: MenuScreen instance is null.");
                return;
            }

            GameObject playButton = GameObject.Find("Common UI/Common UI/MenuScreen/PlayButton")?.gameObject;
            if (playButton == null || !playButton.activeSelf)
            {
                Plugin.LogSource.LogDebug("PlayerProfileFeaturesPatch.Postfix: PlayButton is null or inactive. Profile features might not apply if menu is not fully shown.");
            }

            await AddPlayerModel().ConfigureAwait(false);
            SubscribeToProfileSettingsChanges();
            SubscribeToCharacterLevelUpEvent();

            if (clonedPlayerModelView != null)
            {
                UpdatePlayerModelPosition();
                UpdatePlayerModelRotation();
                BottomFieldPositionChanged();
            }
        }

        private static void SubscribeToProfileSettingsChanges()
        {
            if (_profileSettingsSubscribed) return;

            Settings.PositionPlayerModelHorizontal.SettingChanged += OnPlayerModelPositionChanged;
            Settings.PositionBottomFieldHorizontal.SettingChanged += OnBottomFieldPositionChanged;
            Settings.PositionBottomFieldVertical.SettingChanged += OnBottomFieldPositionChanged;
            Settings.RotationPlayerModelHorizontal.SettingChanged += OnPlayerModelRotationChanged;

            _profileSettingsSubscribed = true;
            Plugin.LogSource.LogDebug("Profile-specific settings changes subscribed.");
        }

        private static void OnPlayerModelPositionChanged(object sender, EventArgs e) => UpdatePlayerModelPosition();
        private static void OnPlayerModelRotationChanged(object sender, EventArgs e) => UpdatePlayerModelRotation();
        private static void OnBottomFieldPositionChanged(object sender, EventArgs e) => BottomFieldPositionChanged();

        public static void UpdatePlayerModelPosition()
        {
            if (clonedPlayerModelView != null)
            {
                clonedPlayerModelView.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -250f, 0f);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdatePlayerModelPosition - clonedPlayerModelView is null.");
            }
        }

        public static void UpdatePlayerModelRotation()
        {
            if (clonedPlayerModelView != null)
            {
                Transform playerModelTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/MenuPlayer");
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

        public static void BottomFieldPositionChanged()
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
                Plugin.LogSource.LogDebug("Character experience events subscribed.");
            }
            else
            {
                Plugin.LogSource.LogWarning("SubscribeToCharacterLevelUpEvent - BackEndSession.Profile.Info is null.");
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
            if (clonedPlayerModelView != null)
            {
                return clonedPlayerModelView.transform.Find("BottomField");
            }
            Plugin.LogSource.LogDebug("GetBottomFieldTransform - clonedPlayerModelView is null.");
            return null;
        }

        private static async Task AddPlayerModel()
        {
            if (MenuPlayerCreated && clonedPlayerModelView != null)
            {
                await RefreshPlayerModel().ConfigureAwait(false);
                return;
            }
            if (MenuPlayerCreated)
            {
                Plugin.LogSource.LogWarning("AddPlayerModel - MenuPlayerCreated is true, but clonedPlayerModelView is null. Attempting to recreate.");
                MenuPlayerCreated = false;
            }

            GameObject playerModelViewPrefab = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView");
            GameObject menuScreenParent = GameObject.Find("Common UI/Common UI/MenuScreen");

            if (playerModelViewPrefab == null || menuScreenParent == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - PlayerModelView prefab or MenuScreen parent not found. Cannot create player model.");
                return;
            }

            clonedPlayerModelView = CreateClonedPlayerModelView(menuScreenParent, playerModelViewPrefab);
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - Failed to create clonedPlayerModelView.");
                return;
            }

            MenuPlayerCreated = true;

            PlayerModelView playerModelViewScript = clonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
            if (playerModelViewScript == null)
            {
                Plugin.LogSource.LogError("AddPlayerModel - PlayerModelView script not found on the cloned PlayerModelViewObject.");
                GameObject.Destroy(clonedPlayerModelView);
                MenuPlayerCreated = false;
                clonedPlayerModelView = null;
                return;
            }

            ConfigurePlayerModelVisuals(clonedPlayerModelView);

            GameObject playerLevelViewPrefab = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level");
            GameObject playerLevelIconViewPrefab = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level Icon");
            if (playerLevelViewPrefab == null || playerLevelIconViewPrefab == null)
            {
                Plugin.LogSource.LogWarning("AddPlayerModel - PlayerLevelViewPrefab or PlayerLevelIconViewPrefab not found. BottomField setup might be incomplete.");
            }
            SetupBottomField(clonedPlayerModelView, playerLevelViewPrefab, playerLevelIconViewPrefab);

            HidePlayerModelExtraElements(clonedPlayerModelView);

            if (PatchConstants.BackEndSession?.Profile != null)
            {
                await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, false).ConfigureAwait(false);
                AdjustInnerPlayerModelPosition(clonedPlayerModelView);
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
            GameObject instance = GameObject.Instantiate(prefab, parent.transform);
            instance.name = "MainMenuPlayerModelView";
            instance.SetActive(true);
            return instance;
        }

        private static void ConfigurePlayerModelVisuals(GameObject modelInstance)
        {
            if (modelInstance == null) { Plugin.LogSource.LogWarning("ConfigurePlayerModelVisuals - modelInstance is null."); return; }

            modelInstance.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -250f, 0f);
            modelInstance.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            Transform cameraTransform = modelInstance.transform.Find("PlayerMVObject/Camera_inventory");
            if (cameraTransform != null)
            {
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

            // Configure BottomField's RectTransform first
            RectTransform bftRect = bottomFieldTransform.GetComponent<RectTransform>();
            if (bftRect == null)
            {
                Plugin.LogSource.LogError("SetupBottomField - RectTransform not found on BottomField. Cannot configure layout.");
                return;
            }
            bftRect.anchorMin = new Vector2(0, 1); // Top
            bftRect.anchorMax = new Vector2(0, 1); // Top
            bftRect.pivot = new Vector2(0, 1);     // Top-Left pivot
            bftRect.sizeDelta = new Vector2(0, 0); // Reset sizeDelta, ContentSizeFitter will control it

            // Add ContentSizeFitter to BottomField itself
            ContentSizeFitter bftCSF = bottomFieldTransform.GetComponent<ContentSizeFitter>();
            if (bftCSF == null) bftCSF = bottomFieldTransform.gameObject.AddComponent<ContentSizeFitter>();
            bftCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bftCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup bftVLG = bottomFieldTransform.GetComponent<VerticalLayoutGroup>();
            if (bftVLG != null)
            {
                bftVLG.childAlignment = TextAnchor.UpperRight;
                bftVLG.spacing = 15f; // Increase spacing between rows for better readability
                bftVLG.padding = new RectOffset(0, 0, 0, 0);
                bftVLG.childForceExpandHeight = false;
                bftVLG.childControlHeight = true; // Let children control their height
                bftVLG.childForceExpandWidth = false;
                bftVLG.childControlWidth = true; // Let children control their width
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - VerticalLayoutGroup component not found on BottomField. Row layout might be incorrect."); }

            bottomFieldTransform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            bottomFieldTransform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);

            // Destroy existing dynamic elements to prevent duplication
            Action<string> DestroyChildIfExists = (name) => {
                Transform child = bottomFieldTransform.Find(name);
                if (child != null) {
                    Plugin.LogSource.LogDebug($"SetupBottomField - Destroying existing {name}.");
                    GameObject.Destroy(child.gameObject);
                }
            };

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
                GameObject.Destroy(existingExperience.gameObject);
                Plugin.LogSource.LogDebug("SetupBottomField - Destroying existing Experience panel to recreate it.");
            }

            // ====== ROW 1: Level Icon + Level Text ======
            GameObject levelInfoRow = new GameObject("LevelInfoRow");
            levelInfoRow.transform.SetParent(bottomFieldTransform, false);
            RectTransform levelInfoRect = levelInfoRow.AddComponent<RectTransform>();
            levelInfoRect.anchorMin = new Vector2(1, 1);
            levelInfoRect.anchorMax = new Vector2(1, 1);
            levelInfoRect.pivot = new Vector2(1, 1);

            LayoutElement levelInfoRowLE = levelInfoRow.AddComponent<LayoutElement>();
            levelInfoRowLE.minHeight = 56f; // Make it taller to match new icon size
            levelInfoRowLE.preferredHeight = 56f;
            levelInfoRowLE.flexibleHeight = 0f;

            HorizontalLayoutGroup levelInfoHLG = levelInfoRow.AddComponent<HorizontalLayoutGroup>();
            levelInfoHLG.padding = new RectOffset(0, 0, 0, 0);
            levelInfoHLG.childAlignment = TextAnchor.MiddleRight;
            levelInfoHLG.spacing = 0f; // Eliminate horizontal spacing between icon and text
            levelInfoHLG.childForceExpandWidth = false;
            levelInfoHLG.childForceExpandHeight = false;
            levelInfoHLG.childControlWidth = true;
            levelInfoHLG.childControlHeight = true;
            levelInfoHLG.reverseArrangement = true; // This ensures right alignment while maintaining order

            ContentSizeFitter levelInfoCSF = levelInfoRow.AddComponent<ContentSizeFitter>();
            levelInfoCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            levelInfoCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add level text first (will appear on the right due to reverseArrangement)
            GameObject clonedLevel = null;
            if (playerLevelViewPrefab != null)
            {
                clonedLevel = GameObject.Instantiate(playerLevelViewPrefab, levelInfoRow.transform);
                clonedLevel.name = "Level";
                clonedLevel.SetActive(true);

                RectTransform levelRect = clonedLevel.GetComponent<RectTransform>();
                if (levelRect != null)
                {
                    levelRect.anchorMin = new Vector2(1, 0.5f);
                    levelRect.anchorMax = new Vector2(1, 0.5f);
                    levelRect.pivot = new Vector2(1, 0.5f);
                }

                // Check if it has TMP component and set alignment
                TextMeshProUGUI levelTMP = clonedLevel.GetComponent<TextMeshProUGUI>();
                if (levelTMP != null)
                {
                    levelTMP.alignment = TextAlignmentOptions.Right; // Right alignment works better with reverseArrangement
                    levelTMP.fontSize = 54f; // Increased font size for level
                    levelTMP.enableWordWrapping = false;
                    levelTMP.margin = Vector4.zero;
                }
                
                LayoutElement levelTextLE = clonedLevel.GetComponent<LayoutElement>();
                if (levelTextLE == null) levelTextLE = clonedLevel.AddComponent<LayoutElement>();
                levelTextLE.minHeight = 56f;
                levelTextLE.preferredHeight = 56f;
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - playerLevelViewPrefab is null. Level text will be missing."); }

            // Add level icon (will appear on the left of text due to reverseArrangement)
            if (playerLevelIconViewPrefab != null)
            {
                GameObject clonedIcon = GameObject.Instantiate(playerLevelIconViewPrefab, levelInfoRow.transform);
                clonedIcon.name = "Level Icon";
                clonedIcon.SetActive(true);

                RectTransform iconRect = clonedIcon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(1, 0.5f);
                    iconRect.anchorMax = new Vector2(1, 0.5f);
                    iconRect.pivot = new Vector2(1, 0.5f);
                    iconRect.sizeDelta = new Vector2(56f, 56f); // Increased icon size
                    // Use a negative right margin to move the icon closer to the text
                    iconRect.offsetMax = new Vector2(-5f, iconRect.offsetMax.y);
                }

                LayoutElement iconLE = clonedIcon.GetComponent<LayoutElement>();
                if (iconLE == null) iconLE = clonedIcon.AddComponent<LayoutElement>();
                iconLE.minHeight = 56f; // Increased size
                iconLE.minWidth = 56f; // Increased size
                iconLE.preferredHeight = 56f;
                iconLE.preferredWidth = 56f;

                // Ensure image stretches to fill the space
                Image iconImage = clonedIcon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.preserveAspect = true;
                }
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - playerLevelIconViewPrefab is null. Level Icon will be missing."); }

            // ====== ROW 2: Nickname ======
            GameObject nicknameTextGO = new GameObject("NicknameText");
            nicknameTextGO.transform.SetParent(bottomFieldTransform, false);
            RectTransform nicknameRect = nicknameTextGO.AddComponent<RectTransform>();
            nicknameRect.anchorMin = new Vector2(1, 1);
            nicknameRect.anchorMax = new Vector2(1, 1);
            nicknameRect.pivot = new Vector2(1, 1);
            
            LayoutElement nicknameTextGOLE = nicknameTextGO.AddComponent<LayoutElement>();
            nicknameTextGOLE.minHeight = 40f; // Slightly taller for better appearance
            nicknameTextGOLE.preferredHeight = 40f;
            nicknameTextGOLE.flexibleHeight = 0f;

            TextMeshProUGUI nicknameTMP = nicknameTextGO.AddComponent<TextMeshProUGUI>();
            nicknameTMP.fontSize = 36f * 1.6f;
            nicknameTMP.color = new Color(1f, 0.75f, 0.3f, 1f);
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
                    nicknameTMP.color = originalNicknameTMP.color;
                    Plugin.LogSource.LogDebug("SetupBottomField - Applied original NicknameAndKarma style to NicknameText.");
                }
            }

            ContentSizeFitter nicknameCSF = nicknameTextGO.AddComponent<ContentSizeFitter>();
            nicknameCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nicknameCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ====== ROW 3: Experience ======
            GameObject experienceRow = new GameObject("ExperienceRow");
            experienceRow.transform.SetParent(bottomFieldTransform, false);
            RectTransform expRect = experienceRow.AddComponent<RectTransform>();
            expRect.anchorMin = new Vector2(1, 1);
            expRect.anchorMax = new Vector2(1, 1);
            expRect.pivot = new Vector2(1, 1);
            
            LayoutElement expRowLE = experienceRow.AddComponent<LayoutElement>();
            expRowLE.minHeight = 35f; // Slightly taller
            expRowLE.preferredHeight = 35f;
            expRowLE.flexibleHeight = 0f;
            
            HorizontalLayoutGroup expHLG = experienceRow.AddComponent<HorizontalLayoutGroup>();
            expHLG.padding = new RectOffset(0, 0, 0, 0);
            expHLG.childAlignment = TextAnchor.MiddleRight;
            expHLG.spacing = 5f;
            expHLG.childForceExpandWidth = false;
            expHLG.childForceExpandHeight = false;
            expHLG.childControlWidth = true;
            expHLG.childControlHeight = true;
            
            ContentSizeFitter expCSF = experienceRow.AddComponent<ContentSizeFitter>();
            expCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            expCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Add "EXP:" label
            GameObject expLabelGO = new GameObject("ExpLabel");
            expLabelGO.transform.SetParent(experienceRow.transform, false);
            
            TextMeshProUGUI expLabelTMP = expLabelGO.AddComponent<TextMeshProUGUI>();
            expLabelTMP.text = "EXP:";
            expLabelTMP.fontSize = 24f;
            expLabelTMP.color = Color.white;
            expLabelTMP.alignment = TextAlignmentOptions.Right;
            expLabelTMP.margin = Vector4.zero;
            expLabelTMP.enableWordWrapping = false;
            
            // Add experience value
            GameObject expValueGO = new GameObject("ExpValue");
            expValueGO.transform.SetParent(experienceRow.transform, false);
            
            TextMeshProUGUI expValueTMP = expValueGO.AddComponent<TextMeshProUGUI>();
            expValueTMP.fontSize = 24f;
            expValueTMP.color = new Color(1f, 0.75f, 0.3f, 1f);
            expValueTMP.alignment = TextAlignmentOptions.Right;
            expValueTMP.margin = Vector4.zero;
            expValueTMP.enableWordWrapping = false;
            
            // Look for existing font references to keep consistent styling
            Transform experienceOriginal = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/Experience")?.transform;
            if (experienceOriginal != null)
            {
                TextMeshProUGUI originalExpTMP = experienceOriginal.GetComponentInChildren<TextMeshProUGUI>();
                if (originalExpTMP != null && originalExpTMP.font != null)
                {
                    expLabelTMP.font = originalExpTMP.font;
                    expValueTMP.font = originalExpTMP.font;
                    Plugin.LogSource.LogDebug("SetupBottomField - Applied original Experience font to new experience elements.");
                }
            }
            
            // Hide original experience and nickname panels if they exist
            Transform originalNicknameAndKarma = bottomFieldTransform.Find("NicknameAndKarma");
            if (originalNicknameAndKarma != null)
            {
                originalNicknameAndKarma.gameObject.SetActive(false);
            }
            
            // Set sibling order (top to bottom)
            levelInfoRow.transform.SetAsFirstSibling();
            nicknameTextGO.transform.SetSiblingIndex(1);
            experienceRow.transform.SetSiblingIndex(2);
            
            // Update the stats immediately
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
            Transform playerMVObject = modelInstance.transform.Find("PlayerMVObject");
            if (playerMVObject != null)
            {
                Transform innerModelTransform = playerMVObject.Find("MenuPlayer");
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
            if (clonedPlayerModelView == null)
            {
                Plugin.LogSource.LogWarning("RefreshPlayerModel - clonedPlayerModelView is null. Cannot refresh.");
                return;
            }

            PlayerModelView playerModelViewScript = clonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
            if (playerModelViewScript != null)
            {
                playerModelViewScript.Close();
                if (PatchConstants.BackEndSession?.Profile != null)
                {
                    await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, false).ConfigureAwait(false);
                    AdjustInnerPlayerModelPosition(clonedPlayerModelView);
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
                Plugin.LogSource.LogDebug("UpdateNicknameDisplay - Deactivated original NicknameAndKarma in BottomField.");
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
                    levelTMP.alignment = TextAlignmentOptions.Right; // Right alignment with reverseArrangement
                    levelTMP.margin = Vector4.zero; // Ensure no internal margins
                    levelTMP.lineSpacing = 0; // Ensure minimal line spacing
                    levelTMP.fontSize = 54f; // Maintain the larger font size during updates
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
                        // Apply appropriate level icon
                        PlayerLevelPanel.SetLevelIcon(iconImage, profile.Info.Level);
                        // Make sure image fills its container while preserving aspect ratio
                        iconImage.preserveAspect = true;
                    }
                    
                    // Ensure icon stays at the proper size and position
                    RectTransform iconRect = iconTransform.GetComponent<RectTransform>();
                    if (iconRect != null)
                    {
                        iconRect.sizeDelta = new Vector2(56f, 56f); // Keep larger icon size
                        // Maintain the negative right margin to keep icon close to text
                        iconRect.offsetMax = new Vector2(-5f, iconRect.offsetMax.y);
                    }

                    LayoutElement iconLE = iconTransform.GetComponent<LayoutElement>();
                    if (iconLE != null)
                    {
                        iconLE.minWidth = 56f;
                        iconLE.minHeight = 56f;
                        iconLE.preferredWidth = 56f;
                        iconLE.preferredHeight = 56f;
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
                    hlg.spacing = 0f; // Make sure spacing stays at zero
                }
            }
            else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - LevelInfoRow transform not found in BottomField."); }
        }
    }
}