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

        private static Task LoadPatchContent(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null) return Task.CompletedTask;
            LayoutHelpers.HideGameObject(menuScreenInstance, "_alphaWarningGameObject");
            LayoutHelpers.HideGameObject(menuScreenInstance, "_warningGameObject");
            return Task.CompletedTask;
        }

        private static void OnLayoutSettingsChanged(object sender, EventArgs e) => UpdateLayoutElements();
        private static void OnScaleBackgroundChanged(object sender, EventArgs e) => UpdateCustomPlaneScale();

        private static void UpdateLayoutElements()
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
                    decalPlaneTransform.position = new Vector3(Settings.PositionLogotypeHorizontal.Value, -999.4f, 0f);
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

        private static void UpdateCustomPlaneScale()
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
            LayoutHelpers.SetChildActive(envObjects.FactoryLayout, "panorama", false);
            LayoutHelpers.SetChildActive(envObjects.FactoryLayout, "LampContainer", true);
            LayoutHelpers.SetPanoramaEmissionMap(envObjects.FactoryLayout);
            LayoutHelpers.SetChildActive(envObjects.FactoryLayout, "CustomPlane", Settings.EnableBackground.Value);
        }
    }
}
