using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using MoxoPixel.MenuOverhaul.Infrastructure.Diagnostics;
using MoxoPixel.MenuOverhaul.Utils;
using System.Reflection;

namespace MoxoPixel.MenuOverhaul.Helpers.Services
{
    internal static class MenuGuardHelpers
    {
        public static MethodBase GetMenuScreenShowTargetMethod()
        {
            return typeof(MenuScreen).GetMethod(
                MenuOverhaulConstants.Reflection.MenuScreenShowMethod,
                [typeof(Profile), typeof(MatchmakerPlayersController), typeof(ESessionMode)]);
        }

        public static bool IsMainMenuShowContext(MenuScreen menuScreen, Profile profile, MatchmakerPlayersController matchmaker, string scope)
        {
            if (menuScreen == null)
            {
                MenuDiagnosticsLogger.Warning(LogSubsystem.General, $"{scope}: MenuScreen instance is null.");
                return false;
            }

            if (profile == null || matchmaker == null || GameStateUtility.IsInGame())
            {
                return false;
            }

            return true;
        }

        public static bool CanApplyButtonAnimation(DefaultUIButtonAnimation buttonAnimation)
        {
            if (buttonAnimation == null)
            {
                return false;
            }

            if (!MenuScreenVisibilityPolicy.IsMainMenuActive)
            {
                return false;
            }

            return MainMenuLayoutRuntime.IsPartOfMenuScreen(buttonAnimation);
        }
    }
}
