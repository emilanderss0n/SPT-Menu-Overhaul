using EFT.UI.Screens;
using MoxoPixel.MenuOverhaul.Helpers;
using MoxoPixel.MenuOverhaul.Helpers.Services;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Utils;

namespace MoxoPixel.MenuOverhaul.Infrastructure.Lifecycle
{
    internal static class MenuLifecycleCoordinator
    {
        private enum ScreenPolicy
        {
            MainMenu,
            BackgroundOnly,
            HideAll
        }

        private static bool _screenSubscribed;
        private static bool _layoutSettingsSubscribed;
        private static bool _profileSettingsSubscribed;
        private static bool _experienceSubscribed;

        private static bool _handlingScreenChange;
        private static bool _hasQueuedScreenChange;
        private static EEftScreenType _queuedScreenType;
        private static readonly object ScreenChangeLock = new object();
        private static bool _isShuttingDown;

        public static void EnsureScreenSubscription()
        {
            if (_screenSubscribed)
            {
                TraceSubscription("Screen", "AlreadySubscribed");
                return;
            }

            EftScreenManager singleton = EftScreenManager.Instance;
            if (singleton == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.Lifecycle, "Screen subscription deferred; EftScreenManager.Instance is null.");
                return;
            }

            singleton.OnScreenChanged += OnScreenChanged;
            _screenSubscribed = true;
            TraceSubscription("Screen", "Subscribed");
        }

        public static void EnsureLayoutSettingsSubscription()
        {
            if (_layoutSettingsSubscribed)
            {
                TraceSubscription("LayoutSettings", "AlreadySubscribed");
                return;
            }

            MainMenuLayoutService.SubscribeToLayoutSettingsChanges();
            _layoutSettingsSubscribed = true;
            TraceSubscription("LayoutSettings", "Subscribed");
        }

        public static void EnsureProfileSubscriptions()
        {
            if (!_profileSettingsSubscribed)
            {
                PlayerProfileViewService.SubscribeToProfileSettingsChanges();
                _profileSettingsSubscribed = true;
                TraceSubscription("ProfileSettings", "Subscribed");
            }
            else
            {
                TraceSubscription("ProfileSettings", "AlreadySubscribed");
            }

            if (!_experienceSubscribed)
            {
                PlayerProfileViewService.SubscribeToCharacterLevelUpEvent();
                _experienceSubscribed = true;
                TraceSubscription("ProfileExperience", "Subscribed");
            }
            else
            {
                TraceSubscription("ProfileExperience", "AlreadySubscribed");
            }
        }

        public static void CleanupOnUnload()
        {
            _isShuttingDown = true;

            lock (ScreenChangeLock)
            {
                _hasQueuedScreenChange = false;
                _handlingScreenChange = false;
            }

            if (_screenSubscribed)
            {
                EftScreenManager singleton = EftScreenManager.Instance;
                if (singleton != null)
                {
                    singleton.OnScreenChanged -= OnScreenChanged;
                }

                _screenSubscribed = false;
                TraceSubscription("Screen", "Unsubscribed");
            }

            if (_layoutSettingsSubscribed)
            {
                MainMenuLayoutService.UnsubscribeFromLayoutSettingsChanges();
                _layoutSettingsSubscribed = false;
                TraceSubscription("LayoutSettings", "Unsubscribed");
            }

            if (_profileSettingsSubscribed)
            {
                PlayerProfileViewService.UnsubscribeFromProfileSettingsChanges();
                _profileSettingsSubscribed = false;
                TraceSubscription("ProfileSettings", "Unsubscribed");
            }

            if (_experienceSubscribed)
            {
                PlayerProfileViewService.UnsubscribeFromCharacterLevelUpEvent();
                _experienceSubscribed = false;
                TraceSubscription("ProfileExperience", "Unsubscribed");
            }

            MenuScreenVisibilityPolicy.HideCustomElements();
        }

        private static void OnScreenChanged(EEftScreenType screenType)
        {
            if (_isShuttingDown)
            {
                return;
            }

            lock (ScreenChangeLock)
            {
                if (_handlingScreenChange)
                {
                    _queuedScreenType = screenType;
                    _hasQueuedScreenChange = true;
                    return;
                }

                _handlingScreenChange = true;
            }

            EEftScreenType currentScreenType = screenType;

            try
            {
                while (true)
                {
                    ApplyScreenPolicy(currentScreenType);

                    lock (ScreenChangeLock)
                    {
                        if (_hasQueuedScreenChange)
                        {
                            currentScreenType = _queuedScreenType;
                            _hasQueuedScreenChange = false;
                            continue;
                        }

                        _handlingScreenChange = false;
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                lock (ScreenChangeLock)
                {
                    _handlingScreenChange = false;
                }

                MenuDiagnosticsLogger.Error(LogSubsystem.Lifecycle, $"Screen change handler error: {ex}");
            }
        }

        private static void ApplyScreenPolicy(EEftScreenType screenType)
        {
            if (GameStateUtility.IsInGame())
            {
                MenuScreenVisibilityPolicy.HideCustomElements();
                return;
            }

            ScreenPolicy policy = ResolvePolicy(screenType);
            switch (policy)
            {
                case ScreenPolicy.MainMenu:
                    MenuScreenVisibilityPolicy.ShowCustomElements();
                    break;
                case ScreenPolicy.BackgroundOnly:
                    MenuScreenVisibilityPolicy.ShowBackgroundOnly();
                    break;
                default:
                    MenuScreenVisibilityPolicy.HideCustomElements();
                    break;
            }
        }

        private static ScreenPolicy ResolvePolicy(EEftScreenType screenType)
        {
            switch (screenType)
            {
                case EEftScreenType.MainMenu:
                    return ScreenPolicy.MainMenu;
                case EEftScreenType.WeaponModding:
                case EEftScreenType.EditBuild:
                case EEftScreenType.EquipmentBuilds:
                    return ScreenPolicy.BackgroundOnly;
                default:
                    return ScreenPolicy.HideAll;
            }
        }

        private static void TraceSubscription(string scope, string state)
        {
            MenuDiagnosticsLogger.Debug(LogSubsystem.Lifecycle, $"{scope}: {state}");
        }
    }
}
