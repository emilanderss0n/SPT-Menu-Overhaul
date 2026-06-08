using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Utils;
using EFT;

namespace MoxoPixel.MenuOverhaul.Patches
{
    internal class MenuOverhaulPatch : ModulePatch, ICleanupPatch
    {
        private static bool _layoutSettingsSubscribed;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MenuScreen).GetMethod(MenuOverhaulConstants.Reflection.MenuScreenShowMethod, [typeof(Profile), typeof(MatchmakerPlayerControllerClass), typeof(ESessionMode)
            ]);
        }

        [PatchPostfix]
        private static async void Postfix(MenuScreen __instance, Profile profile, MatchmakerPlayerControllerClass matchmaker)
        {
            try
            {
                if (__instance == null)
                {
                    Plugin.LogSource.LogWarning("MenuScreen instance is null.");
                    return;
                }

                // Only apply the overhaul to the actual main menu screen.
                // The in-raid Disconnect/Resume menu (MenuScreen.GClass3880) and the reconnect
                // menu (MenuScreen.GClass3879) both invoke Show with null profile/matchmaker;
                // skip those so the default game UI is preserved.
                if (profile == null || matchmaker == null || Utility.IsInGame())
                {
                    return;
                }

                // Mark the main menu active up-front so that DefaultUIButtonAnimation
                // idle/hover callbacks fired during button setup are styled by
                // SetAlphaPatch / TweenButtonPatch. Otherwise icons remain invisible
                // until the user hovers a button.
                MenuVisibilityController.EnsureSubscribed();
                MenuVisibilityController.MarkMainMenuActive();

                ButtonHelpers.SetupButtonIcons(__instance);
                await LoadPatchContent(__instance).ConfigureAwait(false);

                var env = LayoutHelpers.FindEnvironmentObjects();
                if (env != null && env.FactoryLayout != null)
                {
                    ApplyMenuLayout(env);
                }

                ButtonHelpers.ProcessButtons(__instance);
                SubscribeToLayoutSettingsChanges();
                UpdateLayoutElements();
                LayoutHelpers.DisableCameraMovement();

                // The game invokes DefaultUIButtonAnimation.method_1 (idle state)
                // during MenuScreen.Show BEFORE our postfix runs, so SetAlphaPatch
                // sees IsMainMenuActive == false and skips restoring icon alpha,
                // leaving icons invisible until the first hover. Re-trigger the
                // idle state on every button now that the gate is active.
                ButtonHelpers.RefreshButtonIdleState(__instance);
            }
            catch (Exception e)
            {
                Plugin.LogSource.LogError(e.ToString());
            }
        }

        private static void ApplyMenuLayout(LayoutHelpers.EnvironmentObjects env)
        {
            Transform panoramaTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.Panorama);
            GameObject panorama = panoramaTransform != null ? panoramaTransform.gameObject : null;
            if (panorama != null)
            {
                panorama.SetActive(false);
            }

            LayoutHelpers.SetChildActive(env.FactoryLayout, MenuOverhaulConstants.Environment.LampContainer, true);

            // One-shot: only create the CustomPlane if it does not exist yet.
            if (env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane) == null)
            {
                LayoutHelpers.SetPanoramaEmissionMap(env.FactoryLayout);
            }

            Transform customPlaneTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane);
            GameObject customPlane = customPlaneTransform != null ? customPlaneTransform.gameObject : null;
            if (customPlane != null)
            {
                customPlane.SetActive(Settings.EnableBackground.Value);
            }

            if (!Utility.IsInGame())
            {
                Utility.ConfigureDecalPlane(true);
                Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value, Settings.PositionLogotypeVertical.Value);
            }
        }

        private static void SubscribeToLayoutSettingsChanges()
        {
            if (_layoutSettingsSubscribed) return;

            Settings.EnableTopGlow.SettingChanged += OnLayoutSettingsChanged;
            Settings.EnableBackground.SettingChanged += OnLayoutSettingsChanged;
            Settings.EnableLogotypeBulbAccentColor.SettingChanged += OnLayoutSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged += OnLayoutSettingsChanged;
            Settings.PositionLogotypeVertical.SettingChanged += OnLayoutSettingsChanged;
            Settings.ScaleBackgroundX.SettingChanged += OnScaleBackgroundChanged;
            Settings.ScaleBackgroundY.SettingChanged += OnScaleBackgroundChanged;
            Settings.EnableExtraShadows.SettingChanged += OnLayoutSettingsChanged;
            Settings.EnableMenuButtonIcons.SettingChanged += OnMenuIconVisibilityChanged;
            Settings.PositionPlayButtonHorizontal.SettingChanged += OnButtonGroupPositionChanged;
            Settings.PositionCharacterButtonHorizontal.SettingChanged += OnButtonGroupPositionChanged;
            Settings.PositionTradeButtonHorizontal.SettingChanged += OnButtonGroupPositionChanged;
            Settings.PositionHideoutButtonHorizontal.SettingChanged += OnButtonGroupPositionChanged;
            Settings.PositionExitButtonHorizontal.SettingChanged += OnButtonGroupPositionChanged;
            Settings.AccentColor.SettingChanged += OnLayoutSettingsChanged;

            _layoutSettingsSubscribed = true;
            Plugin.LogSource.LogDebug("Layout-specific settings changes subscribed.");
        }

        private static void UnsubscribeFromLayoutSettingsChanges()
        {
            if (!_layoutSettingsSubscribed) return;

            Settings.EnableTopGlow.SettingChanged -= OnLayoutSettingsChanged;
            Settings.EnableBackground.SettingChanged -= OnLayoutSettingsChanged;
            Settings.EnableLogotypeBulbAccentColor.SettingChanged -= OnLayoutSettingsChanged;
            Settings.PositionLogotypeHorizontal.SettingChanged -= OnLayoutSettingsChanged;
            Settings.PositionLogotypeVertical.SettingChanged -= OnLayoutSettingsChanged;
            Settings.ScaleBackgroundX.SettingChanged -= OnScaleBackgroundChanged;
            Settings.ScaleBackgroundY.SettingChanged -= OnScaleBackgroundChanged;
            Settings.EnableExtraShadows.SettingChanged -= OnLayoutSettingsChanged;
            Settings.EnableMenuButtonIcons.SettingChanged -= OnMenuIconVisibilityChanged;
            Settings.PositionPlayButtonHorizontal.SettingChanged -= OnButtonGroupPositionChanged;
            Settings.PositionCharacterButtonHorizontal.SettingChanged -= OnButtonGroupPositionChanged;
            Settings.PositionTradeButtonHorizontal.SettingChanged -= OnButtonGroupPositionChanged;
            Settings.PositionHideoutButtonHorizontal.SettingChanged -= OnButtonGroupPositionChanged;
            Settings.PositionExitButtonHorizontal.SettingChanged -= OnButtonGroupPositionChanged;
            Settings.AccentColor.SettingChanged -= OnLayoutSettingsChanged;

            _layoutSettingsSubscribed = false;
            Plugin.LogSource.LogDebug("Layout-specific settings changes unsubscribed.");
        }

        private static Task LoadPatchContent(MenuScreen menuScreenInstance)
        {
            if (menuScreenInstance == null) return Task.CompletedTask;
            LayoutHelpers.HideGameObject(menuScreenInstance, MenuOverhaulConstants.MenuScreen.AlphaWarningField);
            LayoutHelpers.HideGameObject(menuScreenInstance, MenuOverhaulConstants.MenuScreen.WarningField);
            return Task.CompletedTask;
        }

        private static void OnLayoutSettingsChanged(object sender, EventArgs e) => UpdateLayoutElements();
        private static void OnScaleBackgroundChanged(object sender, EventArgs e) => UpdateCustomPlaneScale();
        private static void OnMenuIconVisibilityChanged(object sender, EventArgs e) => ButtonHelpers.UpdateMenuButtonIconVisibility();
        private static void OnButtonGroupPositionChanged(object sender, EventArgs e) => ButtonHelpers.UpdateMenuButtonGroupPositions();

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
                LayoutHelpers.SetChildActive(environmentObjects.CommonObj, MenuOverhaulConstants.Environment.GlowCanvas, Settings.EnableTopGlow.Value);
                LayoutHelpers.UpdateTopGlowColor(environmentObjects.CommonObj, Settings.AccentColor.Value);
            }
            else
            {
                Plugin.LogSource.LogWarning("UpdateLayoutElements - CommonObj not found.");
            }

            if (environmentObjects.FactoryLayout != null)
            {
                // Handle custom plane
                LayoutHelpers.SetChildActive(environmentObjects.FactoryLayout, MenuOverhaulConstants.Environment.CustomPlane, Settings.EnableBackground.Value);
                LayoutHelpers.UpdateLogotypeBulbLightColor(environmentObjects.FactoryLayout);
                
                // Only update decal plane if we're not in game
                if (!Utility.IsInGame())
                {
                    // Update decal plane position and ensure it's active
                    Utility.ConfigureDecalPlane(true);
                    Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value, Settings.PositionLogotypeVertical.Value);
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

        public void CleanupBeforeDisable()
        {
            UnsubscribeFromLayoutSettingsChanges();
        }
    }
}