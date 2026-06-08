# MenuOverhaul

MenuOverhaul is a BepInEx plugin designed to enhance and customize the main menu interface of the game. It provides users with options to change the visual appearance of the menu, including background elements, player model display, UI positioning, and lighting effects. 

Install the mod by dragging the first folder in the zip into your SPT install directory. Huge thanks to GrooveypenguinX for all the help and ability to look at your initial version of this menu. I also want to thank the SPT modding community in Discord. This is a WTT release.

> [!NOTE]
> Make sure to apply the Factory theme in the game menu before installing this mod.

![image](https://i.imgur.com/NZJPKny.jpeg)

[Go to the mod page](https://hub.sp-tarkov.com/files/file/2412-wtt-menu-overhaul)

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
    *   Enable a larger/closer player model framing.
    *   Enable extra shadows for a more detailed player model.
    *   Toggle high-quality player preview rendering (sharper image, higher GPU usage).
    *   Toggle default EFT player preview animation behavior.
*   **Player Information Panel:**
    *   Adjust the horizontal and vertical position of the player information text (level, nickname, etc.).
*   **Button & Animation Enhancements:**
    *   Modifies button icon and label appearances.
    *   Adjusts alpha and animations for UI elements for a cleaner look.
*   **Configuration:**
    *   In-game configuration options available via BepInEx ConfigurationManager.

---

![image](https://i.imgur.com/UVo352O.jpeg)

## Prerequisites

*   **BepInEx:** This plugin requires a BepInEx pack appropriate for the target game.
*   **BepInEx.ConfigurationManager:** (Recommended) For in-game configuration of the plugin settings.
*   **Target Game:** This plugin is designed for SPT (Single Player Tarkov).

## Configuration

This plugin can be configured through the BepInEx ConfigurationManager interface (accessible by pressing F12 in-game) or by editing the configuration file directly. The configuration file is located at `BepInEx/config/MoxoPixel.MenuOverhaul.cfg` after the first run.

Key settings include:

*   **General Settings:**
    *   `Enable Background`: Toggle the custom menu background.
    *   `Enable Top Glow`: Toggle the glow effect at the top of the menu.
    *   `Enable Extra Shadows`: Toggle additional shadows for the player model.
    *   `Enable Larger Player Model`: Make the player model appear larger/closer in the main menu.
    *   `Enable High Quality Player Preview`: Enable sharper player preview rendering (higher GPU cost).
    *   `Enable Default Player Animation`: Use EFT's default animated player preview behavior.
*   **Adjustment Settings:**
    *   `Position Logotype Horizontal`: Adjust the horizontal placement of the game's logo.
    *   `Position Player Model Horizontal`: Adjust the horizontal placement of the player character model.
    *   `Position Player Info Horizontal`: Adjust the horizontal placement of the player's information panel.
    *   `Position Player Info Vertical`: Adjust the vertical placement of the player's information panel.
    *   `Scale Background Horizontally`: Control the width of the background image.
    *   `Scale Background Vertically`: Control the height of the background image.
    *   `Rotate Player Model`: Control the rotation of the player character model.

Refer to the ConfigurationManager in-game for detailed descriptions and value ranges for each setting.

### Custom Assets (Icons and Background)

This mod now loads menu assets directly from files instead of an asset bundle.

- Icons are loaded from: `BepInEx/plugins/MoxoPixel.MenuOverhaul/Resources/icons/`
- Background textures are loaded from: `BepInEx/plugins/MoxoPixel.MenuOverhaul/Resources/background/`

Expected background file names:

- `background.jpg` (default)
- `background_ultrawide.jpg` (used on ultrawide aspect ratios)

Icon file names should match the internal icon keys used by the mod:

- `icon_play.png`
- `icon_mainmenu_character.png`
- `icon_trade.png`
- `hideout_icon_black.png`
- `exit_status_runner.png`

## For Developers

### Building

*   This project is written in C#.
*   It targets .NET Standard 2.1.
*   Dependencies include (all resolved from your local SPT install directory):
    *   **From `EscapeFromTarkov_Data/Managed/`:** `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `UnityEngine.UIModule.dll`, `UnityEngine.ImageConversionModule.dll`, `UnityEngine.AssetBundleModule.dll`, `UnityEngine.AudioModule.dll`, `UnityEngine.PhysicsModule.dll`, `UnityEngine.InputLegacyModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.TextRenderingModule.dll`, `Unity.TextMeshPro.dll`, `Unity.Postprocessing.Runtime.dll`, `DOTween.dll`, `DOTween.Modules.dll`, `Comfort.dll`, `Comfort.Unity.dll`, `Newtonsoft.Json.dll`, `Sirenix.Serialization.dll`, `uLipSync.Runtime.dll`
    *   **From `BepInEx/core/`:** `BepInEx.dll`, `0Harmony.dll`
    *   **From `BepInEx/plugins/spt/`:** `spt-common.dll`, `spt-reflection.dll`


## Contributing

Contributions are welcome! If you have suggestions, bug reports, or want to contribute code, please open an issue or submit a pull request on the GitHub repository.
