# Creature Reactions — 2★ Behaviors

Status: `DECIDED` in concept and gating, tuning `PROPOSED`

## Rule

**Activate a behavior the creature already almost has**, rather than inventing a new elite affix —
affixes are BiomeLords' territory. Applies to **2★ creatures only**.

## Gating

Same encounter-scoped model as [boss reactions](04-boss-reactions.md), where a creature's "encounter"
is the set of players currently within its aggro range:

- **Gate:** the creature activates its behavior if **any player in its aggro range** is Rank 1+.
- **Intensity:** scaled to the **highest Old Ways Presence** among those players —
  `chance = 10% + 12% × presence`, the same curve.

Evaluated **on target/aggro change**, not once per spawn. A wolf pack that a veteran joins sharpens up
mid-fight, which is exactly the intended "the land reacts to a practitioner" read from
[00](00-design-principles.md).

A creature engaging only unproven players behaves exactly like vanilla.

## Meadows

| Creature | Behavior |
|---|---|
| Boar (2★) | A hit that doesn't kill triggers an **immediate second charge** instead of the normal recovery pause |
| Neck (2★) | Tail-whip knockback becomes strong enough to actually **shove a player off balance** near edges/water |

## Black Forest

| Creature | Behavior |
|---|---|
| Greydwarf Brute (2★) | Whiffing its heavy overhead smash leaves it **briefly staggered** (mirrors the recovery-frame concept already applied to player misses) |
| Skeleton archer (2★) | Actually **repositions for sightlines/cover** instead of standing still to shoot |

## Swamp

| Creature | Behavior |
|---|---|
| Draugr (2★) | Properly **raises its shield to block** instead of tanking hits face-on |
| Oozer / Blob (2★) | **Splits into smaller Blobs at a health threshold** *before* death, not only on the kill |
| Leech (2★) | Uses its existing **submerge** mechanic to disengage/reposition after a failed lunge |

## Mountain

| Creature | Behavior |
|---|---|
| Wolf (2★) | Pack members **stagger their attacks** (one lunges while others circle) instead of piling in simultaneously |
| Fenring (2★) | **Howl cancels its own recovery frame**, allowing an immediate follow-up |

## Plains

| Creature | Behavior |
|---|---|
| Lox (2★) | Stomp knockback radius properly **matches its actual hitbox** (currently under-reaches) |
| Deathsquito (2★) | **Breaks off and repositions** after a failed dive instead of immediately re-diving |

## Mistlands

| Creature | Behavior |
|---|---|
| Seeker Soldier (2★) | Carapace plating **deflects frontal hits more consistently**, rewarding flanking/backstab positioning |
| Tick (2★) | Latching also **saps stamina regen** in addition to health |

## Ashlands

| Creature | Behavior |
|---|---|
| Charred Melee / Archer (2★) | Melee **actively flanks** while the archer holds range (uses AI logic already partially present for Charred squads) |
| Morgen (2★) | A failed ambush burrow leads to an **immediate second burrow-charge from a different angle** |

## Design notes

- The Wolf pack-staggering change makes packs *more* survivable in some situations and *harder* in
  others — it's a feel change, not a pure buff. Verify it doesn't trivialize wolf packs.
- Seeker Soldier frontal deflection is the one entry that risks becoming a wall for players without
  backstab tools (mages, bows). Cap the deflection or exclude ranged.
- Oozer pre-death splitting multiplies entity count — watch performance on a shared server.
- Tick stamina drain stacks with the Tick's existing health drain; confirm it can't chain-lock a player
  out of blocking.

## Open items

- Whether these apply to 3★+ creatures if other mods add them. **PROPOSED:** yes, treat 3★+ as 2★ for
  gating purposes rather than excluding them.
- Retarget re-evaluation cost with large packs — needs a profiling pass.
- Per-creature config toggles (planned, see [07](07-technical-architecture.md)).

## Decision log

| Date | Decision |
|---|---|
| (handoff) | 2★-only; behaviors must be latent in vanilla AI, not new affixes. |
| 2026-08-06 | Encounter-scoped: gate on any Rank 1+ player in aggro range, intensity on the **highest** Presence there. |
| 2026-08-06 | Re-evaluated on target/aggro change, not once per spawn. |
