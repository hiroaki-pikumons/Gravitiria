# Gravitiria: Level Guide

Design notes and intended solutions for every stage. **Spoilers throughout.** Coordinates refer to the scene's world space (x right, y up, z forward from the start).

## How the stages are built

- The tutorial and Stage 1 are hand-built scenes by Hiroaki. Stages 2 to 5, the title and the ending are assembled from coordinate tables in `Assets/Editor/LevelGenerator.cs`, with Stage 1 as the template. The Remix stages come from `Assets/Editor/RemixLevelGenerator.cs`.
- The rule for every room: if the player can see the goal, the route to it must require at least one flip. If the player cannot see it, something in the room (a glowing guide strip, a coloured switch, a gap in a fence) must point at the first flip.
- Hint text exists only in the tutorial and the early original stages. The Remix stages have none.
- Every stage up to Stage 4 is cleared by the autopilot script with zero deaths on every build. Stage 5 and Remix 5 were rebuilt in version 1.0 and their scripted routes have not been re-timed yet; see the notes on those stages.

## Tutorial · Basics

A straight corridor. A radio message explains the first flip. Walk forward, close the message, keep walking to the goal. The second half of the corridor shows a laser without putting it in the way.

## Stage 1 · To the Underside

The exit is directly beneath the start, on the underside of the floor. Walk backwards off the room's rear edge, aim at the underside while falling, flip, and walk back toward the start. Lasers on the underside are arranged as fences with alternating gaps: right, then left, then right. The lesson is that "behind you" and "below you" are both places.

## Stage 2 · Walk the Walls

A long hall with vertical laser columns on the walls. Walk the floor until a column blocks the way, flip to the wall and climb, step over a column, drop back. Later the walls are sealed and the ceiling is the only road; near the far end the ceiling is sealed too and the route returns to a wall before the final drop to the goal platform.

## Stage 3 · Keepers of Time

Three ideas in a row. First a corridor of Keepers, boxes that move faster when time is slowed; cross it without aiming. Then a jump pad that throws you over a laser line. Then a stasis field that slows everything inside it, including a Keeper, which is the moment to pass.

## Stage 4 · The Tower

A vertical shaft. Bands of lasers wrap each wall at different heights, so the player climbs by switching walls: flip to the north wall, walk up to the first band, flip to the east wall to step around it, and so on. The ceiling is fully covered with lasers. A switch on the north wall near the top disables them, which is the only way to flip onto the ceiling and reach the goal. Dying resets the switch.

## Stage 5 · Final Exam

Floor, wall and ceiling in one route: walk the floor, flip to the right wall to pass a block, flip to the ceiling, follow it to the goal platform.

Version 1.0 added a group of low lasers that sweep sideways across the corridor around 24 to 26 m in, plus a laser on the right wall at the same depth. They have to be timed: wait for the sweep to pass, then cross, or flip to the left wall before them. The autopilot route in `Tools/autopilot_full_run.txt` still walks straight through this section and dies there; it needs re-timing against the sweep.

## Remix 1 · Flip & Key

Rules introduced: a switch and the lasers it controls share a colour; glowing strips on the floor mark a path. The room has four red fences on the floor with alternating gaps (right, left, right, left) and a blue laser field across the ceiling. A blue switch at the far end turns the ceiling off. The goal is on the ceiling above the middle of the room.

Route: pick up the Dash crystal by the entrance, weave through the red fences, step on the blue switch, aim at the ceiling, flip, walk back along the ceiling to the goal.

## Remix 2 · Back Door

The goal is on the underside of the floor, behind the start. Nothing in front of the player leads anywhere. Guide strips run toward the rear edge.

Route: walk back off the rear edge, flip to the underside while falling, pass two red fences (gap on the right, then on the left), take the pink cross (Quick Gravity), reach the goal.

## Remix 3 · Lure

A patrol drone runs across the room. A yellow wall shortens its patrol; a yellow switch at the far end removes the wall. Once the wall is gone the drone's patrol reaches a low tunnel with a green switch inside it, which the player cannot enter. The drone presses it, and the green gate in front of the goal opens.

Route: through the red fences, step on the yellow switch, wait for the drone to cross the tunnel, walk through the open green gate.

## Remix 4 · Blink

Three full-width laser fences blink on a 2.4 second period, lit 55 % of the time, each a third of a cycle out of phase. The player can time each one, or use the Dash's invincibility to cross while lit. Nothing else is in the room, on purpose: the stage is about reading a rhythm.

## Remix 5 · Final Remix

The last room was redesigned in version 1.0 and is far longer than the others. The player starts about 65 m behind the original room on a new floor section. Known elements, from the start forward:

- A line of laser turrets across the mouth of the main room, firing back toward the start. Two laser corridors run along the sides of the approach at ankle height, with jump pads placed between them.
- A field of hanging lasers at the room entrance, a patrol drone, and three magnets in the middle of the room.
- Turrets on the right wall at several heights, and more turrets under the floor near the far end.
- The blue switch sits high on the right wall near the far end. It opens a grid of blue gates under the floor.
- The goal is under the floor, two thirds of the way along the room.

Intended idea: use the jump pads and the side corridors to get past the turret line, flip to the right wall to reach the blue switch while the magnets fall away from you, then return to the rear edge, flip to the underside and walk to the goal through the opened gates.

This stage is the one place where the autopilot route in `Tools/autopilot_full_run.txt` may lag behind the scene. If the script reports `SECTION END WITHOUT SCENE CHANGE: R5`, the route needs updating to match the current layout; the rest of the chain is unaffected.

## Ending

The tally screen. Per-stage time, deaths and score, the run total, and whether it is a new best. Enter or a tap returns to the title.
