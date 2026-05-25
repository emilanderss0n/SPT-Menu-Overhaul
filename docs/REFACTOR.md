# MenuOverhaul Refactor Plan

Goal: remove defensive / duplicated lifecycle code that is no longer needed now that the Harmony postfixes are correctly gated to the actual main menu screen, without changing any observable behavior of the mod.

This plan is written so it can be executed end-to-end without intermediate builds. A single build and manual test pass at the end is sufficient and is described in the acceptance section.

## Definition of "no observable behavior change"

After the refactor, every one of the following must still be true:

1. The initial main menu (game launch) shows: cloned player model, bottom field (nickname / level / XP), background `CustomPlane`, lamp, button icons, accent color, custom alignment camera, `_alphaWarningGameObject` and `_warningGameObject` hidden.
2. Changing any BepInEx config value at runtime updates the menu live (positions, colors, larger model toggle, top glow, extra shadows, background scale).
3. Returning to the main menu from the Hideout / Trader / Character screens still looks identical (no missing element, no duplicated `CustomPlane` / `MainMenuPlayerModelView`).
4. Entering a raid: the cloned player model is hidden, and no custom UI elements leak into the in-raid HUD.
5. Pressing ESC in raid (the `GClass3880` Disconnect/Resume menu) shows the vanilla layout (`_disconnectButton`, `_hideScreenButton`), with no cloned player model, no custom plane, and no recoloring.
6. The reconnect screen (`GClass3879`) also shows the vanilla layout.
7. Exiting a raid (death, extract, disconnect) restores the main menu to the same state as step 1, including XP updates after level-up.
8. Doing 4-7 repeatedly does not accumulate duplicate GameObjects, lights, or subscriptions, and does not produce error spam in BepInEx logs.

## Background - why the trim is safe

`EFT.UI.MenuScreen.Show(Profile, MatchmakerPlayerControllerClass, ESessionMode)` is invoked by three controller subclasses in the obfuscated assembly:

| Controller              | Screen                              | profile / matchmaker |
| ----------------------- | ----------------------------------- | -------------------- |
| `MenuScreen.GClass3878` | Real main menu                      | non-null             |
| `MenuScreen.GClass3879` | Reconnect / matchmaker              | null                 |
| `MenuScreen.GClass3880` | In-raid Disconnect/Resume (ESC)     | null                 |

Both Harmony postfixes (`MenuOverhaulPatch`, `PlayerProfileFeaturesPatch`) already early-return when `profile == null || matchmaker == null || Utility.IsInGame()`. This means the existing patch enable/disable cycling, scene-loaded re-entry, and "restore menu after raid" rebuild logic are now redundant. The next time the user returns to the main menu, `MenuScreen.Show` is invoked by the game, our gated postfix runs, and `AddPlayerModel` re-uses or recreates the cloned model via its existing `menuPlayerCreated && view == null` branch.

## Invariants the refactor must preserve

These are easy to break by accident. Call them out explicitly so the implementer does not regress them.

- `SetPanoramaEmissionMap` is expensive to re-run: `Renderer.materials` returns instanced copies on every access (so each call leaks fresh `Material` allocations), and the method then destroys and recreates the `CustomPlane` GameObject. It must run at most once per main-menu session to avoid allocation churn and a visible flicker. Guard: only call it when `factoryLayout.Find("CustomPlane") == null`.
- Setting-changed subscriptions must remain idempotent. The existing `_layoutSettingsSubscribed` / `_profileSettingsSubscribed` / `_experienceEventsSubscribed` flags already guarantee this. Keep them.
- `ClonedPlayerModelView` must not be destroyed on raid start unless we are prepared to recreate it on raid end. Today it is only hidden, keep that. `AddPlayerModel` already handles "view destroyed externally" via the `menuPlayerCreated && view == null` recreate branch, but we should not actively rely on that during normal raid flow.
- `AlignmentCamera` re-parenting is one-shot (gated by `isAlignmentCameraMoved`). Do not move it again on subsequent menu shows.
- BepInEx setting unsubscribes must happen exactly once on plugin teardown. Otherwise domain-reload scenarios leak handlers. Keep the existing subscribed-flag guards.
- `OnExperienceChanged` is subscribed against `PatchConstants.BackEndSession.Profile.Info`. This object survives across raids (it is the session profile, not the in-raid player). Do not unsubscribe on raid start.

## Work items

Each item describes the change, the rationale, and the safety guard that protects the invariants above.

### 1. `Utility` - drop scene-object caches

Change. Remove `cachedEnvironmentObjects`, `cachedDecalPlane`, the `GetDecalPlane()` cache helper, and `DisableDecalPlaneIfInGame()`. `ConfigureDecalPlane` and `SetDecalPlanePosition` look up `decal_plane` on demand via `LayoutHelpers.FindEnvironmentObjects()`. Also remove the `Utility.DisableDecalPlaneIfInGame()` invocation at the bottom of `LightHelpers.UpdateLights()` (the only call site outside `Utility` itself) so the helper can be deleted cleanly.

Keep:

- `isInGame` flag, `SetGameStarted(bool)`, `IsInGame()`, `ResetGameState()`.
- `ConfigureDecalPlane(bool)` - same body, but with the lookup inlined.
- `SetDecalPlanePosition(float)` - same body, but with the lookup inlined.

Rationale. Cached references point into the previous `CommonUIScene` after a scene reload and silently no-op. Looking up once per menu show is negligible. The defensive call in `UpdateLights` becomes unnecessary because the slim `OnGameStartedPatch` (item 4) explicitly hides the decal plane on raid start, and the gated postfix prevents `UpdateLayoutElements` from running during a raid through the main code path.

Safety. After this change, `DisableDecalPlaneIfInGame()` has zero callers. Grep for `DisableDecalPlaneIfInGame` and `cachedEnvironmentObjects` / `cachedDecalPlane` before deleting to confirm.

### 2. `MenuOverhaulPatch` - collapse scene events into the postfix

Change. Remove `_sceneEventsInitialized`, `InitializeSceneEvents`, `CleanupSceneEvents`, `OnSceneLoaded`, `OnSceneUnloaded`, and `HandleScene`. Inline the still-needed parts of `ActivateSceneLayoutElements` into a single private helper `ApplyMenuLayout(EnvironmentObjects)` that the postfix calls.

`ApplyMenuLayout` must:

1. Hide `panorama` (`SetActive(false)`).
2. Enable `LampContainer` via `LayoutHelpers.SetChildActive`.
3. Call `LayoutHelpers.SetPanoramaEmissionMap(factoryLayout)` only if `factoryLayout.Find("CustomPlane") == null` (the one-shot guard from the Invariants section).
4. `customPlane.SetActive(Settings.EnableBackground.Value)` if a `CustomPlane` exists (it will after step 3 has ever run).
5. If `!Utility.IsInGame()`: call `Utility.ConfigureDecalPlane(true)` and `Utility.SetDecalPlanePosition(Settings.PositionLogotypeHorizontal.Value)`.

`Postfix` flow becomes:

1. Existing null / profile / `IsInGame` guard.
2. `ButtonHelpers.SetupButtonIcons(__instance)`.
3. `LoadPatchContent(__instance)` (hide warning game objects).
4. `var env = LayoutHelpers.FindEnvironmentObjects(); if (env?.FactoryLayout != null) ApplyMenuLayout(env);`
5. `ButtonHelpers.ProcessButtons(__instance)`.
6. `SubscribeToLayoutSettingsChanges()`.
7. `UpdateLayoutElements()`.
8. `LayoutHelpers.DisableCameraMovement()`.

`CleanupBeforeDisable()` shrinks to just `UnsubscribeFromLayoutSettingsChanges()`.

Rationale. `MenuScreen.Show` is the canonical "menu is now visible" signal. The `SceneManager.sceneLoaded` callback was firing for the same logical event a second time, racing with the postfix.

Safety. The one-shot guard on `SetPanoramaEmissionMap` prevents material reassignment churn. The decal-plane setup keeps its `!IsInGame()` guard.

### 3. `LayoutHelpers.CleanupGameObjects` - delete

Change. Delete the method. It is currently only called from `OnGameStartedPatch`, which is being slimmed (item 4).

Rationale. Unity destroys scene objects on scene unload; we do not need to toggle them manually on raid start. The custom-plane and decal-plane state are re-derived from `Settings.EnableBackground` and `IsInGame` the next time the menu is shown.

Safety. Grep for `CleanupGameObjects` before deleting to confirm `OnGameStartedPatch` is the only caller.

### 4. `OnGameStartedPatch` - slim down

Final body:

```csharp
[PatchPostfix]
private static void PatchPostfix()
{
    Utility.SetGameStarted(true);

    if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null)
    {
        PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(false);
    }

    // Hide the decal plane explicitly. The previous implementation got this side-effect
    // from LayoutHelpers.CleanupGameObjects (item 3) and from
    // LightHelpers.UpdateLights -> DisableDecalPlaneIfInGame (item 1); both are removed.
    Utility.ConfigureDecalPlane(false);

    Plugin.LogSource.LogDebug("MenuOverhaul: game started, custom menu suspended.");
}
```

Remove.

- All `Disable()` calls on patches.
- `CleanupBeforeDisable()` invocations.
- `LayoutHelpers.CleanupGameObjects()` call.
- `LightHelpers.Cleanup()` call. It only nulls the cached `Light` references; it does not destroy the underlying `Light` GameObjects. The slim `OnGameEndedPatch` re-runs `LightHelpers.SetupLights(ClonedPlayerModelView)` which re-acquires the references, so clearing them on raid start adds no value but does add a window where `UpdateAccentLightColor` / `UpdateLights` become no-ops if a setting changes mid-raid.

Rationale. With the gated postfix, the patches are no-ops in-raid. Disabling them adds Harmony churn without benefit.

Safety. `IsInGame()` is set first, so any racing `MenuScreen.Show` invocation that fires before raid HUD takes over still early-returns. `ConfigureDecalPlane(false)` runs after the flag flips, so any concurrent `MenuOverhaulPatch` postfix already early-returned and will not re-enable it.

### 5. `OnGameEndedPatch` - slim down

Final body:

```csharp
[PatchPostfix]
private static void PatchPostfix()
{
    Utility.SetGameStarted(false);

    if (PlayerProfileFeaturesPatch.ClonedPlayerModelView != null)
    {
        PlayerProfileFeaturesPatch.ClonedPlayerModelView.SetActive(true);
        LightHelpers.SetupLights(PlayerProfileFeaturesPatch.ClonedPlayerModelView);
    }

    Plugin.LogSource.LogDebug("MenuOverhaul: game ended, custom menu re-armed.");
}
```

Remove. `Enable()` calls, `RestoreMenuUIElements`, `ResetOriginalState`, `RebuildCustomElements`, scene-name check.

Rationale. Returning to the main menu always triggers `MenuScreen.Show` again. Our postfix (now containing `ApplyMenuLayout`) rebuilds whatever is missing. Re-calling `LightHelpers.SetupLights` on the surviving cloned model restores any lights that depended on the in-raid camera being inactive.

Safety. `Utility.SetGameStarted(false)` is called before the model is shown, so the next `MenuScreen.Show` postfix is no longer gated out.

### 6. `Plugin` - single teardown path

Change.

- Remove `OnDisable()` entirely.
- Keep `OnDestroy()` calling `CleanupResources()`.
- `CleanupResources()` body:
  1. For each patch in `_patches`, if it is `MenuOverhaulPatch` or `PlayerProfileFeaturesPatch`, call `CleanupBeforeDisable()`.
  2. `LayoutHelpers.DisposeResources()` (already clears the texture cache and unloads the asset bundle).
  3. `LightHelpers.Cleanup()`.
  4. `Utility.ResetGameState()`.

Rationale. BepInEx plugins are not enabled/disabled at runtime; `OnDestroy` is the only reliable teardown hook. Duplicating it in `OnDisable` doubles unsubscribe calls, which is normally a no-op but masks future bugs.

Safety. Subscribed-flag guards already make double-unsubscribe a no-op, but removing the second call still removes the foot-gun.

## Files touched (final list)

- `SPT-Menu-Overhaul/Utils/Utility.cs` - caches removed.
- `SPT-Menu-Overhaul/Patches/MenuOverhaulPatch.cs` - scene events removed, layout helper inlined into `Postfix`.
- `SPT-Menu-Overhaul/Helpers/LayoutHelpers.cs` - `CleanupGameObjects` removed.
- `SPT-Menu-Overhaul/Patches/OnGameStartedPatch.cs` - slim body.
- `SPT-Menu-Overhaul/Patches/OnGameEndedPatch.cs` - slim body.
- `SPT-Menu-Overhaul/Plugin.cs` - `OnDisable` removed, `CleanupResources` shrunk.

Estimated reduction: 150-250 lines.

## Recommended execution order

No intermediate builds. These edits do not need to be tested individually because each leaves the project compiling on its own, and the contracts at the boundaries are unchanged.

1. `Utility.cs` - drop the caches and `DisableDecalPlaneIfInGame`.
2. `LayoutHelpers.cs` - delete `CleanupGameObjects`.
3. `MenuOverhaulPatch.cs` - fold scene handling into `Postfix`, shrink `CleanupBeforeDisable`, add the one-shot `SetPanoramaEmissionMap` guard.
4. `OnGameStartedPatch.cs` - replace body with the slim version.
5. `OnGameEndedPatch.cs` - replace body with the slim version.
6. `Plugin.cs` - delete `OnDisable`, shrink `CleanupResources`.

Then run a single solution build, then execute the acceptance test pass.

## Acceptance test pass (run once, in order)

Each step references the invariants from the "Definition of no observable behavior change" section.

1. Cold launch. Game starts, main menu fully customized (invariant 1).
2. Live config tweak. Open the BepInEx config menu (F12), toggle `EnableBackground`, change `AccentColor`, drag `PositionPlayerModelHorizontal`, toggle `EnableLargerPlayerModel`. All visible immediately (invariant 2).
3. Hideout round-trip. Click `HideoutButton`, then return to the main menu (via Home / back button). Menu state matches step 1, no duplicates (invariant 3).
4. Character round-trip. Same as step 3 with `CharacterButton`.
5. Enter raid. Pick a map, load in. Custom menu is gone, no HUD pollution (invariant 4).
6. In-raid ESC. Press ESC. Vanilla Disconnect/Resume menu, no cloned model (invariant 5). Press ESC again to resume.
7. Exit raid (extract or die). Return to main menu. Matches step 1, XP updated if relevant (invariant 7).
8. Repeat raid cycle. Repeat steps 5-7 a second time. Still no duplicate `CustomPlane` / `MainMenuPlayerModelView` under `Common UI/Common UI/MenuScreen` and `Environment UI/.../FactoryLayout` (invariant 8). Check the BepInEx console for new warnings or exceptions.
9. Reconnect screen sanity (best-effort). If reproducible (kill EFT process mid-raid then relaunch), confirm the reconnect screen shows vanilla layout (invariant 6). If not reproducible, document as untested.

Pass criteria: every invariant 1-8 holds. Skip 9 if not reproducible.

## Rollback plan

The refactor is a single logical change. If any acceptance step fails:

1. `git diff` against the pre-refactor commit to identify which file's change regressed the invariant.
2. Revert that single file, leave the rest in place, re-run the acceptance steps for the affected invariants.

Because the work items are decoupled (none of them depend on another's API changes), reverting one does not require reverting the others.

## Out of scope

- Renaming obfuscated symbols (`GClass####`, `method_##`).
- Restyling existing UI logic (button positions, font sizes, colors).
- Performance work beyond the cleanup itself.
- Cross-version compatibility shims for older / newer SPT builds.
