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
    internal class MenuOverhaulPatch : ModulePatch // all patches must inherit ModulePatch
    {
        public static bool MenuPlayerCreated = false;

        protected override MethodBase GetTargetMethod()
        {
            // Target the method_3 method in the MenuScreen class
            return typeof(MenuScreen).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance)
        {
            GameObject isMainMenuScreen = GameObject.Find("Common UI/Common UI/MenuScreen");
            if (isMainMenuScreen == null)
            {
                return;
            }

            ButtonHelpers.SetButtonIconTransform(__instance, "PlayButton", new Vector3(0.8f, 0.8f, 0.8f), new Vector3(-48f, 0f, 0f));
            ButtonHelpers.SetButtonIconTransform(__instance, "TradeButton", new Vector3(0.8f, 0.8f, 0.8f));
            ButtonHelpers.SetButtonIconTransform(__instance, "HideoutButton", new Vector3(0.8f, 0.8f, 0.8f));
            ButtonHelpers.SetButtonIconTransform(__instance, "ExitButtonGroup", new Vector3(0.8f, 0.8f, 0.8f));

            await LoadPatchContent(__instance).ConfigureAwait(false);

            // Subscribe to the sceneLoaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Initial check for the active scene
            HandleScene(SceneManager.GetActiveScene());

            // List of button names to process
            string[] buttonNames = { "PlayButton", "CharacterButton", "TradeButton", "HideoutButton", "ExitButtonGroup" };

            foreach (var buttonName in buttonNames)
            {
                // Find the button object
                GameObject buttonObject = __instance.gameObject.transform.Find(buttonName)?.gameObject;
                if (buttonObject != null)
                {
                    // Adjust the RectTransform to position the text on the left side of the screen
                    RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.pivot = new Vector2(0, 0.5f);

                    // Calculate the y position based on the index and margin
                    int index = Array.IndexOf(buttonNames, buttonName);
                    float yOffset = -index * 60; // margin between buttons
                    rectTransform.anchoredPosition = new Vector2(250, yOffset); // Adjust the x value as needed

                    LayoutHelpers.SetIconImages(buttonObject, buttonName);

                    // If the button is PlayButton, change the font size using DefaultUIButton
                    if (buttonName == "PlayButton")
                    {
                        FieldInfo playButtonField = typeof(MenuScreen).GetField("_playButton", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (playButtonField != null)
                        {
                            ButtonHelpers.ModifyButtonText(playButtonField, __instance, fontSize: 36);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("_playButton field not found in MenuScreen.");
                        }

                        // Delay the visibility change to ensure it runs last
                        await Task.Delay(1);
                        GameObject SizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
                        GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
                        if (iconContainer != null)
                        {
                            iconContainer.SetActive(true);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning($"IconContainer not found for {buttonName}.");
                        }
                    }

                    // Special handling for ExitButtonGroup
                    if (buttonName == "ExitButtonGroup")
                    {
                        GameObject exitButton = buttonObject.transform.Find("ExitButton")?.gameObject;
                        if (exitButton != null)
                        {
                            exitButton.transform.Find("Background")?.gameObject.SetActive(false);
                            GameObject SizeLabel = exitButton.transform.Find("SizeLabel")?.gameObject;
                            GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
                            GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
                            if (icon != null)
                            {
                                icon.SetActive(true);
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning($"Icon not found in exitButton.");
                            }
                            LayoutHelpers.SetIconImages(exitButton, "ExitButton");
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("ExitButton not found in ExitButtonGroup.");
                        }
                    }
                    else
                    {
                        buttonObject.transform.Find("Background")?.gameObject.SetActive(false);
                        GameObject SizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
                        GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
                        GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
                        GameObject iconIdle = iconContainer.transform.Find("IconIdle")?.gameObject;
                        if (icon != null)
                        {
                            icon.SetActive(true);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning($"Icon not set active for {buttonName}.");
                        }
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning($"{buttonName} not found in MenuScreen.");
                }

            }

            // Call AddPlayerModel
            await AddPlayerModel().ConfigureAwait(false);

            Settings.EnableTopGlow.SettingChanged += OnSettingsChanged;
            Settings.EnableBackground.SettingChanged += OnSettingsChanged;
            UpdateSetElements();
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

            // Check if CommonObj is found
            if (environmentObjects.CommonObj != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.CommonObj, "Glow Canvas", Settings.EnableTopGlow.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("CommonObj not found.");
            }

            // Check if FactoryLayout is found
            if (environmentObjects.FactoryLayout != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "CustomPlane", Settings.EnableBackground.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("FactoryLayout not found.");
            }
        }

        private static async Task AddPlayerModel()
        {
            if (!MenuPlayerCreated)
            {
                GameObject playerModelView = GameObject.Find("Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView");
                GameObject menuScreenParent = GameObject.Find("Common UI/Common UI/MenuScreen");

                if (playerModelView != null && menuScreenParent != null)
                {
                    GameObject clonedPlayerModelView = GameObject.Instantiate(playerModelView, menuScreenParent.transform);
                    clonedPlayerModelView.name = "MainMenuPlayerModelView";
                    clonedPlayerModelView.SetActive(true);

                    MenuPlayerCreated = true;

                    PlayerModelView playerModelViewScript = clonedPlayerModelView.GetComponentInChildren<PlayerModelView>();
                    if (playerModelViewScript != null)
                    {
                        clonedPlayerModelView.transform.localPosition = new Vector3(400f, -250f, 0f);
                        clonedPlayerModelView.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f); // Set the scale


                        Transform cameraTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/Camera_inventory");
                        if (cameraTransform != null)
                        {
                            var prismEffects = cameraTransform.GetComponent<PrismEffects>();
                            if (prismEffects != null)
                            {
                                // Set the specified properties
                                prismEffects.useExposure = true;
                                prismEffects.useAmbientObscurance = true;
                                prismEffects.tonemapType = Prism.Utils.TonemapType.ACES;
                                prismEffects.toneValues = new Vector3(4f, 0.28f, 0.5f);
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

                        // Locate the Main Light
                        Transform mainLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Main Light");
                        if (mainLightTransform != null)
                        {
                            Light mainLight = mainLightTransform.GetComponent<Light>();
                            if (mainLight != null)
                            {
                                mainLight.color = new Color(0.5f, 0.7f, 1f, 1f);
                                mainLight.range = 2.8f;
                                mainLight.intensity = 0.4f;
                                if (Settings.EnableExtraShadows.Value)
                                {
                                    mainLight.shadows = LightShadows.Soft;
                                }
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("Light component not found on Main Light.");
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("Main Light GameObject not found.");
                        }

                        // Locate the Hair Light
                        Transform hairLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Hair Light");
                        if (hairLightTransform != null)
                        {
                            Light hairLight = hairLightTransform.GetComponent<Light>();
                            if (hairLight != null)
                            {
                                hairLight.intensity = 0.3f;
                                if (Settings.EnableExtraShadows.Value)
                                {
                                    hairLight.shadows = LightShadows.Soft;
                                }
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("Light component not found on Hair Light.");
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("Hair Light GameObject not found.");
                        }

                        // Locate the Fill Light
                        Transform fillLightTransform = clonedPlayerModelView.transform.Find("PlayerMVObject/PlayerMVObjectLights/Fill Light");
                        if (fillLightTransform != null)
                        {
                            Light fillLight = fillLightTransform.GetComponent<Light>();
                            if (fillLight != null)
                            {
                                fillLight.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("Light component not found on Fill Light.");
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("Fill Light GameObject not found.");
                        }

                        Transform bottomFieldTransform = clonedPlayerModelView.transform.Find("BottomField");
                        if (bottomFieldTransform != null)
                        {
                            // Set the VerticalLayoutGroup childAlignment to UpperLeft
                            VerticalLayoutGroup layoutGroup = bottomFieldTransform.GetComponent<VerticalLayoutGroup>();
                            if (layoutGroup != null)
                            {
                                layoutGroup.childAlignment = TextAnchor.UpperLeft;
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning("VerticalLayoutGroup component not found on BottomField.");
                            }

                            bottomFieldTransform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                            bottomFieldTransform.localPosition = new Vector3(840f, 0f, 0f);

                            // Update the nickname and experience count
                            UpdatePlayerStats(bottomFieldTransform);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("BottomField GameObject not found.");
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

                        // Locate the DragTrigger GameObject and hide it
                        Transform dragTriggerTransform = clonedPlayerModelView.transform.Find("DragTrigger");
                        if (dragTriggerTransform != null)
                        {
                            dragTriggerTransform.gameObject.SetActive(false);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("DragTrigger GameObject not found.");
                        }

                        await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0, null, true);
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
                        await playerModelViewScript.Show(PatchConstants.BackEndSession.Profile, null, null, 0, null, true);
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
            // Find the NicknameAndKarma object
            var nicknameAndKarmaTransform = bottomFieldTransform.Find("NicknameAndKarma");

            if (nicknameAndKarmaTransform != null)
            {
                // Find the Label component
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

                // Hide the icon inside NicknameAndKarma
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

            // Find the Experience object
            var experienceTransform = bottomFieldTransform.Find("Experience");
            if (experienceTransform != null)
            {
                // Find the ExpValue component
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
            if (scene.name == "CommonUIScene")
            {
                // Use the helper method to find environment objects
                var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
                if (environmentObjects == null)
                {
                    Plugin.LogSource.LogInfo("EnvironmentUI instances not found - Probably starting a game.");
                    return;
                }

                // Find and control the visibility of specific children
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "panorama", true);
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "decal_plane", true);
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "LampContainer", true);

                // Find the LampContainer and its children
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

                // Load and set the new EmissionMap for the panorama GameObject
                LayoutHelpers.SetPanoramaEmissionMap(environmentObjects.FactoryLayout);

                // Disable camera movement
                LayoutHelpers.DisableCameraMovement();
            }
        }
    }
}
