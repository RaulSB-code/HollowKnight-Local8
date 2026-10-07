# Hollow Knight 8-Player Co-op

**Public version: v0.10.0 · DLL version: 0.10.0 · ModLinks version: 0.10.0.0**

Local cooperative multiplayer for **2–8 players** inside one Hollow Knight game. Players share the world, enemies, bosses and story progression while keeping their own controls, health, SOUL, charms, optional roles and supported Custom Knight skins.

Target: **Hollow Knight 1.5.78.11833** with its matching **Hollow Knight Modding API**. This is local multiplayer, not a separate network-client system.

## What is new

- Nine saved role choices, including no role, with independent selection and role changes from the charm menu.
- Configurable PvP trips to boss-free vanilla Godhome arenas, restoring adventure resources and positions when the match ends.
- More reliable input ownership, numeric-keypad controls, spell direction, controller vibration and menu recovery.
- Improvements to swimming, rescue, room transitions, safe spawning, Godhome framing and boss music.
- Cooperative crystal-dash bonuses, native bench/challenge motions and shared ending/dark-ability effects.

See the [v0.10.0 release notes](docs/RELEASE_NOTES_v0.10.0.md), [changelog](CHANGELOG.md) and [features](docs/FEATURES.md).

## Installation

Install the matching Modding API, then extract **HollowKnightLocal8-v0.10.0.zip** into a `HollowKnightLocal8` folder under the game's `Mods` directory. Replace the existing DLL; do not keep multiple versions installed.

The release ZIP contains only **HollowKnightLocal8.dll**. All mod-owned artwork and translations are embedded in that DLL. [Download v0.10.0](https://github.com/RaulSB-code/HollowKnight-Local8/releases/download/v0.10.0/HollowKnightLocal8-v0.10.0.zip).

Search for **Hollow Knight 8-Player Co-op** in Lumafly once its ModLinks entry has been accepted and updated. Manual installation remains available. See [installation](docs/INSTALLATION.md).

## Playing

Load a save, press Start on another controller to join, or use **F8** to configure extra players and keyboard profiles. The panel key is configurable. The first role selection is saved per player slot; later role changes use the charm menu near a bench.

**RB** is assigned to the mod's dream rescue instead of quick cast during co-op. The default keyboard rescue key is **1**, and bindings can be changed. Hold the rescue input to move as dream essence and rejoin another living player. Cooldown and safe-landing checks prevent repeated unsafe rescues.

Up to four keyboard profiles are supported. Some physical keyboards cannot report all simultaneous key combinations; the mod includes a setup warning and rebinding options. Normal controllers and supported vJoy/DirectInput devices are also available.

## Compatibility

**Custom Knight** is recommended and has dedicated integration for individual player skins. **Enemy HP Bar** is recommended for shared fights. Neither is a required dependency. **Pale Court** support remains experimental; large content mods and unusual scripted scenes still need testing. See [compatibility](docs/COMPATIBILITY.md).

## Source and development

This package contains the complete source used to compile the v0.10.0 DLL, including the latest Work development changes through alpha186. It builds directly from C# and embedded assets; no previous mod DLL is used as a build input. Game libraries are not redistributed.

[Build instructions](docs/BUILDING.md) · [source notes](docs/SOURCE_NOTES.md) · [publishing instructions](docs/PUBLISHING.md)

The mod is under active development. Automated checks and deterministic rebuild verification supplement gameplay testing; they do not cover every native scene, controller or mod combination. [Report bugs](https://github.com/RaulSB-code/HollowKnight-Local8/issues) with the version, game version, player count, other mods, scene, reproduction steps and relevant log.

Developed by **RaulSB-code**. Hollow Knight belongs to Team Cherry. Hollow Knight 8-Player Co-op is an unofficial community mod and is not affiliated with Team Cherry.
