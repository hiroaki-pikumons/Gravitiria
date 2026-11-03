# Gravitiria: How the Code Fits Together

A guide for anyone opening the project for the first time. It follows the order in which things happen at runtime, then covers the editor tooling.

## Runtime overview

There is no bootstrap scene. Four systems create themselves on first load with `[RuntimeInitializeOnLoadMethod]` or on first use, and survive scene changes with `DontDestroyOnLoad`:

| System | Created | Owns |
|---|---|---|
| `GameSession` | After the first scene loads | Run timer, deaths, scores, records, HUD, pause menu, stage banner |
| `TouchControls` | After the first scene loads | On-screen touch UI; registers `TouchInputSource` with `GameInput` |
| `AutoPilot` | Only when launched with `-autopilot` | Scripted play-through for verification |
| `SubtitleManager` | On first message, from `Resources/SubtitleUI.prefab` | Typewriter subtitles and the dismiss hint |

Scenes are plain: geometry, a `Player` with `GravityController`, a `SceneFader`, lights, and prefab instances for lasers, switches, pads, enemies, messages and the goal. `GoalDoor.nextSceneName` is the only link between stages, which is why the chain is easy to re-order.

## The player: `GravityController`

- `gravityDirection` is a unit vector. `FixedUpdate` applies `gravityForce` along it and slerps the body so that `-transform.up` matches it.
- Movement input is turned into a world direction from the camera's forward and right, then projected onto the plane perpendicular to gravity. Without that projection, moving during a flip accumulated velocity along the old "up" and launched the player.
- Aiming (`GameInput.AimHeld`) sets `Time.timeScale` to 0.1 and `fixedDeltaTime` to match, zooms the FOV and darkens a vignette image. `AimReleased` calls `FlipGravityToAim()`, which raycasts from the viewport centre with `QueryTriggerInteraction.Ignore` and sets gravity to the negative hit normal.
- Dash (`TryDash`) and quick gravity (`TryQuickGravity`) are gated by `Progress` unlocks. Dash sets a burst velocity for `dashDuration`, grants `dashInvincibleTime`, and starts a cooldown. Hazards check `IsInvincible`.
- `GravityField.Set(gravityDirection)` is called every physics step. Enemies read `GravityField.Direction` so they always fall the player's way.
- The cursor is locked only when touch controls are not active (`TouchControls.Active`). On a phone there is no cursor; on a PC with the touch UI forced for testing, the mouse is used as a finger.
- `ResetStage()` registers a death with `GameSession` and asks `SceneFader` to fade and reload.
- For automation, `externalControl` bypasses input and `SetExternalMove`, `LookAtWorldPoint`, `FlipGravityToAim`, `TryDash`, `TryQuickGravity` are public.

## Input: `GameInput` and its sources

`GameInput` is a static facade. Gameplay code reads `GameInput.Move`, `GameInput.AimHeld`, `GameInput.DashPressed` and so on, never `UnityEngine.Input` directly. Behind it is a list of `IInputSource`:

- `KeyboardMouseInputSource`: WASD and arrows, mouse look, right button to aim, Shift to dash, Q/E/Z/C for quick gravity, Esc, R, T, Space, Enter, L, Delete.
- `GamepadInputSource`: legacy axes for sticks and D-pad (custom axes `DPadX/DPadY/RightStickX/RightStickY` are defined in `ProjectSettings/InputManager.asset`), buttons in XInput order. Holding aim turns the D-pad into quick gravity.
- `TouchInputSource`: converts `Input.touches` into the same properties. The left half of the screen is a floating stick, the right half is a look surface, and buttons are hit-tested through a delegate supplied by `TouchControls`. A finger on the FLIP button also feeds look deltas, so the player can aim while holding. It can also be driven by scripted touches (`Press`, `MoveTo`, `Release`) and falls back to the mouse as a single finger on desktop.

Each source is polled once per frame. Vectors are combined by taking the largest; buttons are OR-ed. Adding a new device means adding one class and one line.

## Hazards, devices and enemies

- `SecurityLaser` kills on contact and logs where. `LaserBeamVisual` draws the beam with the `Gravity/LaserBeam` shader (core, glow and halo layers, fresnel fade, moving packets) and tints it by the switch group colour. `LaserVignetteSync` pulses the screen red near a beam.
- `LaserBlinker` toggles the colliders and renderers of its children on a period, duty cycle and phase, using `Time.time` so the rhythm is deterministic.
- `LaserSwitch` holds a list of targets and a `groupColor`. It accepts the player and any `Enemy`. The scene reload restores everything, so there is no explicit reset.
- `GravityPad` (the jump pad), `StasisZone` and `TimeDefyingEnemy` (the Keepers) are from the original game. The Keepers use `1 / Time.timeScale` as a speed factor, which is why slowing time is dangerous near them.
- `Enemy` is the abstract base: a kinematic-free rigidbody that falls along `GravityField.Direction`, with `HandleContact` deciding what happens when it touches a laser, a bolt, the player or geometry. `PatrolDrone`, `Magnet` and `LaserTurret` override movement; `LaserBolt` is a trigger projectile spawned by turrets. `StasisZone` scales `speedMultiplier` on enemies too.
- `UnlockPickup` grants a `Progress.Ability` and removes itself. `GameSession.UnlockOnClear` grants the same abilities on clearing a stage, as a safety net.

## Session, scoring, UI

`GameSession` listens to `SceneManager.sceneLoaded`. A load is a *reload* when the scene name equals the previous one and the stage was not cleared, in which case the stage timer and death counter are kept. A *new run* starts only when the tutorial is entered from a non-stage scene (the title). `RunTime` is the sum of cleared stage times plus the current stage's elapsed unscaled time, minus paused time.

All UI is created in code through `UiKit` (`CreateCanvas`, `Text`, `Panel`, `Rect`). Every canvas uses `CanvasScaler.ScaleWithScreenSize` at 1920x1080 with match 0.5. `UiKit.HudScale` is 1.3 when touch controls are active, which enlarges the HUD and subtitles. `TitleScreen` and `EndingScreen` build their screens the same way, which is what makes the Japanese/English switch a simple scene reload.

`Loc.T(ja, en)` is the whole localisation system: a static class that picks the OS language, honours a saved preference, and accepts `-lang ja|en` on the command line.

## Persistence

Everything is `PlayerPrefs`:

| Key | Meaning |
|---|---|
| `best_score`, `best_time`, `runs_completed` | Run records |
| `best_stage_time_<scene>` | Per-stage best |
| `unlock_dash`, `unlock_quickgravity`, `unlock_echo` | Abilities |
| `lang` | `ja` or `en` |

On Windows this lives in the registry under the company and product name; on Android in the app's shared preferences; in the browser in IndexedDB.

## Audio

`MusicPlayer` cross-fades between the title and stage loops. `SfxPlayer.Play(name, volume, pitch)` loads clips from `Resources/Audio`. Every clip was synthesised by `Tools/make_audio.py` with NumPy, so the repository has no licensing questions around sound.

## Editor tooling (`Assets/Editor`)

- `LevelGenerator`: builds Title, Ending and the original stages 2 to 5 from coordinate tables. It is kept for reference and must not be run against hand-edited scenes; it regenerates them.
- `RemixLevelGenerator`: builds R1 to R5 (rooms, coloured fences, switches, guides, enemies, pickups), points the Stage 5 exit at R1, repairs the post-processing profile and registers the scenes in Build Settings. Helpers are small and composable (`Room`, `ColoredFenceX`, `Button`, `Guide`, `Enemy`, `Pickup`).
- `EnemyPrefabBuilder` and `PickupPrefabBuilder`: create the enemy and pickup prefabs and materials in code so they can be rebuilt consistently.
- `PostFxSetup`: creates or repairs `Assets/Settings/GravityPostFx.asset` (Bloom plus ACES tonemapping) and attaches a `PostProcessLayer` and global volume to the camera. Note that `PostProcessProfile.AddSettings<T>()` does not create a sub-asset; the effect must be added with `AssetDatabase.AddObjectToAsset` or it is null after a reload and Bloom silently does nothing.
- `LaserStyler`: applies the beam material and visual setup to the laser prefab.
- `BuildScript.BuildWindows` and `AndroidBuildSettings.BuildDevelopmentApk`: command-line builds. The Android script also applies the Player Settings described in the README.

## Verification: `AutoPilot`

`-autopilot <file>` loads a script of `[SceneName]` sections. When a scene loads, its section runs: `lookat x y z`, `walk seconds [x y]`, `flip`, `dash`, `quick x y`, `space`, `wait`, `log`, `startgame`, `continue`, plus touch commands `touchui`, `manual`, `tdown`, `tmove`, `tup`, `tbutton`, `tlog`. `-capture <dir>` saves a screenshot of every scene; `-startScene` jumps to a stage. Reaching the title after the ending logs `CHAIN COMPLETE`. Turret bolts and drones are deterministic, so scripts use `wait` to cross their paths at safe moments.

## Things that bit us

- `SecurityLaser.prefab` is saved with a 90 degree roll on its root. Instances without a rotation override are therefore horizontal, and the Euler hint stored in the scene does not always match the real rotation. Read `m_LocalRotation` when in doubt.
- Opening the project in a newer Unity 6 release upgrades `ProjectVersion.txt` and may bump package versions in `packages-lock.json`. Both editor streams open the project; keep the version file at the one your team uses.
- Screen captures taken while the Windows session is locked come back as a single flat colour. Unlock the screen before capturing.
