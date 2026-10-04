# Hollow Knight 8-Player Co-op Features

This page gives a little more detail about the main multiplayer systems without listing development history or old bug fixes.

## Shared game world

All players exist inside the same Hollow Knight game instance.

They share:

- Rooms and transitions.
- Enemies and bosses.
- NPCs and world objects.
- Story and save progression.
- Important events and encounters.

P2–P8 are not separate online game clients. They are added directly to the same running game as P1.

## Players

Each player can have their own:

- Movement and combat input.
- Health.
- Soul.
- HUD.
- Color and optional player label.
- Charm setup.
- Skin when Custom Knight is available.

Players can be added or removed during a session.

## Input

The mod supports normal controllers and additional keyboard players.

Up to four keyboard profiles can be active at once, with configurable bindings for each profile. P1 keeps Hollow Knight's normal controls.

vJoy / DirectInput support is also available for virtual-controller testing.

## Camera and group movement

The cooperative camera tries to keep the active group visible and changes zoom depending on how far players are separated.

The mod also contains systems for:

- Waiting for the group during room changes.
- Recovering players who become separated.
- Gathering players for important arenas or encounters.
- Manual dream-style rescue with a visible teleport trail.

## Death and recovery

Players can be knocked down and recovered individually instead of forcing the whole group to restart immediately.

The mod includes multiplayer systems for:

- Respawning.
- Reviving.
- Safe recovery positions.
- Optional Shade behaviour.
- Rescue between players.

## Difficulty

Enemy damage taken from the group can be scaled according to the number of living players.

Current presets are approximately:

- Easy: 45%
- Normal: 65%
- Hard: 85%
- Extreme: 100%

Players who are currently down do not count as active players for this scaling.

## PvP

The mod includes three main play styles:

- Co-op.
- Friendly Fire.
- Round Duels.

PvP can separately control different damage sources such as nail attacks, spells, Nail Arts and some charm or companion effects.

Round Duels include options for teams, round rules and score tracking.

## World interactions

Many systems that normally expect only the original Knight are being adapted so additional players can participate.

This includes work around benches, doors, transitions, NPC interactions, boss arenas, important events and other progression-related systems.

## Lighting and visibility

The mod includes cooperative lighting for dark areas.

Additional player lights can be enabled or disabled, and a Full Visibility option can remove darkness from supported rooms when preferred.

## Configuration

Most multiplayer behaviour can be changed from the F8 menu, including:

- Players and input.
- Camera behaviour.
- Difficulty.
- PvP.
- Rescue and recovery.
- Lighting.
- Performance options.
- Player identification.

The interface is being translated for Spanish, English, French, German, Italian, Portuguese, Russian, Chinese, Japanese and Korean.
