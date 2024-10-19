using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SPT.Reflection.Utils;
using System.IO;
using TMPro;
using UnityEngine.EventSystems;
using System.Globalization;

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
            // Load your patch content here
            await LoadPatchContent(__instance);

            // Subscribe to the sceneLoaded event
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Initial check for the active scene
            HandleScene(SceneManager.GetActiveScene());

            // Disable camera movement
            await DisableCameraMovement();

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
                    rectTransform.anchoredPosition = new Vector2(200, yOffset); // Adjust the x value as needed

                    // If the button is PlayButton, change the font size using DefaultUIButton
                    if (buttonName == "PlayButton")
                    {
                        FieldInfo playButtonField = typeof(MenuScreen).GetField("_playButton", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (playButtonField != null)
                        {
                            ModifyButtonText(playButtonField, __instance, "ESCAPE FROM TARKOV", 36);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("_playButton field not found in MenuScreen.");
                        }
                    }

                    // Special handling for ExitButtonGroup
                    if (buttonName == "ExitButtonGroup")
                    {
                        GameObject exitButton = buttonObject.transform.Find("ExitButton")?.gameObject;
                        if (exitButton != null)
                        {
                            SetButtonHoverColors(exitButton);
                            exitButton.transform.Find("Background")?.gameObject.SetActive(false);
                            GameObject SizeLabel = exitButton.transform.Find("SizeLabel")?.gameObject;
                            GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
                            GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
                            if (icon != null)
                            {
                                icon.SetActive(false);
                            }
                            else
                            {
                                Plugin.LogSource.LogWarning($"Icon not found in exitButton.");
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("ExitButton not found in ExitButtonGroup.");
                        }
                    }
                    else
                    {
                        SetButtonHoverColors(buttonObject);
                        buttonObject.transform.Find("Background")?.gameObject.SetActive(false);
                        GameObject SizeLabel = buttonObject.transform.Find("SizeLabel")?.gameObject;
                        GameObject iconContainer = SizeLabel.transform.Find("IconContainer")?.gameObject;
                        GameObject icon = iconContainer.transform.Find("Icon")?.gameObject;
                        if (icon != null)
                        {
                            icon.SetActive(false);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning($"Icon not found in {buttonName}.");
                        }

                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning($"{buttonName} not found in MenuScreen.");
                }
            }

            // Call AddPlayerModel
            await AddPlayerModel();
        }

        private static void SetButtonHoverColors(GameObject buttonObject)
        {
            if (buttonObject == null)
            {
                Plugin.LogSource.LogWarning("buttonObject is null.");
                return;
            }

            if (!buttonObject.TryGetComponent(out DefaultUIButton buttonComponent))
            {
                Plugin.LogSource.LogWarning("DefaultUIButton component not found on button.");
                return;
            }

            var buttonField = AccessTools.Field(typeof(DefaultUIButton), "_button");
            var animationField = AccessTools.Field(typeof(TweenAnimatedButton), "_animation");
            var normalLabelColorField = AccessTools.Field(typeof(DefaultUIButtonAnimation), "_normalLabelColor");
            var highlightedLabelColorField = AccessTools.Field(typeof(DefaultUIButtonAnimation), "_highlightedLabelColor");

            if (buttonField == null || animationField == null || normalLabelColorField == null || highlightedLabelColorField == null)
            {
                Plugin.LogSource.LogWarning("One or more fields could not be found using AccessTools.");
                return;
            }

            if (!(buttonField.GetValue(buttonComponent) is TweenAnimatedButton _button))
            {
                Plugin.LogSource.LogWarning("TweenAnimatedButton component not found on button.");
                return;
            }

            if (!(animationField.GetValue(_button) is DefaultUIButtonAnimation _animation))
            {
                Plugin.LogSource.LogWarning("DefaultUIButtonAnimation component not found on button.");
                return;
            }
            normalLabelColorField.SetValue(_animation, new Color(0.85f, 0.92f, 0.92f, 1f));
            highlightedLabelColorField.SetValue(_animation, new Color(1f, 0.75f, 0.3f, 1f));
        }

        private static Task LoadPatchContent(MenuScreen __instance)
        {
            HideGameObject(__instance, "_alphaWarningGameObject");
            HideGameObject(__instance, "_warningGameObject");

            return Task.CompletedTask; // Return a completed task to satisfy the method signature
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
                                prismEffects.useChromaticAberration = true;
                                prismEffects.useDof = true;
                                prismEffects.useExposure = true;
                                prismEffects.useAmbientObscurance = true;
                                prismEffects.tonemapType = Prism.Utils.TonemapType.ACES;
                                prismEffects.toneValues = new Vector3(3f, 0.2f, 0.5f);
                                prismEffects.exposureUpperLimit = 0.55f;
                                prismEffects.aoBias = 0.1f;
                                prismEffects.aoIntensity = 3f;
                                prismEffects.aoRadius = 0.2f;
                                prismEffects.aoBlurFilterDistance = 1.25f;
                                prismEffects.aoMinIntensity = 1f;
                                prismEffects.aoLightingContribution = 5f;
                                prismEffects.useBloom = true;
                                prismEffects.bloomIntensity = 0.04f;
                                prismEffects.bloomThreshold = 0.02f;
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
                                mainLight.color = new Color(0.7f, 1f, 1f, 1f);
                                mainLight.range = 2.8f;
                                mainLight.intensity = 0.4f;
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
                                hairLight.intensity = 0.5f;
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
                            bottomFieldTransform.localPosition = new Vector3(820f, -100f, 0f);

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
            else
            {
                Plugin.LogSource.LogWarning("NicknameAndKarma GameObject not found in BottomField.");
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

        private static void HandleScene(Scene scene)
        {
            // Find instances of EnvironmentUI in the active scene
            EnvironmentUI[] environmentUIs = UnityEngine.Object.FindObjectsOfType<EnvironmentUI>();
            if (environmentUIs.Length == 0)
            {
                Plugin.LogSource.LogWarning("EnvironmentUI instances not found.");
                return;
            }

            foreach (var environmentUI in environmentUIs)
            {
                // Find the specific GameObject named "EnvironmentUISceneFactory"
                GameObject environmentUISceneFactory = environmentUI.gameObject.transform.Find("EnvironmentUISceneFactory")?.gameObject;
                if (environmentUISceneFactory == null)
                {
                    Plugin.LogSource.LogWarning("EnvironmentUISceneFactory GameObject not found.");
                    continue;
                }

                // Find the child GameObject named "FactoryLayout"
                GameObject factoryLayout = environmentUISceneFactory.transform.Find("FactoryLayout")?.gameObject;
                if (factoryLayout != null)
                {
                    // Find and control the visibility of specific children
                    SetChildActive(factoryLayout, "panorama", true);
                    SetChildActive(factoryLayout, "decal_plane", true);
                    SetChildActive(factoryLayout, "LampContainer", true);

                    // Find the LampContainer and its children
                    GameObject lampContainer = factoryLayout.transform.Find("LampContainer")?.gameObject;
                    if (lampContainer != null)
                    {
                        SetChildActive(lampContainer, "Lamp", true);
                        SetChildActive(lampContainer, "MultiFlare", false);
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("FactoryLayout GameObject not found.");
                }

                // Find the FactoryCameraContainer and MainMenuCamera
                GameObject factoryCameraContainer = environmentUISceneFactory.transform.Find("FactoryCameraContainer")?.gameObject;
                if (factoryCameraContainer != null)
                {
                    GameObject mainMenuCamera = factoryCameraContainer.transform.Find("MainMenuCamera")?.gameObject;
                    if (mainMenuCamera != null)
                    {
                        // Disable the PhysicsSimulator component on MainMenuCamera
                        var physicsSimulator = mainMenuCamera.GetComponent<CW2.Animations.PhysicsSimulator>();
                        if (physicsSimulator != null)
                        {
                            physicsSimulator.enabled = false;
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("PhysicsSimulator component not found on MainMenuCamera.");
                        }
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("MainMenuCamera GameObject not found.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("FactoryCameraContainer GameObject not found.");
                }

                // Load and set the new EmissionMap for the panorama GameObject
                SetPanoramaEmissionMap(factoryLayout);
            }
        }

        private static void SetPanoramaEmissionMap(GameObject factoryLayout)
        {
            string mainMenuImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx", "plugins", "MenuOverhaul", "Resources", "background.jpg");
            GameObject panorama = factoryLayout.transform.Find("panorama")?.gameObject;

            if (panorama != null)
            {
                Renderer renderer = panorama.GetComponent<Renderer>();
                if (renderer != null)
                {
                    string[] materialNames = { "part1", "part2", "part3", "part4" };
                    byte[] fileData = File.ReadAllBytes(mainMenuImage);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);
                    texture = FlipTextureVertically(texture);
                    texture = FlipTextureHorizontally(texture);

                    List<Material> materials = new List<Material>();

                    foreach (string materialName in materialNames)
                    {
                        Material material = renderer.materials.FirstOrDefault(mat => mat.name.Contains(materialName));
                        if (material != null)
                        {
                            material.SetTexture("_EmissionMap", texture);
                            material.EnableKeyword("_EMISSION");
                            materials.Add(material);
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning($"Material with name containing '{materialName}' not found on panorama.");
                        }
                    }

                    // Hide the panorama GameObject
                    panorama.SetActive(false);

                    // Check if CustomPlane already exists
                    GameObject existingPlane = factoryLayout.transform.Find("CustomPlane")?.gameObject;
                    if (existingPlane == null)
                    {
                        // Create a new plane mesh inside FactoryLayout
                        GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        newPlane.name = "CustomPlane";
                        newPlane.transform.SetParent(factoryLayout.transform);

                        // Set the transforms
                        newPlane.transform.localPosition = new Vector3(0f, 0f, 5.399f);
                        newPlane.transform.position = new Vector3(0f, -999.999f, 5.399f);
                        newPlane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                        newPlane.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                        newPlane.transform.localScale = new Vector3(1.9f, 1f, 0.92f);

                        // Transfer other transforms from panorama object
                        newPlane.layer = panorama.layer;
                        newPlane.tag = panorama.tag;

                        // Apply the materials to the new plane
                        Renderer newPlaneRenderer = newPlane.GetComponent<Renderer>();
                        newPlaneRenderer.materials = materials.ToArray();

                        // Turn off shadow casting
                        newPlaneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("Renderer component not found on panorama.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("panorama GameObject not found in FactoryLayout.");
            }
        }

        private static Texture2D FlipTextureVertically(Texture2D original)
        {
            Texture2D flipped = new Texture2D(original.width, original.height);
            for (int y = 0; y < original.height; y++)
            {
                for (int x = 0; x < original.width; x++)
                {
                    flipped.SetPixel(x, original.height - y - 1, original.GetPixel(x, y));
                }
            }
            flipped.Apply();
            return flipped;
        }

        private static Texture2D FlipTextureHorizontally(Texture2D original)
        {
            int width = original.width;
            int height = original.height;
            Texture2D flipped = new Texture2D(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flipped.SetPixel(width - x - 1, y, original.GetPixel(x, y));
                }
            }

            flipped.Apply();
            return flipped;
        }

        private static void SetChildActive(GameObject parent, string childName, bool isActive)
        {
            Transform childTransform = parent.transform.Find(childName);
            if (childTransform != null)
            {
                childTransform.gameObject.SetActive(isActive);

                // Special handling for decal_plane
                if (childName == "decal_plane" && isActive)
                {
                    // Position the main parent decal_plane
                    childTransform.position = new Vector3(-1.9f, -999.4f, 0f);

                    Transform decalPlanePve = childTransform.Find("decal_plane_pve");
                    Transform decalPlane = childTransform.Find("decal_plane");

                    if (decalPlanePve != null)
                    {
                        decalPlanePve.gameObject.SetActive(true);
                    }

                    if (decalPlane != null)
                    {
                        decalPlane.gameObject.SetActive(false);
                    }
                }
                // Special handling for LampContainer
                if (childName == "LampContainer" && isActive)
                {
                    // Show the Lamp object within LampContainer
                    Transform lampTransform = childTransform.Find("Lamp");
                    if (lampTransform != null)
                    {
                        lampTransform.gameObject.SetActive(true);

                        // Show the nested Lamp object within the first Lamp
                        Transform nestedLampTransform = lampTransform.Find("Lamp");
                        if (nestedLampTransform != null)
                        {
                            nestedLampTransform.gameObject.SetActive(true);

                            Transform pointLightBulbTransform = nestedLampTransform.Find("Point light_bulb");
                            if (pointLightBulbTransform != null)
                            {
                                pointLightBulbTransform.gameObject.SetActive(true);

                                Light pointLight = pointLightBulbTransform.GetComponent<Light>();
                                if (pointLight != null)
                                {
                                    pointLight.color = new Color(1f, 1f, 1f, 1f);
                                }
                                else
                                {
                                    Plugin.LogSource.LogWarning("Light component not found on Point light_bulb.");
                                }

                                // Set the localPosition
                                pointLightBulbTransform.localPosition = new Vector3(-2.9435f, 1.2058f, 0.024f);
                            }

                            Transform bulbTransform = nestedLampTransform.Find("bulb");
                            if (bulbTransform != null)
                            {
                                bulbTransform.gameObject.SetActive(false);
                            }

                            Transform flareLampMenuTransform = nestedLampTransform.Find("flare_lampmenu");
                            if (flareLampMenuTransform != null)
                            {
                                flareLampMenuTransform.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("Nested Lamp GameObject not found within Lamp.");
                        }
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("Lamp GameObject not found within LampContainer.");
                    }
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"{childName} not found in {parent.name}.");
            }
        }

        private static void HideGameObject(MenuScreen instance, string fieldName)
        {
            FieldInfo fieldInfo = typeof(MenuScreen).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                GameObject gameObject = fieldInfo.GetValue(instance) as GameObject;
                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    Plugin.LogSource.LogWarning($"{fieldName} GameObject is null.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning($"{fieldName} field not found in MenuScreen.");
            }
        }

        private static void ModifyButtonText(FieldInfo buttonField, MenuScreen screen, string newText, int fontSize)
        {
            try
            {
                if (buttonField != null)
                {
                    DefaultUIButton button = (DefaultUIButton)buttonField.GetValue(screen);
                    if (button != null)
                    {
                        button.SetRawText(newText, fontSize);
                    }
                    else
                    {
                        Plugin.LogSource.LogWarning("DefaultUIButton component not found on button.");
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("Button field is null.");
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"Error modifying button text: {ex.Message}");
            }
        }

        private static async Task DisableCameraMovement()
        {
            GameObject environmentUI = null;

            for (int i = 0; i < 50; i++)  // Try up to 50 times (5 seconds total)
            {
                environmentUI = GameObject.Find("Environment UI");
                if (environmentUI != null)
                {
                    break;  // Found the object, break out of the loop
                }
                await Task.Delay(100);  // Wait 100ms before checking again
            }

            if (environmentUI == null)
            {
                Plugin.LogSource.LogWarning("Environment UI GameObject not found.");
                return;
            }

            // Find the EnvironmentUISceneFactory GameObject
            GameObject environmentUISceneFactory = environmentUI.transform.Find("EnvironmentUISceneFactory")?.gameObject;
            if (environmentUISceneFactory == null)
            {
                Plugin.LogSource.LogWarning("EnvironmentUISceneFactory GameObject not found.");
                return;
            }

            // Find the FactoryCameraContainer GameObject
            GameObject factoryCameraContainer = environmentUISceneFactory.transform.Find("FactoryCameraContainer")?.gameObject;
            if (factoryCameraContainer == null)
            {
                Plugin.LogSource.LogWarning("FactoryCameraContainer GameObject not found.");
                return;
            }

            // Find the MainMenuCamera GameObject
            GameObject mainMenuCamera = factoryCameraContainer.transform.Find("MainMenuCamera")?.gameObject;
            if (mainMenuCamera == null)
            {
                Plugin.LogSource.LogWarning("MainMenuCamera GameObject not found.");
                return;
            }

            mainMenuCamera.SetActive(false);

            // Find the FactoryLayout GameObject
            GameObject factoryLayout = environmentUISceneFactory.transform.Find("FactoryLayout")?.gameObject;
            if (factoryLayout == null)
            {
                Plugin.LogSource.LogWarning("FactoryLayout GameObject not found.");
                return;
            }

            GameObject alignmentCameraPos = environmentUI.transform.Find("AlignmentCamera")?.gameObject;
            if (alignmentCameraPos == null)
            {
                Plugin.LogSource.LogWarning("AlignmentCamera GameObject not found.");
                return;
            }

            // Move the AlignmentCamera into the FactoryLayout
            if (alignmentCameraPos.transform.parent != factoryLayout.transform)
            {
                alignmentCameraPos.transform.SetParent(factoryLayout.transform);
            }

            // Find or create the AlignmentCamera GameObject
            GameObject alignmentCamera = environmentUI.transform.Find("EnvironmentUISceneFactory/FactoryLayout/AlignmentCamera")?.gameObject;
            if (alignmentCamera == null)
            {
                // If AlignmentCamera is not found, create a new one
                alignmentCamera = new GameObject("AlignmentCamera");
                alignmentCamera.transform.SetParent(factoryLayout.transform); // Set it under FactoryLayout instead

                // Add a Camera component to the new GameObject
                Camera cameraComponent = alignmentCamera.AddComponent<Camera>();
                cameraComponent.fieldOfView = 80;
            }
            else
            {
                alignmentCamera.SetActive(true);

                // Set the fieldOfView to 80
                Camera cameraComponent = alignmentCamera.GetComponent<Camera>();
                if (cameraComponent != null)
                {
                    cameraComponent.fieldOfView = 80;
                }
                else
                {
                    Plugin.LogSource.LogWarning("Camera component not found on AlignmentCamera.");
                }
            }
        }
    }
}
