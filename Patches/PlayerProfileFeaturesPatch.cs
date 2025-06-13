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
using SPT.Reflection.Utils; // Added this using directive

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
            // Ensure MenuOverhaulPatch's core setup (like ButtonHelpers) has a chance to run if there are dependencies.
            // For now, assuming this patch can run its logic based on the state of MenuScreen.Show
            // and doesn't have hard dependencies on MenuOverhaulPatch.Postfix completing first for its own elements.

            if (__instance == null)
            {
                Plugin.LogSource.LogWarning("PlayerProfileFeaturesPatch.Postfix: MenuScreen instance is null.");
                return;
            }
            
            // It's possible MenuOverhaulPatch's Postfix check for playButton is also relevant here
            // if AddPlayerModel or other logic depends on the menu being fully ready.
            GameObject playButton = GameObject.Find("Common UI/Common UI/MenuScreen/PlayButton")?.gameObject;
            if (playButton == null || !playButton.activeSelf)
            {
                Plugin.LogSource.LogDebug("PlayerProfileFeaturesPatch.Postfix: PlayButton is null or inactive. Profile features might not apply if menu is not fully shown.");
                // Depending on requirements, might return here or proceed cautiously.
            }

            await AddPlayerModel().ConfigureAwait(false); // Handles creation and refresh
            SubscribeToProfileSettingsChanges();
            SubscribeToCharacterLevelUpEvent();

            // Initial updates after model creation/potential refresh
            if (clonedPlayerModelView != null)
            {
                UpdatePlayerModelPosition();
                UpdatePlayerModelRotation(); // AddPlayerModel calls this, but if settings changed before it ran, this ensures latest.
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
            // Note: EnableExtraShadows is handled by MenuOverhaulPatch as it affects LightHelpers.UpdateLights which is general.

            _profileSettingsSubscribed = true;
            Plugin.LogSource.LogDebug("Profile-specific settings changes subscribed.");
        }

        private static void OnPlayerModelPositionChanged(object sender, EventArgs e) => UpdatePlayerModelPosition();
        private static void OnPlayerModelRotationChanged(object sender, EventArgs e) => UpdatePlayerModelRotation();
        private static void OnBottomFieldPositionChanged(object sender, EventArgs e) => BottomFieldPositionChanged();

        public static void UpdatePlayerModelPosition() // Made public static for potential external calls if needed by other patches
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

        public static void UpdatePlayerModelRotation() // Made public static
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

        public static void BottomFieldPositionChanged() // Made public static
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
            Plugin.LogSource.LogDebug("GetBottomFieldTransform - clonedPlayerModelView is null."); // Changed to Debug as it might be called before creation
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
                MenuPlayerCreated = false; // Reset to allow recreation
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
                GameObject.Destroy(clonedPlayerModelView); // Clean up
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

            LightHelpers.SetupLights(modelInstance); // Setup lights for this specific player model
            UpdatePlayerModelRotation(); // Apply initial rotation
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

            VerticalLayoutGroup layoutGroup = bottomFieldTransform.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                layoutGroup.spacing = 15f;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childControlHeight = false;
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - VerticalLayoutGroup component not found on BottomField."); }

            bottomFieldTransform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            bottomFieldTransform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);

            // Destroy existing "LevelGroup" if it was somehow duplicated or from a previous incorrect setup
            Transform existingLevelGroup = bottomFieldTransform.Find("LevelGroup");
            if (existingLevelGroup != null)
            {
                Plugin.LogSource.LogDebug("SetupBottomField - Destroying existing LevelGroup before creating a new one.");
                GameObject.Destroy(existingLevelGroup.gameObject);
            }
            
            GameObject levelGroup = new GameObject("LevelGroup");
            levelGroup.transform.SetParent(bottomFieldTransform, false);
            RectTransform rectTransformLG = levelGroup.AddComponent<RectTransform>(); // Add RectTransform if not present
            rectTransformLG.anchorMin = Vector2.zero;
            rectTransformLG.anchorMax = Vector2.zero;
            rectTransformLG.pivot = Vector2.zero;
            rectTransformLG.anchoredPosition = Vector2.zero;


            // Attempt to find original NicknameAndKarma to copy components from.
            // This path is fragile if the original UI structure changes.
            Transform nicknameAndKarmaSourceOriginal = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView/BottomField/NicknameAndKarma")?.transform;
            if (nicknameAndKarmaSourceOriginal != null)
            {
                 // Copying components by reflection is complex and error-prone.
                 // It's better to recreate the structure or instantiate a prefab if possible.
                 // For now, let's assume it's primarily for layout and TextMeshPro.
                 // A simple TextMeshProUGUI for nickname might be more robust if copying is problematic.
                 // This simplified version just adds a new TextMeshPro for nickname.
                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(levelGroup.transform, false);
                var nicknameTMP = labelObj.AddComponent<TextMeshProUGUI>();
                // Configure nicknameTMP (font, size, etc.) as needed, or copy from a source if available and safe.
                // For now, it will use default TMP settings.
                // Example: nicknameTMP.font = Resources.Load<TMP_FontAsset>("PathToSomeFontAsset");
                nicknameTMP.fontSize = 36; // From original UpdateNicknameDisplay
                nicknameTMP.color = new Color(1f, 0.75f, 0.3f, 1f); // From original

                // If other components from NicknameAndKarma are essential, they need specific handling.
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - Original NicknameAndKarma source for component copy not found. Nickname display might be basic."); }


            if (playerLevelViewPrefab != null && playerLevelIconViewPrefab != null)
            {
                GameObject clonedIcon = GameObject.Instantiate(playerLevelIconViewPrefab, levelGroup.transform);
                clonedIcon.name = "Level Icon";
                clonedIcon.SetActive(true);

                GameObject clonedLevel = GameObject.Instantiate(playerLevelViewPrefab, levelGroup.transform);
                clonedLevel.name = "Level";
                clonedLevel.SetActive(true);
                clonedLevel.transform.SetSiblingIndex(clonedIcon.transform.GetSiblingIndex() + 1);
            }
            else { Plugin.LogSource.LogWarning("SetupBottomField - PlayerLevelViewPrefab or PlayerLevelIconViewPrefab is null. Level display will be missing."); }

            levelGroup.transform.SetAsFirstSibling(); // Make it the first child

            // Adjust existing children if necessary (e.g. the original NicknameAndKarma, Experience if they are still direct children)
            foreach (Transform child in bottomFieldTransform)
            {
                if(child == levelGroup.transform) continue; // Skip the one we just added

                HorizontalLayoutGroup hlg = child.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) { hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false; }

                ContentSizeFitter csf = child.GetComponent<ContentSizeFitter>();
                if (csf != null) { csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
            }
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
                    AdjustInnerPlayerModelPosition(clonedPlayerModelView); // Re-adjust position
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
            // Assumes LevelGroup/Label structure from the modified SetupBottomField
            Transform levelGroup = bottomField.Find("LevelGroup");
            if (levelGroup != null)
            {
                TextMeshProUGUI nicknameTMP = levelGroup.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (nicknameTMP != null)
                {
                    nicknameTMP.text = profile.Nickname;
                    // Font size and color already set during creation in SetupBottomField or needs to be reapplied if dynamic
                }
                else { Plugin.LogSource.LogWarning("UpdateNicknameDisplay - Nickname TMP (Label) component not found in LevelGroup."); }
            }
            else { Plugin.LogSource.LogWarning("UpdateNicknameDisplay - LevelGroup transform not found for Nickname."); }
            
            // Hide original NicknameAndKarma if it still exists as a direct child of bottomField
            Transform originalNicknameAndKarma = bottomField.Find("NicknameAndKarma");
            if(originalNicknameAndKarma != null)
            {
                originalNicknameAndKarma.gameObject.SetActive(false);
                 Plugin.LogSource.LogDebug("UpdateNicknameDisplay - Deactivated original NicknameAndKarma in BottomField.");
            }
        }

        private static void UpdateExperienceDisplay(Transform bottomField, Profile profile)
        {
            // This assumes "Experience" is still a direct child of bottomField and not part of the new "LevelGroup"
            Transform experienceTransform = bottomField.Find("Experience");
            if (experienceTransform != null)
            {
                TextMeshProUGUI experienceTMP = experienceTransform.Find("ExpValue")?.GetComponent<TextMeshProUGUI>();
                if (experienceTMP != null)
                {
                    var numberFormat = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
                    experienceTMP.text = profile.Experience.ToString("N", numberFormat);
                }
                else { Plugin.LogSource.LogWarning("UpdateExperienceDisplay - Experience TMP component (ExpValue) not found."); }
            }
            else { Plugin.LogSource.LogWarning("UpdateExperienceDisplay - Experience transform not found in BottomField."); }
        }

        private static void UpdateLevelDisplay(Transform bottomField, Profile profile)
        {
            Transform levelGroup = bottomField.Find("LevelGroup"); // Level and Icon are now children of LevelGroup
            if (levelGroup != null)
            {
                if (profile.Info == null)
                {
                    Plugin.LogSource.LogWarning("UpdateLevelDisplay - Profile.Info is null.");
                    return;
                }

                TextMeshProUGUI levelTMP = levelGroup.Find("Level")?.GetComponent<TextMeshProUGUI>();
                if (levelTMP != null)
                {
                    levelTMP.text = profile.Info.Level.ToString();
                }
                else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - Level TMP component not found in LevelGroup."); }

                Image iconImage = levelGroup.Find("Level Icon")?.GetComponent<Image>();
                if (iconImage != null)
                {
                    PlayerLevelPanel.SetLevelIcon(iconImage, profile.Info.Level);
                }
                else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - Level Icon Image component not found in LevelGroup."); }
            }
            else { Plugin.LogSource.LogWarning("UpdateLevelDisplay - LevelGroup transform not found for Level/Icon."); }
        }
    }
}
