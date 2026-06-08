using UnityEngine;
using EFT.UI.Screens;
using MoxoPixel.MenuOverhaul.Patches;
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
    internal static class MenuVisibilityController
    {
        private static bool _subscribed;

        /// <summary>
        /// True while EFT's current screen is the real main menu (and we are
        /// not in a raid). Other patches (button styling) gate on this so they
        /// do not affect the in-raid ESC menu (MenuScreen.GClass3880) or the
        /// reconnect menu (MenuScreen.GClass3879), which both reuse the
        /// MenuScreen GameObject and DefaultUIButtonAnimation instances.
        /// </summary>
        public static bool IsMainMenuActive { get; private set; }

        public static void EnsureSubscribed()
        {
            if (_subscribed) return;

            var singleton = CurrentScreenSingletonClass.Instance;
            if (singleton == null)
            {
                Plugin.LogSource.LogWarning("MenuVisibilityController - CurrentScreenSingletonClass.Instance not available yet.");
                return;
            }

            singleton.OnScreenChanged += OnScreenChanged;
            _subscribed = true;
            Plugin.LogSource.LogDebug("MenuVisibilityController subscribed to OnScreenChanged.");
        }

        public static void Unsubscribe()
        {
            if (!_subscribed) return;

            var singleton = CurrentScreenSingletonClass.Instance;
            if (singleton != null)
            {
                singleton.OnScreenChanged -= OnScreenChanged;
            }

            _subscribed = false;
            IsMainMenuActive = false;
            Plugin.LogSource.LogDebug("MenuVisibilityController unsubscribed from OnScreenChanged.");
        }

        private static void OnScreenChanged(EEftScreenType screenType)
        {
            if (Utility.IsInGame())
            {
                HideCustomElements();
                return;
            }

            if (screenType == EEftScreenType.MainMenu)
            {
                ShowCustomElements();
            }
            else if (IsBackgroundOnlyScreen(screenType))
            {
                ShowBackgroundOnly();
            }
            else
            {
                HideCustomElements();
            }
        }

        /// <summary>
        /// Screens where the user wants the custom plane + background to also
        /// be visible, but without the rest of the main-menu customizations
        /// (cloned player model, lamps, alignment camera, glow, decal, etc.).
        /// </summary>
        private static bool IsBackgroundOnlyScreen(EEftScreenType screenType)
        {
            switch (screenType)
            {
                case EEftScreenType.WeaponModding:   // weapon and armor/item modding
                case EEftScreenType.EditBuild:       // building a weapon preset
                case EEftScreenType.EquipmentBuilds: // weapon/equipment build list
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Show only the custom background plane (and hide the stock panorama)
        /// on screens listed in <see cref="IsBackgroundOnlyScreen"/>. All other
        /// modded elements stay hidden so the rest of the UI looks default.
        /// </summary>
        private static void ShowBackgroundOnly()
        {
            try
            {
                IsMainMenuActive = false;

                if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null
                    && PlayerProfileFeaturesPatch.ClonedPlayerModelView.activeSelf)
                {
                    PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(false);
                }

                var env = LayoutHelpers.FindEnvironmentObjects();
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
                Plugin.LogSource.LogError($"MenuVisibilityController.ShowBackgroundOnly error: {ex}");
            }
        }

        /// <summary>
        /// Called from MenuOverhaulPatch.Postfix after the initial main-menu
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

                if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null
                    && PlayerProfileFeaturesPatch.ClonedPlayerModelView.activeSelf)
                {
                    PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(false);
                }

                var env = LayoutHelpers.FindEnvironmentObjects();
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
                Plugin.LogSource.LogError($"MenuVisibilityController.HideCustomElements error: {ex}");
            }
        }

        public static void ShowCustomElements()
        {
            try
            {
                IsMainMenuActive = true;

                if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null)
                {
                    if (!PlayerProfileFeaturesPatch.ClonedPlayerModelView.activeSelf)
                    {
                        PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(true);
                    }
                    LightHelpers.SetupLights(PlayerProfileFeaturesPatch.ClonedPlayerModelView);
                }

                var env = LayoutHelpers.FindEnvironmentObjects();
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

                Utility.ConfigureDecalPlane(true);
                Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value, Settings.PositionLogotypeVertical.Value);
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource.LogError($"MenuVisibilityController.ShowCustomElements error: {ex}");
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
