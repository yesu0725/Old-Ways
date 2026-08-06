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

## Granularity: 11 tracks, 13 powers

Per-skill tracking maps cleanly for every family except shields — Blocking is one vanilla skill
carrying three powers. Rather than splitting it, the three shield powers **unlock in stages up the
Blocking rank ladder**:

| Vanilla skill | Power(s) | Unlock rank |
|---|---|---|
| Swords, Knives, Clubs, Axes, Polearms, Spears, Bows, Crossbows, ElementalMagic, BloodMagic | one each | Rank 1 |
| **Blocking** | Parry negation → Shield bash → Brace buffer | **Rank 1 → 2 → 3** |

This is a direct consequence of the per-skill decision, and it's a good one: shields get a visible
progression instead of three simultaneous unlocks.

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

**Ranks 2–5 do not add new powers** (except Blocking). They exist to drive the enemy reaction curve —
see [04](04-boss-reactions.md) and [05](05-creature-reactions.md). This is what makes the "steady curve
scaling with player mastery" decision work: one number does both jobs.

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

## Implementation requirements

- **Server-authoritative.** Clients never write Proven. See [07](07-technical-architecture.md).
- Stored per character, server-side, keyed by player ID + character name. Explicitly not in vanilla
  skills data.
- Must survive death, world reload, and server restart.
- Requires damage attribution (which weapon/skill landed the killing blow) and boss-fight state
  tracking.

## Decision log

| Date | Decision |
|---|---|
| (handoff) | Proven is a separate mod-tracked counter, not a vanilla skill value. |
| (handoff) | Proven is visible to the player. |
| 2026-08-06 | Tracked **per vanilla skill** → 11 tracks, shields stage 3 powers up the Blocking ladder. |
| 2026-08-06 | **Permanent**, no decay. |
| 2026-08-06 | 5-rank ladder; Rank 1 unlocks, Ranks 2–5 drive the enemy reaction curve. |
| 2026-08-06 | Weights, tier multiplier, and DR curve set as first-pass proposed values. |
