![image](https://i.imgur.com/UVo352O.jpeg)

# MenuOverhaul

MenuOverhaul is a BepInEx plugin designed to enhance and customize the main menu interface of the game. It provides users with options to change the visual appearance of the menu, including background elements, player model display, UI positioning, and lighting effects. Make sure to apply the Factory theme in game menu before installing this mod.

Install the mod by dragging the first folder in the zip into your SPT install directory. Huge thanks to GrooveypenguinX for all the help and ability to look at your initial version of this menu. I also want to thank the SPT modding community in Discord. This is a WTT release.

---

## Features

*   **Customizable Main Menu Background:**
    *   Enable or disable the custom background.
    *   Adjust horizontal and vertical scaling of the background image.
*   **Adjustable UI Elements:**
    *   Enable or disable the top glow effect in the menu.
    *   Modify the horizontal position of the game logotype.
*   **Enhanced Player Model Display:**
    *   Display player model in the main menu.
    *   Adjust the horizontal position of the player model.
    *   Adjust the horizontal rotation of the player model.
    *   Enable extra shadows for a more detailed player model.
*   **Player Information Panel:**
    *   Adjust the horizontal and vertical position of the player information text (level, nickname, etc.).
*   **Button & Animation Enhancements:**
    *   Modifies button icon and label appearances.
    *   Adjusts alpha and animations for UI elements for a cleaner look.
*   **Configuration:**
    *   In-game configuration options available via BepInEx ConfigurationManager.

## Prerequisites

*   **BepInEx:** This plugin requires a BepInEx pack appropriate for the target game.
*   **BepInEx.ConfigurationManager:** (Recommended) For in-game configuration of the plugin settings.
*   **Target Game:** This plugin is designed for a specific Unity game (likely a modded client of Escape From Tarkov, such as SPT-AKI, based on class names like `EFT.UI.MenuScreen`). Ensure compatibility with your game version.

## Installation

1.  Ensure BepInEx is installed and configured for your game.
2.  If you don't have it, install the [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) plugin (usually by placing its DLL into the `BepInEx/plugins` folder).
3.  Download the latest release of `MenuOverhaul.dll`.
4.  Place the `MenuOverhaul.dll` file into your game's `BepInEx/plugins` folder.
5.  Launch the game. If installed correctly, the plugin will load, and its effects will be visible in the main menu.

## Configuration

This plugin can be configured through the BepInEx ConfigurationManager interface (often accessible by pressing F1 in-game, if ConfigurationManager is installed) or by editing the configuration file directly. The configuration file is typically located at `BepInEx/config/MoxoPixel.MenuOverhaul.cfg` after the first run.

Key settings include:

*   **General Settings:**
    *   `Enable Background`: Toggle the custom menu background.
    *   `Enable Top Glow`: Toggle the glow effect at the top of the menu.
    *   `Enable Extra Shadows`: Toggle additional shadows for the player model.
*   **Adjustment Settings:**
    *   `Position Logotype Horizontal`: Adjust the horizontal placement of the game's logo.
    *   `Position Player Model Horizontal`: Adjust the horizontal placement of the player character model.
    *   `Position Player Info Horizontal`: Adjust the horizontal placement of the player's information panel.
    *   `Position Player Info Vertical`: Adjust the vertical placement of the player's information panel.
    *   `Scale Background Horizontally`: Control the width of the background image.
    *   `Scale Background Vertically`: Control the height of the background image.
    *   `Rotate Player Model`: Control the rotation of the player character model.

Refer to the ConfigurationManager in-game for detailed descriptions and value ranges for each setting.

## For Developers

### Building

*   This project is written in C#.
*   It targets .NET Framework 4.7.1.
*   Dependencies include:
    *   `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.ImageConversionModule.dll` (from the game's `Managed` folder)
    *   `Assembly-CSharp.dll` (from the game's `Managed` folder, or the specific version used by SPT-AKI)
    *   `BepInEx.dll`
    *   `0Harmony.dll`
    *   `BepInEx.PluginInfoProps.dll`
    *   `DOTween.dll` (likely from the game's `Managed` folder or included with BepInEx/SPT)
    *   `TextMeshPro-1.0.55.2017.1.0b12.dll` (or the version used by the game)

### Patching Approach

The plugin utilizes HarmonyX patches to modify game behavior:
*   `MenuOverhaulPatch`: Modifies `MenuScreen.Show` to apply general layout changes, background, and logotype adjustments.
*   `PlayerProfileFeaturesPatch`: Also patches `MenuScreen.Show` to add and configure the player model and its associated UI elements (like stats).
*   `SetAlphaPatch`: Modifies `DefaultUIButtonAnimation.method_1` to adjust button alpha and animations.
*   Other patches (e.g., `OnGameEndedPatch`, `OnGameStartedPatch`, `TweenButtonPatch`) handle specific game events or UI component behaviors.

## Contributing

Contributions are welcome! If you have suggestions, bug reports, or want to contribute code, please open an issue or submit a pull request on the GitHub repository.

[Go to the mod page](https://hub.sp-tarkov.com/files/file/2412-wtt-menu-overhaul)

---

![image](https://i.imgur.com/YMAtT5k.png)
