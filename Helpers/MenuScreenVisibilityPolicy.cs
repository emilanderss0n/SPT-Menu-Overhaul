using UnityEngine;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Helpers
{
    /// <summary>
    /// Tracks the active EFT screen and toggles the mod's environment-level
    /// customizations so they only apply while the real main menu is on-screen.
    ///
    /// - MenuScreen.GClass3878.ScreenType returns EEftScreenType.MainMenu and
    ///   MainEnvironment/ShowEnvironment/ShowEnvironmentCamera are Enabled. The
    ///   Environment UI GameObjects we mutate (CustomPlane, LampContainer,
    ///   decal_plane, AlignmentCamera, MainMenuCamera, Glow Canvas, cloned
    ///   player model) are persistent across screen transitions, so gating
    ///   MenuScreen.Show alone is not enough.
    /// - CurrentScreenSingletonClass : UserInterfaceClass&lt;EEftScreenType&gt;
    ///   fires OnScreenChanged(EEftScreenType) whenever the active screen
    ///   identity changes, which is the correct hook for hiding/showing them.
    /// </summary>
    internal static class MenuScreenVisibilityPolicy
    {
        /// <summary>
        /// True while EFT's current screen is the real main menu (and we are
        /// not in a raid). Other patches (button styling) gate on this so they
        /// do not affect the in-raid ESC menu (MenuScreen.GClass3880) or the
        /// reconnect menu (MenuScreen.GClass3879), which both reuse the
        /// MenuScreen GameObject and DefaultUIButtonAnimation instances.
        /// </summary>
        public static bool IsMainMenuActive { get; private set; }

        /// <summary>
        /// Show only the custom background plane (and hide the stock panorama)
        /// on screens listed in <see cref="IsBackgroundOnlyScreen"/>. All other
        /// modded elements stay hidden so the rest of the UI looks default.
        /// </summary>
        internal static void ShowBackgroundOnly()
        {
            try
            {
                IsMainMenuActive = false;

                if (PlayerProfileViewService.ClonedPlayerModelView != null
                    && PlayerProfileViewService.ClonedPlayerModelView.activeSelf)
                {
                    PlayerProfileViewService.ClonedPlayerModelView.SetActive(false);
                }

                var env = MainMenuLayoutRuntime.FindEnvironmentObjects();
                if (env != null && env.FactoryLayout != null)
                {
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.LampContainer, false);
                    // AlignmentCamera positions/renders the CustomPlane, so it
                    // must stay on whenever CustomPlane is visible.
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.AlignmentCamera, true);
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.DecalPlane, false);

                    Transform panoramaTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.Panorama);
                    GameObject panorama = panoramaTransform != null ? panoramaTransform.gameObject : null;
                    if (panorama != null && panorama.activeSelf)
                    {
                        panorama.SetActive(false);
                    }

                    Transform customPlane = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane);
                    if (customPlane != null)
                    {
                        customPlane.gameObject.SetActive(Settings.EnableBackground.Value);
                    }
                }

                // Leave MainMenuCamera in its default state (the modding/build
                // screens drive their own camera setup; we only changed it on
                // the actual main menu).

                if (env != null && env.CommonObj != null)
                {
                    Transform glowCanvas = env.CommonObj.transform.Find(MenuOverhaulConstants.Environment.GlowCanvas);
                    if (glowCanvas != null && glowCanvas.gameObject.activeSelf)
                    {
                        glowCanvas.gameObject.SetActive(false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Lifecycle, $"MenuScreenVisibilityPolicy.ShowBackgroundOnly error: {ex}");
            }
        }

        /// <summary>
        /// Called from MainMenuLayoutPatchAdapter.Postfix after the initial main-menu
        /// layout has been applied. Sets IsMainMenuActive so
        /// downstream patches (button styling) can opt in immediately, before
        /// the first OnScreenChanged callback fires.
        /// </summary>
        public static void MarkMainMenuActive()
        {
            IsMainMenuActive = true;
        }

        public static void HideCustomElements()
        {
            try
            {
                IsMainMenuActive = false;

                if (PlayerProfileViewService.ClonedPlayerModelView != null
                    && PlayerProfileViewService.ClonedPlayerModelView.activeSelf)
                {
                    PlayerProfileViewService.ClonedPlayerModelView.SetActive(false);
                }

                var env = MainMenuLayoutRuntime.FindEnvironmentObjects();
                if (env != null && env.FactoryLayout != null)
                {
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.CustomPlane, false);
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.LampContainer, false);
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.AlignmentCamera, false);
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.DecalPlane, false);

                    // Restore the stock panorama so non-main-menu screens that
                    // also use the FactoryLayout environment (Hideout etc.)
                    // get the default look back.
                    Transform panoramaTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.Panorama);
                    GameObject panorama = panoramaTransform != null ? panoramaTransform.gameObject : null;
                    if (panorama != null && !panorama.activeSelf)
                    {
                        panorama.SetActive(true);
                    }
                }

                if (env != null && env.EnvironmentUISceneFactory != null)
                {
                    Transform factoryCameraContainer = env.EnvironmentUISceneFactory.transform.Find(MenuOverhaulConstants.Environment.FactoryCameraContainer);
                    if (factoryCameraContainer != null)
                    {
                        Transform mainMenuCamera = factoryCameraContainer.Find(MenuOverhaulConstants.Environment.MainMenuCamera);
                        if (mainMenuCamera != null && !mainMenuCamera.gameObject.activeSelf)
                        {
                            mainMenuCamera.gameObject.SetActive(true);
                        }
                    }
                }

                if (env != null && env.CommonObj != null)
                {
                    Transform glowCanvas = env.CommonObj.transform.Find(MenuOverhaulConstants.Environment.GlowCanvas);
                    if (glowCanvas != null && glowCanvas.gameObject.activeSelf)
                    {
                        glowCanvas.gameObject.SetActive(false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Lifecycle, $"MenuScreenVisibilityPolicy.HideCustomElements error: {ex}");
            }
        }

        public static void ShowCustomElements()
        {
            try
            {
                IsMainMenuActive = true;

                if (PlayerProfileViewService.ClonedPlayerModelView != null)
                {
                    if (!PlayerProfileViewService.ClonedPlayerModelView.activeSelf)
                    {
                        PlayerProfileViewService.ClonedPlayerModelView.SetActive(true);
                    }
                    MainMenuLightingService.SetupLights(PlayerProfileViewService.ClonedPlayerModelView);
                }

                var env = MainMenuLayoutRuntime.FindEnvironmentObjects();
                if (env != null && env.FactoryLayout != null)
                {
                    Transform panoramaTransform = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.Panorama);
                    GameObject panorama = panoramaTransform != null ? panoramaTransform.gameObject : null;
                    if (panorama != null && panorama.activeSelf)
                    {
                        panorama.SetActive(false);
                    }

                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.LampContainer, true);
                    SetChildActiveIfPresent(env.FactoryLayout, MenuOverhaulConstants.Environment.AlignmentCamera, true);

                    Transform customPlane = env.FactoryLayout.transform.Find(MenuOverhaulConstants.Environment.CustomPlane);
                    if (customPlane != null)
                    {
                        customPlane.gameObject.SetActive(Settings.EnableBackground.Value);
                    }
                }

                if (env != null && env.EnvironmentUISceneFactory != null)
                {
                    Transform factoryCameraContainer = env.EnvironmentUISceneFactory.transform.Find(MenuOverhaulConstants.Environment.FactoryCameraContainer);
                    if (factoryCameraContainer != null)
                    {
                        Transform mainMenuCamera = factoryCameraContainer.Find(MenuOverhaulConstants.Environment.MainMenuCamera);
                        if (mainMenuCamera != null && mainMenuCamera.gameObject.activeSelf)
                        {
                            mainMenuCamera.gameObject.SetActive(false);
                        }
                    }
                }

                if (env != null && env.CommonObj != null)
                {
                    Transform glowCanvas = env.CommonObj.transform.Find(MenuOverhaulConstants.Environment.GlowCanvas);
                    if (glowCanvas != null)
                    {
                        glowCanvas.gameObject.SetActive(Settings.EnableTopGlow.Value);
                    }
                }

                GameStateUtility.ConfigureDecalPlane(true);
                GameStateUtility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value, Settings.PositionLogotypeVertical.Value);
            }
            catch (System.Exception ex)
            {
                MenuDiagnosticsLogger.Error(LogSubsystem.Lifecycle, $"MenuScreenVisibilityPolicy.ShowCustomElements error: {ex}");
            }
        }

        private static void SetChildActiveIfPresent(GameObject parent, string childName, bool isActive)
        {
            if (parent == null) return;
            Transform child = parent.transform.Find(childName);
            if (child != null && child.gameObject.activeSelf != isActive)
            {
                child.gameObject.SetActive(isActive);
            }
        }
    }
}
