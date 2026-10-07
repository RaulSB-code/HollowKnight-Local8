# Hollow Knight 8-Player Co-op v0.10.0

An update built from the latest Work development state through alpha186. Target: Hollow Knight **1.5.78.11833** and the matching Modding API.

## Changes since v0.9.0

- Added nine persistent role choices, including no role, independent role selection and bench/charm-menu changes. Refined healer ally healing, mage SOUL sharing, Shaman damage risk and per-player summon bonuses.
- Added configurable trips to boss-free vanilla Godhome PvP arenas, including Markoth with a floor. Custom health/SOUL, jumps, dashes, abilities and match rules restore on return. Charms are enabled by default; Lifeblood and familiar PvP behavior have dedicated handling.
- Improved keyboard/controller isolation, physical numeric-keypad detection, saved extra-keyboard bindings, owned vibration, held jumps and stale menu-input recovery. Added a localized shared-keyboard limitation warning.
- Corrected reversed door destinations and strengthened room-entry/return recovery, safe landings, repeated bench saves and pickup/challenge completion. Added native bench boarding/dismounting and individual challenge draw sounds.
- Improved extra-player water/acid behavior, Isma surface handling, swim presentation and splash suppression. SOUL baths refill all valid occupants without requiring reentry; pooled SOUL spells retain their caster/facing and have a bounded lifetime.
- Refined smooth camera framing, visible-player anchoring, physical-player priority during dream rescue, contextual rescue prompts, bounded essence movement and smooth arrival. Tuned fixed Godhome views for compact and wide arenas.
- Protected ongoing boss/PvP music from local death effects, improved arena lighting/fade recovery, and prevented repeated boss-hit gathering. Added safer arena placements and return handling.
- Individual deaths preserve shared geo/SOUL capacity; whole-party defeat retains native Shade recovery and a visibly broken vessel. Refined recovery poses, full-screen fades and the final Radiance ascent checkpoint.
- Added stronger linked crystal-dash damage with restrained effects and room-to-room flight continuity; restored cooperative ending absorption and extra-player dark-ability rituals with dream essence and native acquisition routing.
- Expanded ten-language UI coverage, charm/role details, PvP countdown/results and finite-round timers. Unlimited round time is the default; timed rounds resolve by remaining health. Added a subtle Delicate Flower aura.
- Unified public branding and all DLL version metadata. The complete source now compiles directly without an older mod binary and reproduces the distributed DLL.

## Installation

Replace the previous installation with **HollowKnightLocal8.dll** from **HollowKnightLocal8-v0.10.0.zip**. The release ZIP contains only that DLL; artwork and translations are embedded. Do not install multiple versions simultaneously.

Public version: **v0.10.0**. Modding API/GetVersion: **0.10.0**. Assembly/file/ModLinks version: **0.10.0.0**.

Custom Knight remains optional and recommended. Pale Court support is experimental. Compilation, assembly checks, regression simulations and deterministic rebuild verification do not replace native gameplay testing of every encounter or mod combination.
