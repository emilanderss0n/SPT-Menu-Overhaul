using EFT.UI;

namespace MoxoPixel.MenuOverhaul.Helpers.Services
{
    internal static class MainMenuButtonsService
    {
        public static void ApplyMainMenuButtons(MenuScreen menuScreen)
        {
            MainMenuButtonRuntime.SetupButtonIcons(menuScreen);
            MainMenuButtonRuntime.ProcessButtons(menuScreen);
            MainMenuButtonRuntime.RefreshButtonIdleState(menuScreen);
        }

        public static void UpdateMenuIconVisibility()
        {
            MainMenuButtonRuntime.UpdateMenuButtonIconVisibility();
        }

        public static void UpdateMenuButtonPositions()
        {
            MainMenuButtonRuntime.UpdateMenuButtonGroupPositions();
        }
    }
}
