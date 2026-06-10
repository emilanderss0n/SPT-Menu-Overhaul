using EFT.UI;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Infrastructure.Lifecycle;
using MoxoPixel.MenuOverhaul.Utils;
using System.Threading.Tasks;
using UnityEngine;

namespace MoxoPixel.MenuOverhaul.Helpers.Services
{
    internal static class MainMenuLayoutService
    {
        public static Task ApplyMainMenuLayoutAsync(MenuScreen menuScreen)
        {
            MenuLifecycleCoordinator.EnsureScreenSubscription();
            MenuLifecycleCoordinator.EnsureLayoutSettingsSubscription();
            MenuScreenVisibilityPolicy.MarkMainMenuActive();

            HideMenuWarnings(menuScreen);

            MainMenuLayoutRuntime.EnvironmentObjects env = MainMenuLayoutRuntime.FindEnvironmentObjects();
            if (env != null && env.FactoryLayout != null)
            {
                ApplyEnvironmentLayout(env);
            }

            UpdateLayoutElements();
            MainMenuLayoutRuntime.DisableCameraMovement();

            return Task.CompletedTask;
        }

        public static void SubscribeToLayoutSettingsChanges()
        {
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
        }

        public static void UnsubscribeFromLayoutSettingsChanges()
        {
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
        }

        public static void UpdateLayoutElements()
        {
            MainMenuLayoutRuntime.EnvironmentObjects environmentObjects = MainMenuLayoutRuntime.FindEnvironmentObjects();
            if (environmentObjects == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Layout, "UpdateLayoutElements - Could not find environment objects.");
                return;
            }

            if (environmentObjects.CommonObj != null)
            {
                MainMenuLayoutRuntime.SetChildActive(environmentObjects.CommonObj, MenuOverhaulConstants.Environment.GlowCanvas, Settings.EnableTopGlow.Value);
                MainMenuLayoutRuntime.UpdateTopGlowColor(environmentObjects.CommonObj, Settings.AccentColor.Value);
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Layout, "UpdateLayoutElements - CommonObj not found.");
            }

            if (environmentObjects.FactoryLayout != null)
            {
                MainMenuLayoutRuntime.SetChildActive(environmentObjects.FactoryLayout, MenuOverhaulConstants.Environment.CustomPlane, Settings.EnableBackground.Value);
                MainMenuLayoutRuntime.UpdateLogotypeBulbLightColor(environmentObjects.FactoryLayout);

                if (!GameStateUtility.IsInGame())
                {
                    GameStateUtility.ConfigureDecalPlane(true);
                    GameStateUtility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value, Settings.PositionLogotypeVertical.Value);
                }
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Layout, "UpdateLayoutElements - FactoryLayout not found.");
            }

            MainMenuLightingService.UpdateLights();
        }

        private static void HideMenuWarnings(MenuScreen menuScreen)
        {
            if (menuScreen == null)
            {
                return;
            }

            MainMenuLayoutRuntime.HideGameObject(menuScreen, MenuOverhaulConstants.MenuScreen.AlphaWarningField);
            MainMenuLayoutRuntime.HideGameObject(menuScreen, MenuOverhaulConstants.MenuScreen.WarningField);
        }

        private static void ApplyEnvironmentLayout(MainMenuLayoutRuntime.EnvironmentObjects env)
        {
            Transform panoramaTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.Panorama);
            GameObject panorama = panoramaTransform != null ? panoramaTransform.gameObject : null;
            if (panorama != null)
            {
                panorama.SetActive(false);
            }

            MainMenuLayoutRuntime.SetChildActive(env.FactoryLayout, MenuOverhaulConstants.Environment.LampContainer, true);

            if (env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane) == null)
            {
                MainMenuLayoutRuntime.SetPanoramaEmissionMap(env.FactoryLayout);
            }

            Transform customPlaneTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane);
            GameObject customPlane = customPlaneTransform != null ? customPlaneTransform.gameObject : null;
            if (customPlane != null)
            {
                customPlane.SetActive(Settings.EnableBackground.Value);
            }

            if (!GameStateUtility.IsInGame())
            {
                GameStateUtility.ConfigureDecalPlane(true);
                GameStateUtility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value, Settings.PositionLogotypeVertical.Value);
            }
        }

        private static void UpdateCustomPlaneScale()
        {
            GameObject customPlane = MainMenuLayoutRuntime.GetBackgroundPlane();
            if (customPlane != null)
            {
                customPlane.transform.localScale = new Vector3(Settings.ScaleBackgroundX.Value, 1f, Settings.ScaleBackgroundY.Value);
            }
            else
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Layout, "UpdateCustomPlaneScale - CustomPlane (background) not found.");
            }
        }

        private static void OnLayoutSettingsChanged(object sender, System.EventArgs e) => UpdateLayoutElements();
        private static void OnScaleBackgroundChanged(object sender, System.EventArgs e) => UpdateCustomPlaneScale();
        private static void OnMenuIconVisibilityChanged(object sender, System.EventArgs e) => MainMenuButtonsService.UpdateMenuIconVisibility();
        private static void OnButtonGroupPositionChanged(object sender, System.EventArgs e) => MainMenuButtonsService.UpdateMenuButtonPositions();
    }
}
