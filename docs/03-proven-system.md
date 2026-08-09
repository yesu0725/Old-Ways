# Proven — The Anti-Cheese Gate

Status: `DECIDED` in structure, numeric values `PROPOSED` (first pass, needs playtest)

## Problem

Vanilla skill level is just an int in the save file — `raiseskill` sets it instantly. Gating mastery
powers on skill level alone makes them free to console-command.

## Solution

A separate, **server-tracked counter per vanilla skill** called **Proven**, earned only through
qualifying combat events. Vanilla skill commands cannot touch it.

- Vanilla skill level remains a **soft prerequisite** — see Soft prerequisite below.
- **Proven is what actually unlocks the power** in [02](02-weapon-mastery.md).
- **Proven is visible** in the skills screen. See [06](06-ui-trial-log.md).
- **Proven is permanent** once earned. No decay. `DECIDED 2026-08-06` — decay would punish breaks from
  play and contradicts the "your body remembers" framing in [00](00-design-principles.md).
- **Proven is per vanilla skill**, not per weapon family. `DECIDED 2026-08-06` — see the consequence
  for shields below.

## Granularity: 12 tracks

Per-skill tracking maps cleanly onto the game's own `Skills.SkillType`, whose combat values are
exactly these eleven. **Unarmed is a twelfth track** added 2026-08-09 so fist weapons are not left
out.

Blocking is the one family with no attack, so it cannot use the standard R1/R3/R5 ladder and carries
its own three powers up the same rungs — see [02](02-weapon-mastery.md).

## Rank ladder

Proven Points (PP) accumulate per skill and never reset. Ranks:

| Rank | Name | Cumulative PP | Meaning |
|---|---|---|---|
| 0 | Untested | 0 | Vanilla behavior |
| 1 | **Blooded** | **150** | **Power unlocks** |
| 2 | Tempered | 400 | — |
| 3 | Hardened | 800 | — |
| 4 | Veteran | 1400 | — |
| 5 | **Old Ways** | **2200** | Ceiling |

**Every rank gives the player something.** `REVISED 2026-08-09` — the earlier model had ranks 2–5
existing only to drive the enemy reaction curve, which meant a player grinding to Rank 5 got nothing
personally while the world got harder. The ladder in [02](02-weapon-mastery.md) now fills it:

| Rank | Player gets | Also |
|---|---|---|
| R1 | the signature power | reaction curve |
| R2 | — | reaction curve |
| R3 | a perk on the weapon's secondary attack | reaction curve |
| R4 | — | reaction curve |
| R5 | the primary chain connects into the secondary | reaction curve |

The single Proven number still drives both jobs, which is what makes the "steady curve scaling with
player mastery" decision work.

**Soft prerequisite:** vanilla skill ≥ 30 in that skill is also required for Rank 1. A console-raised
skill alone still unlocks nothing; the PP is the real gate.

## Earning Proven

### Base weights

| Event | Base PP | Track credited |
|---|---|---|
| Killing blow during an **active boss fight** | 25 | Weapon/school used |
| Killing blow on a **2★+ creature** | 10 | Weapon/school used |
| Killing blow on a **1★ creature** at or above tier | 4 | Weapon/school used |
| **Block a boss's telegraphed heavy attack** without breaking guard | 20 | Blocking |
| **Landed parry** against a non-trivial enemy | 6 | Blocking |
| **Clean sneak-attack kill** | 6 | Knives |
| **Spell lands its full effect** on a real threat | 8 | ElementalMagic / BloodMagic |

"Active boss fight" = the boss is aggro'd and alive; kills of its adds count too, which is intentional —
it rewards fighting *in* the trial, not just landing the last hit on the boss.

### Tier multiplier (anti-farm, applied to every event)

Player progression tier is derived from vanilla `ZoneSystem` global keys (boss defeats). Target tier is
the creature's biome tier.

| Target vs. player tier | Multiplier |
|---|---|
| Two or more tiers below | **0 — no credit** |
| One tier below | 0.5 |
| Same tier | 1.0 |
| Above player tier | 1.5 |

Player tier is **per-player**, not world state — see "Player tier source" below.

### Diminishing returns (anti-farm)

Per creature prefab, on a rolling **10-minute** window:

```
kill 1 → 100%   kill 2 → 60%   kill 3 → 36%   kill 4 → 22%   kill 5+ → 10% (floor)
```

Multiplier ×0.6 per repeat, floored at 10%. The counter for a prefab resets after 10 minutes with no
kills of that type. Boss-fight kills are **exempt** from DR — a boss fight is a discrete trial, not a
farm loop.

### Hard exclusions — no credit, ever

- AFK / training-dummy setups (no player-initiated damage in the last N seconds, or target has never
  dealt damage).
- Tamed or passive creatures.
- Friendly fire / other players.
- Creatures two or more tiers below the player (the 0× multiplier above).
- Damage-over-time or environmental kills where no weapon can be attributed.

### Worked pacing example

A player at Swamp tier killing tier-appropriate 2★ Draugr with a sword: 10 PP × 1.0 tier × DR. Five
kills of the same type ≈ 22 PP. Mixing creature types avoids DR, so a normal Swamp session with varied
combat lands roughly 60–120 PP. **Rank 1 (150 PP) ≈ two solid sessions** with one weapon, or a single
boss fight plus some field work. Rank 5 is a long-tail number reached over a full playthrough.

These values are a **first pass** — the ratios matter more than the absolutes, and they should be
config-exposed so the server can retune without a rebuild.

## Implementation — `IMPLEMENTED` Phase 1

- **Server-authoritative.** Clients never write Proven. See [07](07-technical-architecture.md).
- Stored per player ID, server-side, in `BepInEx/config/OldWays/proven_<worldUID>.dat`. Explicitly
  not in vanilla skills data. Written atomically (temp file + swap) and saved on world save and
  shutdown.
- **Damage attribution turned out to be the easy part.** Vanilla's own `HitData.m_skill` carries the
  skill the game will award XP to, so there is no need to infer the weapon from equipment or
  animation state. The last tracked hit on a creature is recorded and read back on death. DoT and
  environmental kills carry no attacker and so award nothing — which is the desired behavior anyway.
- Boss-fight state uses `Character.IsBoss()` over `Character.GetAllCharacters()` within 60 m.

### Known limitation: kill reports are client-originated

In Valheim a creature's ZDO is owned by whichever client is engaged with it, so **its death runs on
that client, not on the dedicated server**. There is no server-side hook that observes every kill.
The owning client therefore reports the kill and the server decides what it is worth.

What this still buys — everything the system was created for: the server owns the weights, tier
gating, diminishing returns, thresholds and storage, so Proven cannot be granted by `raiseskill`, by
editing a local file, or by changing config. What it does not buy: a modified client could forge kill
reports. That cannot be fully closed within Valheim's ownership model. A rate limit (30 reports /
10 s per peer) blunts the crude version.

### Unstarred creatures award nothing

A consequence of the weights, worth stating plainly: outside a boss fight, only 1★ and 2★ kills earn
Proven. Killing ordinary creatures earns zero. This is on-theme — you are proven by trial, not by
volume — but it means rank 1 is roughly 15 two-star kills, and a player who never fights starred
creatures will never unlock a power.

### Player tier source — `DECIDED 2026-08-08`, per-player. `IMPLEMENTED`

`ProgressionTier.PlayerTier()` reads vanilla boss-defeat global keys, which are **world state**, not
per-character. This was flagged as `PROPOSED` and never signed off, and testing has now shown why it
matters: a starred kill awarded nothing until the tester **reset the world's boss keys**. The gate
behaved exactly as specified — the specification is the problem.

Consequence on TaegukGaming specifically: a player joining an established server inherits the
server's progression tier immediately. If the server has Yagluth down, every creature below Plains
earns that newcomer **zero**, forever. They can never reach Rank 1, so they can never unlock a single
power — on a mod whose entire premise is earning mastery through trial. Combined with "only starred
kills count," the earn surface for a new player on a late-game server is close to nothing.

**Resolution: progression tier is now per-player**, stored on the player's own `ProvenRecord` and
advanced by the highest-tier creature they have personally killed. World boss keys are no longer
consulted at all.

- Everyone starts at tier 1 (Meadows) and grows into the gate.
- Tier only ever rises, never falls.
- The kill that promotes you is still **paid at the rate you earned it** — the gate is evaluated
  before the promotion applies.
- Immune to console commands for the same reason Proven is: it lives in the server-side store.
- Records written before this change simply lack the tier field and load at tier 1. That is the
  safe direction — an existing player earns more, not less, and climbs back within a few kills.

The anti-farm rule now means what it was always meant to mean: *you* have outgrown this creature,
rather than *someone on this server* has.

| | Before | After |
|---|---|---|
| Source | World boss-defeat keys | Highest-tier creature this player killed |
| Newcomer on a late-game server | Inherits max tier, can never earn | Starts at tier 1, earns normally |
| Reset by `resetkeys` | Yes | No |

## Decision log

| Date | Decision |
|---|---|
| (handoff) | Proven is a separate mod-tracked counter, not a vanilla skill value. |
| (handoff) | Proven is visible to the player. |
| 2026-08-06 | Tracked **per vanilla skill** → 11 tracks, shields stage 3 powers up the Blocking ladder. |
| 2026-08-06 | **Permanent**, no decay. |
| 2026-08-06 | 5-rank ladder; Rank 1 unlocks, Ranks 2–5 drive the enemy reaction curve. |
| 2026-08-06 | Weights, tier multiplier, and DR curve set as first-pass proposed values. |
| 2026-08-06 | **Phase 1 implemented.** Attribution via `HitData.m_skill`; store per world UID; `proven` console command. |
| 2026-08-06 | Highest applicable weight wins per kill rather than summing — stacking boss-fight on 2★ would break pacing. |
| 2026-08-08 | **Phase 1 verified in-game.** `raiseskill` confirmed unable to move Proven. |
| 2026-08-08 | Tier gate confirmed working *as specified* — and the specification found wanting for shared servers. |
| 2026-08-08 | **Progression tier is per-player**, from the highest-tier creature that player has killed. World boss keys no longer consulted. |
| 2026-08-09 | **Ranks 2–5 are no longer curve-only** — R3 and R5 now grant player-facing perks ([02](02-weapon-mastery.md)). |
| 2026-08-09 | **Unarmed added as a 12th track** so fist weapons are covered. |
