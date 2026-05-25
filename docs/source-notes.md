# Game Source Notes — Menu / UI Internals

Notes harvested from the obfuscated SPT 4.x `Assembly-CSharp` dump under
`E:\Apps\SPTARKOV Tools\SPT_4\Assembly-CSharp\`. Obfuscated names (`GClass####`,
`method_##`) drift between SPT versions — re-verify after a game update.

This file documents the game source only. Mod-specific behavior and how the
findings are used live in `notes.md`.

## `EFT.UI.MenuScreen` (`EFT\UI\MenuScreen.cs`)

`public class MenuScreen : EftScreen<MenuScreen.GClass3877, MenuScreen>`

The single MonoBehaviour driving the main menu. The same component is reused for
three logical screens, distinguished by the `ScreenController` type assigned to
it.

### Public surface

- `void Show(Profile profile, MatchmakerPlayerControllerClass matchmaker, ESessionMode sessionMode)`
  - Invoked by `GClass3877.ShowAction` via the controller hierarchy below.
  - `profile` and `matchmaker` are non-null only for the real main menu
    (`GClass3878`). The other two controllers (`GClass3879`, `GClass3880`)
    construct the base with `(null, null, ESessionMode.Regular)`, so `Show` is
    invoked with both arguments `null`.
- `void method_3(bool reconnectAvailable)`
  - Toggles between normal menu and the "reconnect / disconnect available"
    layout. Flips `_alphaWarningGameObject` and `_warningGameObject` visibility.
- `void method_9(bool minimized)`
  - Used by `GClass3880` (in-raid pause). Hides Play / Trade / Hideout / Player /
    GameMode / Exit, shows `_disconnectButton` and `_hideScreenButton`.
- `void method_7()`
  - Resets environment rotation and calls `method_9(true)` (minimize).
- `void Init(EnvironmentUI environment)` — stores the `EnvironmentUI` reference
  used by the menu.
- `ECursorResult ShouldLockCursor() => ECursorResult.ShowCursor`
- `InputNode.ETranslateResult TranslateCommand(ECommand command)`
  - `ECommand.ToggleInventory` ? opens the Player menu when not
    minimized/reconnecting.
  - `ECommand.Escape` while `bool_2` (minimized) ? plays `MenuEscape` and calls
    `ScreenController.CloseScreen()` (this is the ESC handler for the in-raid
    pause).

### Private serialized fields (in order)

```text
_playButton              DefaultUIButton
_playerButton            DefaultUIButton
_tradeButton             DefaultUIButton
_exitButton              DefaultUIButton
_disconnectButton        DefaultUIButton   // only shown by GClass3880 (in-raid)
_hideoutButton           DefaultUIButton
_logoutButton            Button            // SetActive(false) in Awake
_toggleGameModeButton    ChangeGameModeButton
_hideScreenButton        DefaultUIButton   // only shown by GClass3880
_warningGameObject       GameObject        // reconnect warning
_alphaWarningGameObject  GameObject        // alpha-build warning banner
bool_1                   bool              // reconnectAvailable
environmentUI_0          EnvironmentUI
```

### Nested controller types

All inherit `MenuScreen.GClass3877 : CurrentScreenSingletonClass.GClass3861<MenuScreen>`.

| Type         | Purpose                            | TaskBarButtons | Constructed with                                  |
| ------------ | ---------------------------------- | -------------- | ------------------------------------------------- |
| `GClass3877` | Abstract base, holds profile/etc.  | n/a            | n/a                                               |
| `GClass3878` | Real main menu                     | Enabled        | `(profile, matchmaker, sessionMode)`              |
| `GClass3879` | Reconnect available menu           | Disabled       | `(null, null, Regular)` ? `method_3(true)`        |
| `GClass3880` | In-raid Disconnect/Resume (ESC)    | Disabled       | `(null, null, Regular)` ? `method_3(false)` + `method_7()` |

`GClass3878.ScreenType` returns `EEftScreenType.MainMenu` and
`MainEnvironment` / `ShowEnvironment` / `ShowEnvironmentCamera` are `Enabled`.

`GClass3880` exposes `event Action OnLeave` (raised by `Leave()`), wired in
`EftGamePlayerOwner.TranslateExitScreenInput`:

```csharp
public override bool TranslateExitScreenInput(ECommand command) {
    if (!base.TranslateExitScreenInput(command)) return false;
    var gclass = new MenuScreen.GClass3880();
    gclass.ShowScreen(EScreenState.Queued);
    gclass.OnLeave += base.OnLeaveHandler;
    return true;
}
```

### Click handlers (registered in `Awake`)

```text
_playButton       -> method_10 -> method_8(bool_1 ? Reconnect : Play)
_playerButton     -> method_11 -> method_8(Player)
_disconnectButton -> method_12 -> (ScreenController as GClass3880)?.Leave()
_tradeButton      -> method_13 -> method_8(Trade)
_hideoutButton    -> method_14 -> method_8(Hideout)
_exitButton       -> method_15 -> method_8(Exit)
_logoutButton     -> method_16 -> method_8(Logout)
_hideScreenButton -> method_17 -> ScreenController.CloseScreen()
```

`method_8(EMenuType)` is the central "menu item selected" dispatcher.

## `CurrentScreenSingletonClass` / `UserInterfaceClass<T>` (screen-change events)

- `CurrentScreenSingletonClass : UserInterfaceClass<EEftScreenType>`
- `CurrentScreenSingletonClass.Instance` — singleton accessor.
- `event Action<EEftScreenType> OnScreenChanged` — defined on
  `UserInterfaceClass<T>` (backing field `Action_0`), fired by the UI framework
  whenever the active screen identity changes.
- `CurrentBaseScreenController` setter also drives `MenuTaskBarVisibility`.

`MainMenuControllerClass.Unsubscribe()` detaches with
`CurrentScreenSingletonClass.Instance.OnScreenChanged -= this.method_0`,
confirming the event shape and that the game itself treats this as a core
lifecycle signal.

The `Environment UI` GameObjects (panorama, LampContainer, decal_plane,
Glow Canvas, AlignmentCamera, MainMenuCamera, the PlayerModelView prefab) are
persistent across screen transitions — they are not destroyed when entering
Hideout, Matchmaker, in-raid pause (`GClass3880`), Reconnect (`GClass3879`), or
BattleUI.

## `EFT.UI.Screens.EEftScreenType`

Screen-identity enum carried by `OnScreenChanged`. Selected values:

- `MainMenu`
- `Reconnect`
- `BattleUI`, `OfflineRaid`, `TimeHasCome`, `FinalCountdown`, `MatchMakerAccept`
- `Hideout`, `Trader`, `Inventory`

## `EFT.UI.CommonUI` (`EFT\UI\CommonUI.cs`)

- Field `public MenuScreen MenuScreen;`
- Registers / releases the menu screen via
  `instance.RegisterScreen<MenuScreen.GClass3877, MenuScreen>(EEftScreenType.MainMenu, ...)`
  and `ReleaseScreen<...>(EEftScreenType.MainMenu)`.
- The menu lives under the GameObject path `Common UI/Common UI/MenuScreen`.
- The inventory player model prefab lives under
  `Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView`.
- Level / Level Icon prefabs live under
  `.../CharacterPanel/Level Panel/Level` and `.../Level Panel/Level Icon`.

## `MainMenuControllerClass`

- `ShowMenuScreenSync()` ? `method_60().HandleExceptions()` — kicks off the
  async show of the main menu after returning from hideout etc.
- `Unsubscribe()` detaches the various session events the controller hooked
  (good reference for which game events are safe-ish to listen to).

## `EFT.EftGamePlayerOwner`

- `TranslateExitScreenInput` is where the in-raid ESC menu (`GClass3880`) is
  created and pushed onto the screen stack.

## Environment / scene GameObjects

Stable scene paths in recent SPT builds (worth re-checking on each update):

```text
Environment UI
Environment UI/Common
Environment UI/Common/Glow Canvas
Environment UI/EnvironmentUISceneFactory
Environment UI/EnvironmentUISceneFactory/FactoryLayout
Environment UI/EnvironmentUISceneFactory/FactoryLayout/panorama
Environment UI/EnvironmentUISceneFactory/FactoryLayout/decal_plane
Environment UI/EnvironmentUISceneFactory/FactoryLayout/LampContainer/Lamp/Lamp/Point light_bulb
Environment UI/EnvironmentUISceneFactory/FactoryCameraContainer/MainMenuCamera
Environment UI/AlignmentCamera
Common UI/Common UI/MenuScreen
Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView
Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level
Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/Level Panel/Level Icon
Menu UI/UI/Matchmaker Time Has Come
```

The `CommonUIScene` is the scene that hosts these objects; it is
loaded/unloaded by the game on transitions in/out of the main menu.

## `EFT.UI.DefaultUIButtonAnimation`

- `public void method_1(bool animated)` — normal/idle state handler. Reads
  `_normalIconColor`, `_normalLabelColor`, `_normalImageColor`, and
  `_backgorundNormalStateAplha`.
- `public void method_2(bool animated)` — highlighted state handler. Reads
  `_highlightedIconColor`, `_highlightedImageColor`.

The game itself calls `method_1(false)` on every button during
`MenuScreen.Show` (before any Harmony postfix runs) to establish the initial
idle state.

Private fields:

```text
_normalIconColor              Color
_normalLabelColor             Color
_normalImageColor             Color
_backgorundNormalStateAplha   float    // sic: original misspelling in game source
_highlightedIconColor         Color
_highlightedImageColor        Color
```

Public surface: `Stop()`, `Icon`, `Label`, `Image`, `ProcessTween(Tween)`,
`ProcessMultipleTweens(Tween[])`.

## `EFT.UI.PlayerModelView`

- `Task Show(Profile, ?, ?, float, ?, bool)` — async show. Positional
  parameters; the trailing arguments are typically `null` / `0f` / `false` for
  static menu rendering.
- `void Close()` — tears down the current display so a subsequent `Show` will
  rebuild it.

## `EFT.GameWorld.OnGameStarted`

Fires when a raid scene is entered.

## `EFT.Player.OnGameSessionEnd`

Fires when the local player's raid ends.

## `Profile.Info.OnExperienceChanged(int oldExp, int newExp)`

Emitted whenever the player's experience changes. Access path is
`PatchConstants.BackEndSession.Profile.Info`.

## `PrismEffects` (post-processing component)

Attached to `Camera_inventory` and (when present) `AlignmentCamera`. Fields:

```text
useChromaticAberration       bool
chromaticIntensity           float
chromaticDistanceOne         float
chromaticDistanceTwo         float
toneValues                   Vector3
exposureUpperLimit           float
useExposure                  bool
```

## Panorama material layout

The panorama renderer in `FactoryLayout/panorama` has 4 materials whose names
contain the substrings `part1` .. `part4`. The standard Unity `Standard` shader
is used; `_EmissionMap` + `_EMISSION` keyword drive the lit background look.

## Obfuscation drift checklist

When upgrading to a new SPT build, re-verify:

1. `MenuScreen.Show` signature is still
   `Show(Profile, MatchmakerPlayerControllerClass, ESessionMode)`.
2. The three controller subclass names (currently `GClass3878/3879/3880`) by
   searching `EFT\UI\MenuScreen.cs` for `: MenuScreen.GClass3877`.
3. `DefaultUIButtonAnimation.method_1` / `method_2` are still the
   normal/highlight handlers — search for `_normalImageColor` and
   `_highlightedImageColor` to find their owning methods.
4. The `_backgorundNormalStateAplha` field name is still misspelled.
5. `CurrentScreenSingletonClass.Instance` exists and
   `UserInterfaceClass<EEftScreenType>.OnScreenChanged` is still an
   `Action<EEftScreenType>` event.
6. `EEftScreenType.MainMenu` is still emitted for the real main menu (check
   `MenuScreen.GClass3878.ScreenType`).
