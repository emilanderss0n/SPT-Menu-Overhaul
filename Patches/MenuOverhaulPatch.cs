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

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("method_3", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance)
        {
            ButtonHelpers.SetupButtonIcons(__instance);
            await LoadPatchContent(__instance).ConfigureAwait(false);
            InitializeSceneEvents();
            HandleScene(SceneManager.GetActiveScene());
            ButtonHelpers.ProcessButtons(__instance);
            await AddPlayerModel().ConfigureAwait(false);
            SubscribeToSettingsChanges();
            UpdateSetElements();
        }

        private static void SubscribeToSettingsChanges()
        {
            Settings.EnableTopGlow.SettingChanged += OnSettingsChanged;
            Settings.EnableBackground.SettingChanged += OnSettingsChanged;
            Settings.EnableExtraShadows.SettingChanged += OnSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged += OnSettingsChanged;
            Settings.PositionLogotypeVertical.SettingChanged += OnSettingsChanged;
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
                Plugin.LogSource.LogWarning("CommonObj not found.");
            }

            if (environmentObjects.FactoryLayout != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "CustomPlane", Settings.EnableBackground.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("FactoryLayout not found.");
            }

            // Position the main parent decal_plane using settings
            Transform decalPlaneTransform = environmentObjects.FactoryLayout.transform.Find("decal_plane");
            if (decalPlaneTransform != null)
            {
                decalPlaneTransform.position = new Vector3(
                    Settings.PositionLogotypeHorizontal.Value,
                    Settings.PositionLogotypeVertical.Value,
                    0f);
            }
            else
            {
                Plugin.LogSource.LogWarning("decal_plane GameObject not found.");
            }

            LightHelpers.UpdateLights();
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
                        clonedPlayerModelView.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);


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

                        LightHelpers.SetupLights(clonedPlayerModelView);

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
