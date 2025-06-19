using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;
using EFT;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class MenuOverhaulPatch : ModulePatch
    {
        private static bool _layoutSettingsSubscribed = false;
        private static bool _sceneEventsInitialized = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod("Show", new[] { typeof(Profile), typeof(MatchmakerPlayerControllerClass), typeof(ESessionMode) });
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance)
        {
            GameObject playButton = GameObject.Find("Common UI/Common UI/MenuScreen/PlayButton")?.gameObject;
            if (__instance == null || playButton == null || !playButton.activeSelf)
            {
                Plugin.LogSource.LogWarning("MenuOverhaulPatch.Postfix: MenuScreen or PlayButton is null or inactive. Postfix will not run.");
                return;
            }

            ButtonHelpers.SetupButtonIcons(__instance);
            await LoadPatchContent(__instance).ConfigureAwait(false);

            InitializeSceneEvents();
            HandleScene(SceneManager.GetActiveScene());

            ButtonHelpers.ProcessButtons(__instance);

            SubscribeToLayoutSettingsChanges();

            UpdateLayoutElements();
        }

        private static void SubscribeToLayoutSettingsChanges()
        {
            if (_layoutSettingsSubscribed) return;

            Settings.EnableTopGlow.SettingChanged += OnLayoutSettingsChanged;
            Settings.EnableBackground.SettingChanged += OnLayoutSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged += OnLayoutSettingsChanged;
            Settings.scaleBackgroundX.SettingChanged += OnScaleBackgroundChanged;
            Settings.scaleBackgroundY.SettingChanged += OnScaleBackgroundChanged;
            Settings.EnableExtraShadows.SettingChanged += OnLayoutSettingsChanged;

            _layoutSettingsSubscribed = true;
            Plugin.LogSource.LogDebug("Layout-specific settings changes subscribed.");
        }

        public static void UnsubscribeFromLayoutSettingsChanges()
        {
            if (!_layoutSettingsSubscribed) return;

            Settings.EnableTopGlow.SettingChanged -= OnLayoutSettingsChanged;
            Settings.EnableBackground.SettingChanged -= OnLayoutSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged -= OnLayoutSettingsChanged;
            Settings.scaleBackgroundX.SettingChanged -= OnScaleBackgroundChanged;
            Settings.scaleBackgroundY.SettingChanged -= OnScaleBackgroundChanged;
            Settings.EnableExtraShadows.SettingChanged -= OnLayoutSettingsChanged;

            _layoutSettingsSubscribed = false;
            Plugin.LogSource.LogDebug("Layout-specific settings changes unsubscribed.");
        }

        public static void CleanupSceneEvents()
        {
            if (!_sceneEventsInitialized) return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            _sceneEventsInitialized = false;
            Plugin.LogSource.LogDebug("Scene events cleaned up by MenuOverhaulPatch.");
        }

        private static Task LoadPatchContent(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null) return Task.CompletedTask;
            LayoutHelpers.HideGameObject(menuScreenInstance, "_alphaWarningGameObject");
            LayoutHelpers.HideGameObject(menuScreenInstance, "_warningGameObject");
            return Task.CompletedTask;
        }

        private static void OnLayoutSettingsChanged(object sender, EventArgs e) => UpdateLayoutElements();
        private static void OnScaleBackgroundChanged(object sender, EventArgs e) => UpdateCustomPlaneScale();

        public static void UpdateLayoutElements()
        {
            var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
            if (environmentObjects == null)
            {
                Plugin.LogSource.LogWarning("UpdateLayoutElements - Could not find environment objects.");
                return;
            }

            if (environmentObjects.CommonObj != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.CommonObj, "Glow Canvas", Settings.EnableTopGlow.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateLayoutElements - CommonObj not found.");
            }

            if (environmentObjects.FactoryLayout != null)
            {
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "CustomPlane", Settings.EnableBackground.Value);
                Transform decalPlaneTransform = environmentObjects.FactoryLayout.transform.Find("decal_plane");
                if (decalPlaneTransform != null)
                {
                    // We no longer unconditionally call DisableDecalPlaneIfInGame()
                    // Instead, we're checking if we're in a game and only then disabling the plane
                    // This allows decal_plane to remain enabled when game ends

                    // Only update position if decal_plane is active
                    if (decalPlaneTransform.gameObject.activeSelf)
                    {
                        decalPlaneTransform.position = new Vector3(Settings.PositionLogotypeHorizontal.Value, -999.4f, 0f);

                        // Ensure decal_plane_pve is active when parent is active
                        Transform pveTransform = decalPlaneTransform.Find("decal_plane_pve");
                        if (pveTransform != null)
                        {
                            if (!pveTransform.gameObject.activeSelf)
                            {
                                pveTransform.gameObject.SetActive(true);
                                Plugin.LogSource.LogDebug("UpdateLayoutElements - Activated decal_plane_pve child object");
                            }
                        }
                        else
                        {
                            Plugin.LogSource.LogWarning("UpdateLayoutElements - Could not find decal_plane_pve child GameObject");
                        }

                        // And ensure child decal_plane is disabled
                        Transform childDecalPlane = decalPlaneTransform.Find("decal_plane");
                        if (childDecalPlane != null && childDecalPlane.gameObject.activeSelf)
                        {
                            childDecalPlane.gameObject.SetActive(false);
                            Plugin.LogSource.LogDebug("UpdateLayoutElements - Disabled child decal_plane GameObject");
                        }
                    }
                }
                else
                {
                    Plugin.LogSource.LogWarning("UpdateLayoutElements - decal_plane GameObject not found in FactoryLayout.");
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateLayoutElements - FactoryLayout not found.");
            }

            LightHelpers.UpdateLights();
        }

        public static void UpdateCustomPlaneScale()
        {
            var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
            if (environmentObjects?.FactoryLayout == null)
            {
                Plugin.LogSource.LogWarning("UpdateCustomPlaneScale - FactoryLayout not found.");
                return;
            }
            GameObject customPlane = LayoutHelpers.GetBackgroundPlane();
            if (customPlane != null)
            {
                customPlane.transform.localScale = new Vector3(Settings.scaleBackgroundX.Value, 1f, Settings.scaleBackgroundY.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateCustomPlaneScale - CustomPlane (background) not found.");
            }
        }

        private static void InitializeSceneEvents()
        {
            if (_sceneEventsInitialized) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            _sceneEventsInitialized = true;
            Plugin.LogSource.LogDebug("Scene events initialized by MenuOverhaulPatch.");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HandleScene(scene);
        private static void OnSceneUnloaded(Scene scene)
        {
            Plugin.LogSource.LogDebug($"Scene unloaded: {scene.name} (MenuOverhaulPatch)");
        }

        private static void HandleScene(Scene scene)
        {
            if (scene.name == "CommonUIScene")
            {
                Plugin.LogSource.LogDebug($"MenuOverhaulPatch: Handling CommonUIScene (Loaded: {scene.isLoaded})");
                var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
                if (environmentObjects?.FactoryLayout != null)
                {
                    ActivateSceneLayoutElements(environmentObjects);
                    LayoutHelpers.DisableCameraMovement();
                }
                else
                {
                    Plugin.LogSource.LogWarning("MenuOverhaulPatch.HandleScene - EnvironmentObjects or FactoryLayout not found for CommonUIScene.");
                }
            }
        }

        private static void ActivateSceneLayoutElements(LayoutHelpers.EnvironmentObjects envObjects)
        {
            // First make sure panorama is disabled - this needs to happen before creating custom plane
            GameObject panorama = envObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama != null)
            {
                panorama.SetActive(false);
                Plugin.LogSource.LogDebug("ActivateSceneLayoutElements - Explicitly disabled panorama");
            }

            // Then activate the lamp container for lighting
            LayoutHelpers.SetChildActive(envObjects.FactoryLayout, "LampContainer", true);

            // Now set up the custom plane with panorama emission map
            LayoutHelpers.SetPanoramaEmissionMap(envObjects.FactoryLayout);

            // Finally, set the custom plane visibility based on settings
            GameObject customPlane = envObjects.FactoryLayout.transform.Find("CustomPlane")?.gameObject;
            if (customPlane != null)
            {
                customPlane.SetActive(Settings.EnableBackground.Value);
                Plugin.LogSource.LogDebug("ActivateSceneLayoutElements - Set CustomPlane active state: " + Settings.EnableBackground.Value);
            }

            // Make sure decal_plane and decal_plane_pve are correctly set up
            GameObject decalPlane = envObjects.FactoryLayout.transform.Find("decal_plane")?.gameObject;
            if (decalPlane != null)
            {
                // Make sure parent decal_plane is active
                if (!decalPlane.activeSelf)
                {
                    decalPlane.SetActive(true);
                    Plugin.LogSource.LogDebug("ActivateSceneLayoutElements - Activated decal_plane GameObject");
                }

                // Make sure decal_plane_pve is active
                Transform pveTransform = decalPlane.transform.Find("decal_plane_pve");
                if (pveTransform != null)
                {
                    if (!pveTransform.gameObject.activeSelf)
                    {
                        pveTransform.gameObject.SetActive(true);
                        Plugin.LogSource.LogDebug("ActivateSceneLayoutElements - Activated decal_plane_pve child object");
                    }
                }

                // Ensure child decal_plane stays disabled
                Transform childDecalPlane = decalPlane.transform.Find("decal_plane");
                if (childDecalPlane != null && childDecalPlane.gameObject.activeSelf)
                {
                    childDecalPlane.gameObject.SetActive(false);
                    Plugin.LogSource.LogDebug("ActivateSceneLayoutElements - Disabled child decal_plane GameObject");
                }
            }
        }

        public void CleanupBeforeDisable()
        {
            UnsubscribeFromLayoutSettingsChanges();
            CleanupSceneEvents();
        }
    }
}