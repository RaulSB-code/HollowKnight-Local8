# Hollow Knight 8-Player Co-op features

## Shared world and independent players

Two to eight local players share scenes, enemies, bosses, NPCs, progression and a save. Each player has their own input, health, SOUL, charm loadout, HUD, colour and supported skin. Controllers, additional keyboard profiles and supported vJoy/DirectInput devices can join a session. Keyboard bindings persist, and keypad input has dedicated Windows handling with fallbacks.

## Roles

Nine choices are available: **no role, Mage, Warrior, Healer, Shaman, Explorer, Guardian, Berserker and Summoner**. Several players may choose the same role. The initial choice is saved per slot; reconnecting or changing rooms does not ask again. Roles can be changed through the charm menu near a bench.

| Role | Main effects | Tradeoff |
| --- | --- | --- |
| No role | Ordinary co-op behavior | None |
| Mage | +10% spell damage; nearby successful enemy damage grants 1/32 of a standard SOUL vessel, capped at two drops per second | -10% nail damage |
| Warrior | +10% nail damage; +15% post-hit invulnerability | -10% spell damage |
| Healer | A completed focus heals nearby living allies by one mask inside a visible dome | +15% focus time |
| Shaman | Replaces guaranteed overcharm double damage with a 10% double-damage chance per received hit | The 10% risk also applies without overcharming |
| Explorer | +10% nail attack speed; -15% dash cooldown | -10% nail damage |
| Guardian | +1 maximum mask; +15% post-hit invulnerability | -10% movement speed; +10% dash cooldown |
| Berserker | At half red health or below: +20% nail damage and +10% attack speed | +35% focus time |
| Summoner | +30% companion and Dreamshield damage | -10% nail and spell damage |

Role damage bonuses affect enemy damage and retain co-op scaling. PvP mask damage uses its own configured rules. Healing does not resurrect, chain into another heal or heal opposing duel teams. Familiar ownership follows the existing per-player summon system.

## Charms and interactions

Players use their own selectors and equipment panels. Selecting an equipped charm again removes it. The custom panel includes descriptions, notch usage, role selection and bench-range feedback. Overcharming is enabled without requiring a previous single-player unlock; its usual penalties apply unless modified by a role.

Interaction ownership is routed to the actor using a door, bench, NPC, shop or Stag interface. Native scripted sequences and acquisition cards have recovery handling. Benches support up to three closely spaced seats, native boarding/dismounting, and repeated saves without requiring a room change.

## Death, recovery and movement

Individual deaths do not remove the party's shared geo or break its SOUL vessel. Whole-party defeat uses the native recoverable Shade and shared broken-vessel state; the HUD displays the missing upper third until native Shade recovery. Optional personal Shades remain configurable.

Revival, safe hazard recovery and automatic respawn are available. The respawn slider spans 10–60 seconds and defaults to 15 seconds for new settings. Dream rescue has a cooldown, movement near screen edges is constrained, and arrival uses a short essence trail. Contextual prompts appear when appropriate.

Room waiting/voting can gather the party before ordinary exits. Scripted travel and Pantheon progression have separate handling. Swimming isolates extra players from P1's native water state, supports Isma-enabled acid surfaces, and controls repeated splash audio. SOUL baths refill all valid occupants, including reserve capacity. The final Radiance climb shares the highest verified safe platform only during its ascent phase.

## Camera, lighting and encounters

The adaptive camera smooths movement, uses a visible player anchor when the party spreads out, and gives physical players priority over dream-rescue clouds. Godhome fights use bounded, fixed combat framing with adjustments for compact, wide and vertical arenas. Extra lights and full-visibility options are configurable.

Encounter entry can gather players inside closing arenas. Boss-hit gathering has been disabled to avoid repeated mid-fight teleports. Boss and arena music guards prevent local death effects from silencing an ongoing fight while retaining genuine phase/scene changes.

## PvP

Co-op, friendly fire and round duels remain available. Duels support teams, best-of matches, nail parries, spells and configurable damage sources. Optional arena trips use actual vanilla Godhome stages, including flat arenas, spikes, void, platform layouts and **Markoth with a floor**. Boss spawning and rewards are suppressed only during an owned arena trip.

Arena configuration includes health, blue masks, starting/capacity SOUL, infinite SOUL, jumps/dashes, movement abilities, spell levels, healing and hazard damage. Adventure state and positions are restored after returning. Timed rounds award the highest remaining health, or team health, with ties drawn. Round time defaults to unlimited; finite rounds show an upper-right timer.

Charms are enabled by default, with an option to disable them. Lifeblood/Joni health and supported familiar combat are handled for duels. Companion opponent targeting and destructible Hatchlings apply only to PvP. Geo is hidden during duel rounds and results; countdown and winner text use larger native-font labels.

## Presentation and configuration

Nearby crystal-dash launches receive cooperative damage factors of 3.5×, 4.75× and 5.5× for groups of two, three and four or more, with restrained per-player crystal effects. Room changes retain valid individual flight state. Trails clear correctly on death or interruption.

Players use native challenge draw/sheath motions with individual draw sounds. Additional players participate in cooperative ending absorption and dark-ability rituals, with controlled dream/dark essence and cleanup across scene fades. The intact Delicate Flower has a subtle owner-specific soul aura.

The configuration interface and role descriptions provide **Spanish, English, French, German, Italian, Portuguese, Russian, Chinese, Japanese and Korean** text, with English fallback. The panel key, player setup, camera, difficulty, PvP, rescue, recovery, lighting and performance options are configurable.
