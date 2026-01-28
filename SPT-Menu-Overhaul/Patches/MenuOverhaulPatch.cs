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
        private static bool _layoutSettingsSubscribed;
        private static bool _sceneEventsInitialized;

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

                ButtonHelpers.SetupButtonIcons(__instance);
                await LoadPatchContent(__instance).ConfigureAwait(false);

                InitializeSceneEvents();
                HandleScene(SceneManager.GetActiveScene());

                ButtonHelpers.ProcessButtons(__instance);
                SubscribeToLayoutSettingsChanges();
                UpdateLayoutElements();
                LayoutHelpers.DisableCameraMovement();
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError(e.ToString());
            }
        }

        private static void SubscribeToLayoutSettingsChanges()
        {
            if (_layoutSettingsSubscribed) return;

            Settings.EnableTopGlow.SettingChanged += OnLayoutSettingsChanged;
            Settings.EnableBackground.SettingChanged += OnLayoutSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged += OnLayoutSettingsChanged;
            Settings.ScaleBackgroundX.SettingChanged += OnScaleBackgroundChanged;
            Settings.ScaleBackgroundY.SettingChanged += OnScaleBackgroundChanged;
            Settings.EnableExtraShadows.SettingChanged += OnLayoutSettingsChanged;

            _layoutSettingsSubscribed = true;
            Plugin.LogSource.LogDebug("Layout-specific settings changes subscribed.");
        }

        private static void UnsubscribeFromLayoutSettingsChanges()
        {
            if (!_layoutSettingsSubscribed) return;

            Settings.EnableTopGlow.SettingChanged -= OnLayoutSettingsChanged;
            Settings.EnableBackground.SettingChanged -= OnLayoutSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged -= OnLayoutSettingsChanged;
            Settings.ScaleBackgroundX.SettingChanged -= OnScaleBackgroundChanged;
            Settings.ScaleBackgroundY.SettingChanged -= OnScaleBackgroundChanged;
            Settings.EnableExtraShadows.SettingChanged -= OnLayoutSettingsChanged;

            _layoutSettingsSubscribed = false;
            Plugin.LogSource.LogDebug("Layout-specific settings changes unsubscribed.");
        }

        private static void CleanupSceneEvents()
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

            // Handle top glow
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
                // Handle custom plane
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, "CustomPlane", Settings.EnableBackground.Value);
                
                // Only update decal plane if we're not in game
                if (!Utility.IsInGame())
                {
                    // Update decal plane position and ensure it's active
                    Utility.ConfigureDecalPlane(true);
                    Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value);
                }
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateLayoutElements - FactoryLayout not found.");
            }

            // Update lighting
            LightHelpers.UpdateLights();
        }

        private static void UpdateCustomPlaneScale()
        {
            GameObject customPlane = LayoutHelpers.GetBackgroundPlane();
            if (customPlane != null)
            {
                customPlane.transform.localScale = new Vector3(Settings.ScaleBackgroundX.Value, 1f, Settings.ScaleBackgroundY.Value);
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
                var environmentObjects = LayoutHelpers.FindEnvironmentObjects();
                if (environmentObjects?.FactoryLayout != null)
                {
                    ActivateSceneLayoutElements(environmentObjects);
                }
                else
                {
                    Plugin.LogSource.LogWarning("MenuOverhaulPatch.HandleScene - EnvironmentObjects or FactoryLayout not found for CommonUIScene.");
                }
            }
        }

        private static void ActivateSceneLayoutElements(LayoutHelpers.EnvironmentObjects envObjects)
        {
            GameObject panorama = envObjects.FactoryLayout.transform.Find("panorama")?.gameObject;
            if (panorama != null)
            {
                panorama.SetActive(false);
            }

            LayoutHelpers.SetChildActive(envObjects.FactoryLayout, "LampContainer", true);

            LayoutHelpers.SetPanoramaEmissionMap(envObjects.FactoryLayout);

            GameObject customPlane = LayoutHelpers.GetBackgroundPlane();
            if (customPlane != null)
            {
                customPlane.SetActive(Settings.EnableBackground.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("ActivateSceneLayoutElements - CustomPlane not found after setup");
            }

            if (!Utility.IsInGame())
            {
                Utility.ConfigureDecalPlane(true);
                Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value);
            }
        }

        public void CleanupBeforeDisable()
        {
            UnsubscribeFromLayoutSettingsChanges();
            CleanupSceneEvents();
        }
    }
}