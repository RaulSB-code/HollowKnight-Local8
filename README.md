# Hollow Knight 8-Player Co-op

**True local co-op for Hollow Knight — play with up to 8 players in the same game.**

Hollow Knight 8-Player Co-op is a local multiplayer mod for **2–8 players on the same PC**.

The goal is to make Hollow Knight feel like a real cooperative game rather than simply adding extra Knights on screen. Every player exists inside the same running game: they explore the same rooms, fight the same enemies and bosses, interact with the same world and share the same progression.

P1 remains Hollow Knight's original player, while P2–P8 are added to that same world with their own controls, movement, combat, health, Soul, HUD, charms and appearance.

> **Current public version: v0.9.0**  
> Target game version: **Hollow Knight 1.5.78.11833**  
> v0.9.0 is based on internal build 139.

## What makes Hollow Knight 8-Player Co-op different

Hollow Knight 8-Player Co-op uses a single Hollow Knight game instance for the whole group.

An enemy being attacked by P1 is the same enemy being attacked by P2–P8. The same applies to bosses, rooms, NPCs, objects, events and progression.

The core of the project is adapting Hollow Knight's original single-player systems so several players can take part in the same adventure together.

## Main features

- 2–8 local players in one Hollow Knight game.
- Shared enemies, bosses, world and progression.
- Independent movement, combat, health and Soul.
- Individual HUD, player colors and optional P1–P8 labels.
- Controller support.
- Up to four simultaneous keyboard players with separate key profiles.
- Keyboard control editor for additional players.
- vJoy / DirectInput support for virtual controllers and testing.
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

The mod is mainly designed for cooperative play, but it also includes optional PvP modes.

**Co-op** keeps players on the same team without damaging each other.

**Friendly Fire** keeps the normal adventure running while allowing players to damage each other.

**Round Duels** turns the mod into a local PvP mode with rounds, teams and a scoreboard.

PvP settings include separate control over nail damage, spells, Nail Arts and some charm or companion damage. Player parries and other PvP interactions are also supported.

## Controls

P1 keeps the normal Hollow Knight controls.

Additional players can use controllers or keyboard profiles. Several keyboard players can play at the same time, each with their own configurable bindings.

The F8 menu manages players, controls, camera behaviour, difficulty, PvP, recovery systems, lighting, performance options and other multiplayer settings.

## Recommended mods

The mod works on its own, but two mods are especially useful alongside it.

### Custom Knight

**Custom Knight is highly recommended.**

The mod can detect Custom Knight and use installed skins for individual players. Different skins also make players much easier to identify when several Knights are on screen.

### Enemy HP Bar

**Enemy HP Bar is also recommended**, especially for larger groups.

Because everyone fights the same enemies and bosses, visible health bars make group fights easier to follow.

## Compatibility with other mods

Compatibility with the wider Hollow Knight modding ecosystem is one of the goals of Hollow Knight 8-Player Co-op.

Custom Knight already has specific support. Other mods may work without changes, while larger content mods can require extra work because they add new rooms, bosses, interactions or scripted events.

**Pale Court is currently experimental.** Parts of it already work with the mod, but multiplayer-specific problems are still expected.

See [Compatibility](docs/COMPATIBILITY.md) for more information.

## Installation

v0.9.0 is available now from [GitHub Releases](https://github.com/RaulSB-code/HollowKnight-Local8/releases/tag/v0.9.0).

The recommended installation method will be **Lumafly** once Hollow Knight 8-Player Co-op is accepted into ModLinks. See [Installation](docs/INSTALLATION.md).

## Development status

v0.9.0 is the first public version of Hollow Knight 8-Player Co-op.

The mod is playable, but it is still under active development. Larger 6–8 player sessions, performance, endings, unusual interactions and compatibility with large content mods will continue to receive testing and fixes.

## Bug reports

When reporting a problem, please include the mod version, Hollow Knight version, number of players, other enabled mods, the area where it happened and steps to reproduce it.

If possible, also attach the mod log and a screenshot or video.

## Source and releases

Public builds are distributed through GitHub Releases and, after acceptance into ModLinks, will also be available through Lumafly.

The v0.9.0 project files, recovered `src` tree and source documentation are included in this repository. See [Building from source](docs/BUILDING.md) and [v0.9.0 source notes](docs/SOURCE_NOTES_v0.9.0.md). Original Hollow Knight game files and Team Cherry assemblies are not included.

## Author

Developed by **RaulSB-code**.

Hollow Knight is property of Team Cherry. Hollow Knight 8-Player Co-op is an unofficial community mod and is not affiliated with Team Cherry.
