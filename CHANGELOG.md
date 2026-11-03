# Changelog

Dates are approximate; the project did not use tagged releases before 1.0.

## 1.0 (November 2026)

- Project renamed to Gravitiria; title screen and product name updated.
- Remix 5 redesigned as a much longer room: a turret wall at the entrance, laser-lined corridors, jump pads, three magnets and a grid of gates under the floor.
- Stage 5 gained a group of lasers that sweep across the corridor, plus a laser on the right wall.
- The hidden second tutorial room was removed from the chain.
- English documentation added: README, PRD, architecture notes, level guide, publishing guide, pitch.

## 0.4 (October 2026): Android

- Touch controls: floating stick, swipe look, FLIP (hold, drag to aim, release), DASH, quick-gravity pad, pause menu, language button. Shown only on mobile, or with `-touchui` / F10 for testing.
- UI kept inside the device safe area; HUD and subtitles 30 % larger on touch.
- Android Player Settings script and development APK build (min API 26, target automatic, IL2CPP, ARM64, landscape).
- Touch test script for the autopilot.

## 0.3 (October 2026): Remix

- Input layer: `GameInput` with keyboard/mouse, gamepad and a pluggable source list.
- Enemies: patrol drone, magnet, laser turret with bolts; all follow the player's gravity and can press switches.
- Unlockable moves: Dash with invincibility frames and cooldown; quick four-way gravity.
- Five Remix stages, colour-coded, without hint text, chained after Stage 5.
- Post-processing profile repaired so Bloom actually renders.
- Product name split from the original game so saves do not collide.

## 0.2 (October 2026): Complete game

- Stages 2 to 5, title and ending screens, best-score and best-time records.
- Run timer and death counter that persist across respawns.
- Radio messages dismissed with Space; the first one explains how.
- Procedural music and sound effects.
- Japanese and English localisation following the OS language.
- Laser visuals rebuilt: thin white-hot core, coloured glow and halo, fresnel fade, flowing packets, emitters at both ends.
- Stage 4 ceiling fully lasered with a switch; Stage 1 entrance moved behind the start.
- Automated play-through harness for verification.

## 0.1: Original prototype by Hiroaki

- First-person gravity flip with slow-motion aiming.
- Security lasers, jump pad, stasis zone, time-defying sentinels, subtitles, scene fader.
- Tutorial and the first level.
