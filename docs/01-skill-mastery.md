# Skill Mastery Tweaks

Status: `DECIDED` in concept and structure, values `PROPOSED` (first pass)

Small qualitative payoffs read off **existing vanilla skill values**. **Not gated behind
[Proven](03-proven-system.md)** — these are flavor/QoL and should be felt by any player who levels
normally.

**Scaling: each tweak scales across all four tiers** — 30 / 50 / 75 / 100. `DECIDED 2026-08-06`. No
tweak is a single on/off unlock; the payoff grows as the skill does.

## The tweaks

### Blocking — parry stagger negation
Chance that a well-timed parry **fully avoids** stagger on a heavy hit, not just reduces it.

| Skill 30 | 50 | 75 | 100 |
|---|---|---|---|
| 10% | 20% | 35% | 50% |

### Sneak — lingering silence
A sneak-attack kill grants **muffled footsteps** instead of stealth simply ending.

| Skill 30 | 50 | 75 | 100 |
|---|---|---|---|
| 2s | 3s | 4s | 6s |

### Jump — soft landing
Additional fall-damage reduction on top of vanilla Jump scaling, plus a **stagger-immunity window** on
hard landings. No double-jump, no air control — stays in vanilla physics.

| | 30 | 50 | 75 | 100 |
|---|---|---|---|---|
| Extra fall dmg reduction | 10% | 20% | 30% | 40% |
| Stagger-immune window | 0.3s | 0.5s | 0.7s | 1.0s |

### Run / Swim — early wind
Stamina regen **delay** after sprinting/swimming is shortened. Not a drain reduction.

| Skill 30 | 50 | 75 | 100 |
|---|---|---|---|
| −15% delay | −30% | −45% | −60% |

### Woodcutting / Pickaxes — clean hit
Chance a swing consumes **no tool durability**.

| Skill 30 | 50 | 75 | 100 |
|---|---|---|---|
| 5% | 10% | 15% | 25% |

Tracked separately per skill (Woodcutting for axes-on-trees, Pickaxes for mining).

### Weapon skills (all) — combo recovery
Subtle windup cue and reduced recovery on the **last attack in a combo**. Rewards control, not damage.

| Skill 30 | 50 | 75 | 100 |
|---|---|---|---|
| −5% recovery | −10% | −15% | −20% |

Applies to every weapon skill and is **independent of Proven** — a player with zero Proven still gets
this.

## Design notes

- No new numbers on the character sheet. These should read as "that felt good," not as a stat change.
- Values interpolate linearly between tiers, or snap at tiers — **TBD**. Proposed: snap, so crossing a
  tier is noticeable.
- The clean-hit chance caps at 25% so durability stays a real resource.
- The Sneak tweak overlaps with the Knives power in [02](02-weapon-mastery.md). Rule: they **do not
  stack** — the longer of the two durations applies.

## Open items

- Snap vs. interpolate between tiers.
- Individual config toggles (see [07](07-technical-architecture.md)) — planned, per-tweak.

## Decision log

| Date | Decision |
|---|---|
| (handoff) | These six tweaks are the v1 set; not Proven-gated. |
| 2026-08-06 | Every tweak **scales across all four tiers** (30/50/75/100), no single-shot unlocks. |
| 2026-08-06 | First-pass values set for all six. |
