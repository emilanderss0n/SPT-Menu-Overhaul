# Change Notes

Running log of changes to this mod. See `source-notes.md` for neutral game-source
references and `REFACTOR.md` for the broader plan.

## How the mod maps to game source

Cross-references between mod files and `source-notes.md`. Re-verify after any
SPT update.

- `Patches/MenuOverhaulPatch.cs` and `Patches/PlayerProfileFeaturesPatch.cs`
  Harmony-postfix `EFT.UI.MenuScreen.Show(Profile, MatchmakerPlayerControllerClass, ESessionMode)`.
- `Patches/SetAlphaPatch.cs` patches `DefaultUIButtonAnimation.method_1` (idle
  state). It reflects the private fields `_normalIconColor`, `_normalLabelColor`,
  `_normalImageColor`, `_backgorundNormalStateAplha`.
- `Patches/TweenButtonPatch.cs` patches `DefaultUIButtonAnimation.method_2`
  (highlighted state). It reflects `_highlightedIconColor` and
  `_highlightedImageColor`.
- `Patches/OnGameStartedPatch.cs` patches `EFT.GameWorld.OnGameStarted`.
- `Patches/OnGameEndedPatch.cs` patches `EFT.Player.OnGameSessionEnd`.
- `Helpers/MenuVisibilityController.cs` subscribes to
  `CurrentScreenSingletonClass.Instance.OnScreenChanged` and gates persistent
  `Environment UI` mutations on `EEftScreenType.MainMenu`.
- `Patches/PlayerProfileFeaturesPatch.cs` reflects `_playButton` on `MenuScreen`
  and clones the inventory `PlayerModelView` from
  `Common UI/Common UI/InventoryScreen/Overall Panel/LeftSide/CharacterPanel/PlayerModelView`
  into `Common UI/Common UI/MenuScreen`. The bottom-field uses the Level and
  Level Icon prefabs from the same character panel.
- `Helpers/LayoutHelpers.cs` walks `Environment UI/...` (see `source-notes.md`
  for the full path list).
- `Helpers/LayoutHelpers.HideGameObject` reflects `_alphaWarningGameObject` and
  `_warningGameObject` on `MenuScreen`.
- `PlayerProfileFeaturesPatch.SubscribeToCharacterLevelUpEvent` subscribes to
  `PatchConstants.BackEndSession.Profile.Info.OnExperienceChanged`.

## 2025 — Restrict overhaul to the real main menu only

### Problem

The custom menu (player model, background plane, button icons, accent colors,
etc.) was being re-applied on every screen that uses `EFT.UI.MenuScreen`,
including:

- The in-raid Disconnect/Resume menu (ESC inside a raid).
- The reconnect / matchmaker screen.

Expected behavior: only the very first main menu (the one shown on game start)
should be customized; every other in-game menu should look and behave like
vanilla.

### Root cause

`MenuOverhaulPatch` and `PlayerProfileFeaturesPatch` both Harmony-postfix
`MenuScreen.Show`. That method is invoked by three different `ScreenController`
subclasses (see `source-notes.md` ? MenuScreen ? controller types):

| Controller   | Screen                         | profile / matchmaker |
| ------------ | ------------------------------ | -------------------- |
| `GClass3878` | Real main menu                 | non-null             |
| `GClass3879` | Reconnect / matchmaker         | null                 |
| `GClass3880` | In-raid Disconnect/Resume      | null                 |

The two `null`-arg variants were silently triggering the same postfix.

### Fix

Both postfixes now accept the Harmony-supplied `profile` and `matchmaker`
parameters and early-return when either is `null` or when we are already in a
game session (`Utility.IsInGame()` is flipped by `OnGameStartedPatch`).

## 2025 — Screen-change driven visibility

### Problem

The previous fix stopped the modded postfixes from running on non-main-menu
screens, but the `Environment UI` GameObjects (custom plane, lights, decal
plane, cloned player model) are persistent across screen transitions. They
remained visible on Hideout, Flea, Matchmaker, in-raid ESC, etc.

### Fix

Added `Helpers/MenuVisibilityController.cs`:

- Subscribes to `CurrentScreenSingletonClass.Instance.OnScreenChanged`
  (`Action<EEftScreenType>`).
- Exposes `IsMainMenuActive` so other patches can scope their behavior to the
  real main menu.
- `ShowCustomElements()` / `HideCustomElements()` toggle the persistent
  GameObjects (CustomPlane, LampContainer, AlignmentCamera, decal_plane,
  Glow Canvas, MainMenuCamera, and the cloned player model).
- Re-shows the original `panorama` GameObject when leaving the main menu so
  other screens see vanilla.

`SetAlphaPatch` and `TweenButtonPatch` now gate on
`MenuVisibilityController.IsMainMenuActive`, so button styling no longer leaks
into in-raid ESC or Reconnect (both reuse the same `MenuScreen` GameObject and
its `DefaultUIButtonAnimation` instances).

## 2025 — Panorama material bleed

### Problem

After the visibility fix, the custom panorama background still appeared on
every non-initial menu screen (Flea, Matchmaker, pause, etc.).

### Root cause

`LayoutHelpers.ApplyEmissionToPanoramaMaterials` was reading
`panoramaRenderer.materials` (which mutates the renderer's instanced materials)
and writing our emission texture into them. Even though we hid the original
`panorama` GameObject on the main menu, when other screens re-enabled it, it
rendered with our baked-in emission texture.

### Fix

In `LayoutHelpers.ApplyEmissionToPanoramaMaterials`:

- Read `panoramaRenderer.sharedMaterials` (does not require the renderer to be
  active, does not mutate the renderer's material array).
- Clone each source material via `new Material(source)` before setting the
  emission texture.
- Assign the clones to our `CustomPlane` only. The original panorama renderer
  is never touched, so other screens get the vanilla background back.

Removed the `panorama.SetActive(true)` / `SetActive(false)` toggle in
`SetPanoramaEmissionMap` — no longer needed and avoids a one-frame window where
another screen could pick up the temporarily-enabled custom panorama.

## 2025 — Main-menu icons hidden until first hover

### Problem

After the gating fixes, the initial main menu's button icons were invisible
until the user hovered a button.

### Root cause

The game calls `DefaultUIButtonAnimation.method_1(false)` (idle state) on every
button during `MenuScreen.Show`, before any Harmony postfix runs. The
`SetAlphaPatch` postfix saw `MenuVisibilityController.IsMainMenuActive == false`
during that initial pass (the controller is marked active by our postfix on
`MenuScreen.Show`, which runs *after* the original body completes) and skipped
restoring icon/label/image alpha. Icons stayed at alpha 0 until a hover-out
re-fired `method_1`.

### Fix

- `MenuOverhaulPatch.Postfix` now calls `MenuVisibilityController.EnsureSubscribed()`
  + `MarkMainMenuActive()` first thing after the main-menu gate.
- `ButtonHelpers.RefreshButtonIdleState(MenuScreen)` was added. It enumerates
  every `DefaultUIButtonAnimation` under the `MenuScreen` and re-invokes
  `method_1(false)` after our postfix has set the gate, so `SetAlphaPatch` runs
  in active-menu mode and the icon alpha is restored immediately.

## 2025 — Dead-code cleanup pass

Removed code that had no remaining callers:

- `LayoutHelpers.GetGlowCanvas()`
- `LayoutHelpers.IsMatchMaker()` (and the corresponding scene-path reference is
  documentation-only)
- `PlayerProfileFeaturesPatch.CleanupClonedPlayerModel()`
- Legacy "original Experience panel" fallback inside
  `PlayerProfileFeaturesPatch.UpdateExperienceDisplay` — `SetupBottomField`
  always destroys/recreates as `ExperienceRow`, so the fallback was
  unreachable.
- Stale comment in `OnGameStartedPatch.cs` that referenced already-deleted
  helpers; trimmed the unused `using MoxoPixel.MenuOverhaul.Helpers;`.

## Docs

- `docs/source-notes.md` — neutral findings from the obfuscated SPT
  `Assembly-CSharp` (no mod references).
- `docs/notes.md` — this file: mod-specific change history and mappings.
- `docs/REFACTOR.md` — broader cleanup plan.
