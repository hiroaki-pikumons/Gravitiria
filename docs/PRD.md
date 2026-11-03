# Gravitiria: Product Requirements

This document was written after the game was built. It records what the game is required to do, so that anyone changing it knows which behaviours are deliberate. Where a requirement came from a problem found in play testing, the problem is noted.

## 1. Summary

Gravitiria is a short first-person puzzle game built in Unity for Windows, Android and the browser. The player changes the direction of gravity by looking at a surface and releasing the aim button. Every stage is a room (or a chain of rooms) that can only be crossed by treating walls and ceilings as floors. The game is meant to be finished in one sitting, replayed for time, and shared as a small portfolio piece.

## 2. Goals

1. One mechanic, explored thoroughly. Every stage must be solvable with the gravity flip plus whatever the player has already been taught.
2. No text where light will do. The five Remix stages must be solvable without reading anything.
3. Honest timing. A run's time and death count must survive respawns, so that a clear means something.
4. Runs anywhere the player is. The same game, with the same rules, on a desktop, a phone and in a browser tab.
5. Bilingual. Japanese and English, chosen automatically and switchable.

## 3. Non-goals

- Procedural or user-generated levels.
- Online leaderboards, accounts, or analytics.
- Narrative beyond the short radio messages in the tutorial and early stages.
- Mobile-specific monetisation or store features.

## 4. Players and platforms

- Primary: PC players with a mouse, 15 to 40 minutes for a first clear.
- Secondary: Android phone players (landscape), and browser players reached through a share link.
- Input: keyboard and mouse, XInput-style gamepad, multi-touch.
- Minimum Android: Android 8.0 (API 26), 64-bit ARM.

## 5. Core loop

1. Enter a room. The stage banner shows its name for two seconds.
2. Look for the exit: a white, slowly rotating box.
3. Move, aim at a surface, flip. Repeat until the geometry lines up.
4. Touch the goal. The stage time is recorded and the next room loads.
5. Die, and the room reloads with the timer and death counter intact.

## 6. Functional requirements

### 6.1 Player and gravity

| ID | Requirement |
|---|---|
| P1 | Holding the aim input slows time to 10 %, narrows the field of view and darkens the edges of the screen. |
| P2 | Releasing the aim input casts a ray from the centre of the screen. If it hits non-trigger geometry within 100 m, gravity becomes the negative of the hit surface normal. Trigger volumes are ignored. |
| P3 | The body re-orients so that "down" matches gravity, smoothly, without the camera snapping. |
| P4 | Movement is projected onto the plane perpendicular to gravity, so moving during a flip never launches the player. (Found as a bug: moving while flipping accumulated vertical velocity.) |
| P5 | Shift (or the gamepad equivalent) is a faster walk and narrows the field of view slightly. |
| P6 | Falling more than 150 m from the origin counts as a death and reloads the stage. |
| P7 | After the Dash unlock, a dash is a short burst (0.18 s at 22 m/s) with 0.35 s of invincibility and a 1.2 s cooldown. |
| P8 | After the Quick Gravity unlock, four inputs snap gravity to the camera's left, right, up or down without aiming. |

### 6.2 Hazards and devices

| ID | Requirement |
|---|---|
| H1 | Security lasers kill the player on contact unless the player is invincible. A near-miss pulses the screen red. |
| H2 | Blinking lasers follow a fixed period and duty cycle; while off they have no collider and no visual. |
| H3 | Switches disable the lasers they reference. A switch and its lasers share a colour. Reloading the stage restores them. |
| H4 | Switches respond to the player and to enemies. |
| H5 | Jump pads add an impulse away from their surface. |
| H6 | Stasis zones slow the player and enemies inside them. |
| H7 | The Keepers (Stage 3) move faster when time is slowed. |
| H8 | The goal advances to the scene named on it. The last stage leads to the ending; the tutorial and stages 1 to 5 chain into the Remix stages. |

### 6.3 Enemies

| ID | Requirement |
|---|---|
| E1 | All enemies fall in the direction of the player's current gravity. |
| E2 | Enemies die when they touch a laser or a turret bolt. |
| E3 | Touching an enemy kills the player unless invincible. |
| E4 | Patrol drones move back and forth along an axis and reverse at a set distance or at a wall. |
| E5 | Magnets move toward the player when within range and stop at a short distance. |
| E6 | Turrets are static and fire a bolt along their forward axis at a fixed interval; optionally they aim at the player. Bolts kill the player and non-turret enemies. |
| E7 | Reloading a stage restores all enemies to their starting state. |

### 6.4 Session, scoring and records

| ID | Requirement |
|---|---|
| S1 | A run starts when the tutorial is entered from outside a stage. |
| S2 | The run timer uses unscaled time, excludes paused time, and does not reset on death or restart of the same stage. |
| S3 | The death counter increments on every death and persists across reloads within the run. |
| S4 | Each stage's time and deaths are kept; a stage's score is `max(500, 10000 - round(seconds) * 20 - deaths * 750)`. |
| S5 | The ending screen shows per-stage results, totals, and whether the run is a new best. |
| S6 | Best total score, best total time, per-stage best times, clear count, unlocked moves and language are persisted with PlayerPrefs. |
| S7 | Holding Delete on the title screen for 1.5 s resets all records and unlocks. |
| S8 | Clearing Stage 5 grants Dash and clearing Remix 1 grants Quick Gravity, as a safety net if the pickup was missed. |

### 6.5 Interface

| ID | Requirement |
|---|---|
| U1 | HUD: stage name, run time, death count, unlocked moves. Hidden outside stages. |
| U2 | Radio messages appear as typewriter subtitles and stay until dismissed. The first message in a session is prefixed with how to dismiss it. |
| U3 | Pause stops time, frees the cursor, and offers resume, restart and return to title. |
| U4 | All UI scales with screen size (1920x1080 reference, match 0.5) and stays inside the device safe area. |
| U5 | Text is at least 30 % larger when touch controls are active. |
| U6 | The language follows the OS (Japanese or English) and can be toggled on the title screen; the choice is saved. |

### 6.6 Touch controls (Android)

| ID | Requirement |
|---|---|
| T1 | Shown only on mobile platforms, or when forced for testing. |
| T2 | Left half: a floating virtual stick that appears under the finger. Right half: swipe to look. |
| T3 | A FLIP button that behaves like the aim input: hold to slow and aim, drag to adjust, release to flip. |
| T4 | DASH and quick-gravity buttons appear only after the matching unlock. |
| T5 | A pause button, and an on-screen pause menu. |
| T6 | A short tap on the right half dismisses messages and confirms menus. |
| T7 | 60 fps requested; screen sleep disabled while playing. |

### 6.7 Audio

| ID | Requirement |
|---|---|
| A1 | Looping title and stage music, cross-faded on scene change. |
| A2 | Sound effects for flip, dash, death, goal, selection and the radio static under subtitles. |
| A3 | All audio is generated from a script in the repository; no external samples. |

## 7. Quality requirements

- Every stage is completed by the automated play-through script with zero deaths before a build is published. As of 1.0 this holds for the title through Stage 4; the Stage 5 and Remix 5 routes are pending an update after those rooms were rebuilt.
- A build with a compile error is never committed.
- Frame rate target: 60 fps on a mid-range phone from 2022 with Bloom enabled; Bloom may be reduced in the post-processing profile if needed.
- Startup to title screen under five seconds on desktop.

## 8. Constraints and decisions

- Built-in Render Pipeline and Post Processing Stack v2, because the original project used them. PPv2 is embedded in `Packages/` with a one-line fix so it compiles on newer Unity 6 releases.
- Legacy Input Manager, wrapped by a single `GameInput` facade so that input sources can be added without touching gameplay code.
- UI is built in code (`UiKit`) rather than in prefabs, so that layouts and text sizes can be changed per platform without editing scenes.
- The original scenes authored by Hiroaki are never regenerated by the editor tools. The Remix generator only creates its own scenes and patches the Stage 5 exit.

## 9. Open items

- Echo (a recorded double that holds a second switch) was designed but not built.
- Device testing on a physical Android phone is pending; the APK builds and the touch controls were verified with simulated input on desktop.
- A speedrun-friendly timer display (milliseconds in the ending table) is a possible follow-up.
