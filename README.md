# Hollow Knight Local8

**True local co-op for Hollow Knight — play with up to 8 players in the same game.**

Hollow Knight Local8 is a local multiplayer mod for **2–8 players on the same PC**.

The main goal is to make Hollow Knight feel like a real cooperative game rather than simply adding extra Knights on screen. Every player exists inside the same running game: they explore the same rooms, fight the same enemies and bosses, interact with the same world and share the same progression.

P1 remains Hollow Knight's original player, while P2–P8 are added to that same world with their own controls, movement, combat, health, Soul, HUD, charms and appearance.

> **Status:** Alpha / work in progress  
> Current development is focused on Hollow Knight **1.5.78.11833**.  
> Local8 is not yet published on Lumafly or ModLinks.

## What makes Local8 different

Local8 uses a single Hollow Knight game instance for the whole group.

That means an enemy being attacked by P1 is the same enemy being attacked by P2–P8. The same applies to bosses, rooms, NPCs, objects, events and progression.

This is the core idea behind the project: adapt Hollow Knight's original single-player systems so several players can take part in the same adventure together.

## Main features

- 2–8 local players in one Hollow Knight game.
- Shared enemies, bosses, world and progression.
- Independent movement and combat.
- Independent health and Soul.
- Individual HUD for P1–P8.
- Player colors and optional P1–P8 labels.
- Controller support.
- Up to four simultaneous keyboard players with separate key profiles.
- Keyboard control editor for additional players.
- vJoy / DirectInput support for testing and virtual controllers.
- Players can join and leave during a session.
- Adaptive cooperative camera and zoom.
- Group waiting and recovery systems for room transitions.
- Manual dream-style rescue and teleport effects.
- Individual death, respawn and revive systems.
- Optional Shade behaviour.
- Individual charm loadouts.
- Multiplayer-aware interactions with benches, doors, NPCs, transitions and other game systems.
- Boss arena and event handling for multiple players.
- Configurable difficulty scaling based on active players.
- Cooperative lighting and optional full-room visibility.
- Performance options for larger groups.
- Multi-language interface and automatic language detection.
- Extensive configuration through the F8 menu.

More detail is available in [Features](docs/FEATURES.md).

## Co-op and PvP

Local8 is mainly designed for cooperative play, but it also includes optional PvP modes.

**Co-op** keeps players on the same team without damaging each other.

**Friendly Fire** keeps the normal adventure running while allowing players to damage each other.

**Round Duels** turns Local8 into a local PvP mode with rounds, teams and a scoreboard.

PvP settings include separate control over nail damage, spells, Nail Arts and some charm or companion damage. Player parries and other PvP interactions are also supported.

## Controls

P1 keeps the normal Hollow Knight controls.

Additional players can use controllers or keyboard profiles. Local8 can support several keyboard players at the same time, each with their own configurable bindings.

The F8 menu is used to manage players, controls, camera behaviour, difficulty, PvP, recovery systems, lighting, performance options and other multiplayer settings.

## Recommended mods

Local8 works on its own, but two mods are especially useful alongside it.

### Custom Knight

**Custom Knight is highly recommended.**

Local8 can detect Custom Knight and use installed skins for individual players. Different skins make players much easier to identify when several Knights are on screen.

### Enemy HP Bar

**Enemy HP Bar is also recommended**, especially for larger groups.

Because everyone is fighting the same enemies and bosses, visible health bars make group fights easier to follow.

## Compatibility with other mods

Compatibility with the wider Hollow Knight modding ecosystem is one of the goals of Local8.

Custom Knight already has specific support. Other mods may work without changes, while larger content mods can require extra work because they add new rooms, bosses, interactions or scripted events.

**Pale Court is currently being tested.** Parts of it already work with Local8, but compatibility is still experimental and multiplayer-specific problems are expected.

See [Compatibility](docs/COMPATIBILITY.md) for the current status.

## Development status

Local8 is still in active development. The current project base is **alpha139**, but it should not yet be considered a stable public release.

Testing is still needed in areas such as 6–8 player sessions, performance, endings and some special interactions.

The project will be prepared for GitHub Releases and ModLinks/Lumafly once a sufficiently tested build is ready.

## Bug reports

When reporting a problem, please include the Local8 version, Hollow Knight version, number of players, other enabled mods, the area where it happened and steps to reproduce it.

If possible, also attach the Local8 log and a screenshot or video.

## Source and releases

The source code will be added and organised here before the public release.

Original Hollow Knight game files and Team Cherry assemblies will not be included in this repository.

Stable builds will eventually be distributed through GitHub Releases and prepared for installation through ModLinks/Lumafly.

## Author

Developed by **RaulSB-code**.

Hollow Knight is property of Team Cherry. Hollow Knight Local8 is an unofficial community mod and is not affiliated with Team Cherry.
