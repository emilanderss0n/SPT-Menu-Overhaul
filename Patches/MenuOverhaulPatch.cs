using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SPT.Reflection.Utils;
using TMPro;
using System.Globalization;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class MenuOverhaulPatch : ModulePatch
    {
        public static bool MenuPlayerCreated = false;
        public static GameObject clonedPlayerModelView;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance)
        {
            // Check if PlayButton is active within the specified path
            GameObject playButton = GameObject.Find("Common UI/Common UI/MenuScreen/PlayButton")?.gameObject;
            if (__instance == null || playButton == null || !playButton.activeSelf)
            {
                Plugin.LogSource.LogWarning("MenuScreen or PlayButton is null or inactive.");
                return;
            }

            ButtonHelpers.SetupButtonIcons(__instance);
            await LoadPatchContent(__instance).ConfigureAwait(false);
            InitializeSceneEvents();
            HandleScene(SceneManager.GetActiveScene());
            ButtonHelpers.ProcessButtons(__instance);
            await AddPlayerModel().ConfigureAwait(false);
            SubscribeToSettingsChanges();
            UpdateSetElements();
            SubscribeToCharacterLevelUpEvent();
        }

        private static void SubscribeToSettingsChanges()
        {
            Settings.EnableTopGlow.SettingChanged += OnSettingsChanged;
            Settings.EnableBackground.SettingChanged += OnSettingsChanged;
            Settings.EnableExtraShadows.SettingChanged += OnSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged += OnSettingsChanged;
            Settings.PositionPlayerModelHorizontal.SettingChanged += OnPlayerModelPositionChanged;
            Settings.PositionBottomFieldHorizontal.SettingChanged += OnBottomFieldPositionChanged;
            Settings.PositionBottomFieldVertical.SettingChanged += OnBottomFieldPositionChanged;
            Settings.scaleBackgroundX.SettingChanged += OnScaleBackgroundChanged;
            Settings.scaleBackgroundY.SettingChanged += OnScaleBackgroundChanged;
        }

        private static Task LoadPatchContent(MenuScreen __instance)
        {
            LayoutHelpers.HideGameObject(__instance, "_alphaWarningGameObject");
            LayoutHelpers.HideGameObject(__instance, "_warningGameObject");

            return Task.CompletedTask;
        }

        private static void OnSettingsChanged(object sender, EventArgs e)
        {
            UpdateSetElements();
        }

        private static void UpdateSetElements()
        {
            var environmentObjects = LayoutHelpers.FindEnvironmentObjects();

            if (environmentObjects.CommonObj != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.CommonObj, "Glow Canvas", Settings.EnableTopGlow.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateSetElements - CommonObj not found.");
            }

            if (environmentObjects.FactoryLayout != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "CustomPlane", Settings.EnableBackground.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateSetElements - FactoryLayout not found.");
            }

            // Position the main parent decal_plane using settings
            Transform decalPlaneTransform = environmentObjects.FactoryLayout.transform.Find("decal_plane");
            if (decalPlaneTransform != null)
            {
                decalPlaneTransform.position = new Vector3(
                    Settings.PositionLogotypeHorizontal.Value,
                    -999.4f,
                    0f);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateSetElements - decal_plane GameObject not found.");
            }

            LightHelpers.UpdateLights();
        }

        private static void OnPlayerModelPositionChanged(object sender, EventArgs e)
        {
            UpdatePlayerModelPosition();
        }
        private static void OnBottomFieldPositionChanged(object sender, EventArgs e)
        {
            BottomFieldPositionChanged();
        }

        private static void OnScaleBackgroundChanged(object sender, EventArgs e)
        {
            UpdateCustomPlaneScale();
        }

        private static void UpdatePlayerModelPosition()
        {
            GameObject mainMenuPlayerModelView = GameObject.Find("Common UI/Common UI/MenuScreen/MainMenuPlayerModelView");
            if (mainMenuPlayerModelView != null)
            {
                mainMenuPlayerModelView.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -250f, 0f);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdatePlayerModelPosition - MainMenuPlayerModelView not found.");
            }
        }

        private static void BottomFieldPositionChanged()
        {
            Transform bottomFieldTransform = GetBottomFieldTransform();
            if (bottomFieldTransform != null)
            {
                bottomFieldTransform.transform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);
            }
            else
            {
                Plugin.LogSource.LogWarning("BottomFieldPositionChanged - BottomField GameObject not found.");
            }
        }

        private static void UpdateCustomPlaneScale()
        {
            var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
            GameObject customPlane = environmentObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (customPlane != null)
            {
                Vector3 localScale = customPlane.transform.localScale;
                localScale.x = Settings.scaleBackgroundX.Value;
                localScale.z = Settings.scaleBackgroundY.Value;
                customPlane.transform.localScale = localScale;
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateCustomPlaneScale - CustomPlane not found.");
            }
        }

        private static void SubscribeToCharacterLevelUpEvent()
        {
            PatchConstants.BackEndSession.Profile.Info.OnExperienceChanged += OnExperienceChanged;
        }

        private static void OnExperienceChanged(int oldExperience, int newExperience)
        {
            // Assuming you have a reference to the bottomFieldTransform
            Transform bottomFieldTransform = GetBottomFieldTransform();
            if (bottomFieldTransform != null)
            {
                UpdatePlayerStats(bottomFieldTransform);
            }
        }

        private static Transform GetBottomFieldTransform()
        {
            // Logic to get the bottomFieldTransform
            GameObject mainMenuPlayerModelView = GameObject.Find("Common UI/Common UI/MenuScreen/MainMenuPlayerModelView");
            if (mainMenuPlayerModelView != null)
            {
                return mainMenuPlayerModelView.transform.Find("BottomField");
            }
            return null;
        }

        private static async Task AddPlayerModel()
        {    
            if (!MenuPlayerCreated)
            {
                GameObject playerModelView = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView");
                GameObject playerLevelView = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level");
                GameObject playerLevelIconView = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level Icon");
                GameObject menuScreenParent = GameObject.Find("Common UI/Common UI/MenuScreen");

                if (playerModelView != null && menuScreenParent != null)
                {
                    clonedPlayerModelView = GameObject.Instantiate(playerModelView, menuScreenParent.transform);
                    clonedPlayerModelView.name = "MainMenuPlayerModelView";
                    clonedPlayerModelView.SetActive(true);

                    MenuPlayerCreated = true;

                    PlayerModelView playerModelViewScript = clonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
                    if (playerModelViewScript != null)
                    {
                        clonedPlayerModelView.transform.localPosition = new Vector3(Settings.PositionPlayerModelHorizontal.Value, -250f, 0f);
                        clonedPlayerModelView.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

                        Transform cameraTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/Camera_inventory");
                        if (cameraTransform != null)
                        {
                            var prismEffects = cameraTransform.GetComponent<PrismEffects>();
                            if (prismEffects != null)
                            {
                                // Set the specified properties
                                prismEffects.useExposure = true;
                                prismEffects.useAmbientObscurance = false;
                                prismEffects.useRays = true;
                                prismEffects.tonemapType = Prism.Utils.TonemapType.ACES;
                                prismEffects.toneValues = new Vector3(9f, 0.28f, 0.5f);
                                prismEffects.exposureUpperLimit = 0.55f;
                                prismEffects.aoBias = 0.1f;
                                prismEffects.aoIntensity = 4f;
                                prismEffects.aoRadius = 0.2f;
                                prismEffects.aoBlurFilterDistance = 1.25f;
                                prismEffects.aoMinIntensity = 1f;
                                prismEffects.aoLightingContribution = 5f;
                                prismEffects.useLensDirt = false;
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("PrismEffects component not found on Camera_inventory.");
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("Camera_inventory not found.");
                        }

                        LightHelpers.SetupLights(clonedPlayerModelView);

                        Transform bottomFieldTransform = clonedPlayerModelView.transform.Find("BottomField");
                        if (bottomFieldTransform != null)
                        {
                            // Set the VerticalLayoutGroup childAlignment to UpperLeft
                            VerticalLayoutGroup layoutGroup = bottomFieldTransform.GetComponent<VerticalLayoutGroup>();
                            if (layoutGroup != null)
                            {
                                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                                layoutGroup.spacing = 15f; // Add vertical spacing between children
                                layoutGroup.childForceExpandHeight = false; // Ensure children do not expand vertically
                                layoutGroup.childControlHeight = false;
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("VerticalLayoutGroup component not found on BottomField.");
                            }

                            bottomFieldTransform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                            bottomFieldTransform.localPosition = new Vector3(Settings.PositionBottomFieldHorizontal.Value, Settings.PositionBottomFieldVertical.Value, 0f);

                            // Create a new GameObject to group Level and Level Icon
                            GameObject levelGroup = new GameObject("LevelGroup");
                            levelGroup.transform.SetParent(bottomFieldTransform);

                            // Clone components and values from nicknameAndKarmaTransform
                            Transform nicknameAndKarmaTransform = GameObject.Find("Common UI/Common UI/MenuScreen/MainMenuPlayerModelView/BottomField/NicknameAndKarma")?.transform;
                            if (nicknameAndKarmaTransform != null)
                            {
                                foreach (var component in nicknameAndKarmaTransform.GetComponents<Component>())
                                {
                                    Type componentType = component.GetType();
                                    Component newComponent = levelGroup.AddComponent(componentType);
                                    foreach (var field in componentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                    {
                                        field.SetValue(newComponent, field.GetValue(component));
                                    }
                                }
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("nicknameAndKarmaTransform not found.");
                            }

                            if (levelGroup != null)
                            {
                                RectTransform rectTransform = levelGroup.GetComponent<RectTransform>();
                                if (rectTransform != null)
                                {
                                    rectTransform.anchorMin = new Vector2(0f, 0f); // Left top anchor
                                    rectTransform.anchorMax = new Vector2(0f, 0f); // Left top anchor
                                    rectTransform.pivot = new Vector2(0f, 0f); // Left top pivot
                                }
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("Level Panel not found within BottomField.");
                            }

                            if (playerLevelView != null && playerLevelIconView != null)
                            {
                                GameObject clonedPlayerLevelIconView = GameObject.Instantiate(playerLevelIconView, levelGroup.transform);
                                clonedPlayerLevelIconView.name = "Level Icon";
                                clonedPlayerLevelIconView.SetActive(true);

                                GameObject clonedPlayerLevelView = GameObject.Instantiate(playerLevelView, levelGroup.transform);
                                clonedPlayerLevelView.name = "Level";
                                clonedPlayerLevelView.SetActive(true);

                                // Set the sibling index to ensure Level is after Level Icon
                                clonedPlayerLevelView.transform.SetSiblingIndex(clonedPlayerLevelIconView.transform.GetSiblingIndex() + 1);
                            }

                            // Set the sibling index to ensure LevelGroup is before NicknameAndKarma
                            levelGroup.transform.SetSiblingIndex(0);

                            // Adjust the HorizontalLayoutGroup properties for each child
                            foreach (Transform child in bottomFieldTransform)
                            {
                                HorizontalLayoutGroup horizontalLayoutGroup = child.GetComponent<HorizontalLayoutGroup>();
                                if (horizontalLayoutGroup != null)
                                {
                                    horizontalLayoutGroup.childForceExpandWidth = false; // Ensure children do not expand horizontally
                                    horizontalLayoutGroup.childForceExpandHeight = false; // Ensure children do not expand vertically
                                }
                                else
                                {
                                    Plugin.LogSource.LogWarning($"HorizontalLayoutGroup component not found on {child.name}.");
                                }

                                // Adjust the ContentSizeFitter properties for each child
                                ContentSizeFitter contentSizeFitter = child.GetComponent<ContentSizeFitter>();
                                if (contentSizeFitter != null)
                                {
                                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Ensure the preferred size is used for vertical fitting
                                }
                                else
                                {
                                    Plugin.LogSource.LogWarning($"ContentSizeFitter component not found on {child.name}.");
                                }
                            }

                            // Update the nickname and experience count
                            UpdatePlayerStats(bottomFieldTransform);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("BottomField not found.");
                        }

                        // Locate the SideImage GameObject and hide it
                        Transform sideImageTransform = clonedPlayerModelView.transform.Find("SideImage");
                        if (sideImageTransform != null)
                        {
                            sideImageTransform.gameObject.SetActive(false);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("SideImage GameObject not found.");
                        }

                        Transform dragTriggerTransform = clonedPlayerModelView.transform.Find("DragTrigger");
                        if (dragTriggerTransform != null)
                        {
                            dragTriggerTransform.gameObject.SetActive(false);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("DragTrigger GameObject not found.");
                        }

                        // playerModelViewScript default: PlayerVisualRepresentation playerVisual, InventoryController inventoryController = null, Action onCreated = null, float update = 0f, Vector3? position = null, bool animateWeapon = true
                        await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, false);
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("PlayerModelView script not found on the PlayerModelViewObject.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("PlayerModelView GameObject or MenuScreen parent not found.");
                }
            }
            else
            {
                GameObject mainMenuPlayerModelView = GameObject.Find("Common UI/Common UI/MenuScreen/MainMenuPlayerModelView");
                if (mainMenuPlayerModelView != null)
                {
                    PlayerModelView playerModelViewScript = mainMenuPlayerModelView.GetComponentInChildren<PlayerModelView>();
                    if (playerModelViewScript != null)
                    {
                        playerModelViewScript.Close();
                        await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0f, null, false);
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("PlayerModelView script not found on the MainMenuPlayerModelView.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("MainMenuPlayerModelView not found.");
                }
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }


        private static void UpdatePlayerStats(Transform bottomFieldTransform)
        {
            // Update Nickname and Experience
            var nicknameAndKarmaTransform = bottomFieldTransform.Find("NicknameAndKarma");
            if (nicknameAndKarmaTransform != null)
            {
                var labelTransform = nicknameAndKarmaTransform.Find("Label");
                if (labelTransform != null)
                {
                    TextMeshProUGUI nicknameTMP = labelTransform.GetComponent<TextMeshProUGUI>();
                    if (nicknameTMP != null)
                    {
                        nicknameTMP.text = PatchConstants.BackEndSession.Profile.Nickname;
                        nicknameTMP.fontSize = 36;
                        nicknameTMP.color = new Color(1f, 0.75f, 0.3f, 1f);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("Label component not found in NicknameAndKarma.");
                }

                var nicknameIconTransform = nicknameAndKarmaTransform.Find("AccountType");
                if (nicknameIconTransform != null)
                {
                    nicknameIconTransform.gameObject.SetActive(false);
                }
                else
                {
                    Plugin.LogSource.LogWarning("Icon component not found in NicknameAndKarma.");
                }
            }

            var experienceTransform = bottomFieldTransform.Find("Experience");
            if (experienceTransform != null)
            {
                var expValueTransform = experienceTransform.Find("ExpValue");
                if (expValueTransform != null)
                {
                    TextMeshProUGUI experienceTMP = expValueTransform.GetComponent<TextMeshProUGUI>();
                    if (experienceTMP != null)
                    {
                        var numberFormat = new NumberFormatInfo
                        {
                            NumberGroupSeparator = " ",
                            NumberDecimalDigits = 0
                        };
                        experienceTMP.text = PatchConstants.BackEndSession.Profile.Experience.ToString("N", numberFormat);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("TMP UI SubObject component not found in ExpValue.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("Experience GameObject not found in BottomField.");
            }

            // Update Player Level View
            var levelGroupTransform = bottomFieldTransform.Find("LevelGroup");
            if (levelGroupTransform != null)
            {
                var levelTextTransform = levelGroupTransform.Find("Level");
                if (levelTextTransform != null)
                {
                    TextMeshProUGUI levelTMP = levelTextTransform.GetComponent<TextMeshProUGUI>();
                    if (levelTMP != null)
                    {
                        levelTMP.text = PatchConstants.BackEndSession.Profile.Info.Level.ToString();
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("LevelText component not found in LevelGroup.");
                }

                // Update Player Level Icon View
                var iconImageTransform = levelGroupTransform.Find("Level Icon");
                if (iconImageTransform != null)
                {
                    Image iconImage = iconImageTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        int playerLevel = PatchConstants.BackEndSession.Profile.Info.Level;
                        PlayerLevelPanel.SetLevelIcon(iconImage, playerLevel);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("IconImage component not found in LevelGroup.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("LevelGroup GameObject not found in BottomField.");
            }
        }

        private static void InitializeSceneEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            HandleScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HandleScene(scene);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            HandleScene(scene);
        }

        private static void HandleScene(Scene scene)
        {
            // MenuUIScene, CommonUIScene, EnvironmentUIScene, PreloaderUIScene
            if (scene.name == "CommonUIScene")
            {
                var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
                if (environmentObjects == null)
                {
                    Plugin.LogSource.LogInfo("EnvironmentUI instances not found - Possibly starting a game.");
                    return;
                }

                ActivateSceneElements(environmentObjects);
                LayoutHelpers.DisableCameraMovement();
            }
        }

        private static void ActivateSceneElements(LayoutHelpers.EnvironmentObjects environmentObjects)
        {
            LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "panorama", false);
            LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "LampContainer", true);

            GameObject lampContainer = environmentObjects.FactoryLayout.transform.Find("LampContainer")?.gameObject;
            if (lampContainer != null)
            {
                LayoutHelpers.SetChildActive(lampContainer, "Lamp", true);
                LayoutHelpers.SetChildActive(lampContainer, "MultiFlare", false);
            }
            else
            {
                Plugin.LogSource.LogWarning("LampContainer GameObject not found.");
            }

            LayoutHelpers.SetPanoramaEmissionMap(environmentObjects.FactoryLayout);
        }
    }
}
