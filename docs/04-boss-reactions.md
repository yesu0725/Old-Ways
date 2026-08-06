# Boss Reactions

Status: `DECIDED` in concept and gating, curve values `PROPOSED`

## Rule

**No new bosses, no reskins, no new movesets.** Take one attack the boss already has and give it a
**second act**, gated to trigger only against players with active Old Ways mastery. This is a
mastery-reactive layer, **not a permanent difficulty change**.

## The reaction curve — encounter-scoped

**The whole encounter scales off the highest-mastery player present.** `DECIDED 2026-08-06`

| Layer | Rule |
|---|---|
| **Gate** — does this boss have its second act at all this fight? | Yes if **any one player present is Rank 1+** |
| **Intensity** — how strong is it, for everyone? | Scaled to the **highest Old Ways Presence** among players in the encounter |

**Old Ways Presence** = a player's **highest Proven rank across all skills** (0–5). One number per
player, recomputed on Proven gain. Encounter Presence = the max across participants.

**Accepted consequence:** a veteran raises the fight for everyone in it. A newcomer brought to Bonemass
by a Rank 5 player faces the Rank 5 version of Festering Ground. This is deliberate — it keeps the boss
fight a single coherent encounter rather than each player experiencing a privately-tuned version of the
same ability, and it reads correctly in the fiction from [00](00-design-principles.md): the *land* is
reacting to a practitioner being present, not tuning itself politely per-target.

Practical effect on the server: bringing a new player to their first Bonemass is now something a
veteran should think about. That's a real social cost, and it's the intended trade — flag it in the
README so it isn't a surprise.

*(Supersedes the earlier per-player-intensity model. The Q1 "not global/server-wide" constraint still
holds: scaling is per encounter, driven by who is actually there — never a server difficulty setting,
and never anything a distant player's mastery can affect.)*

### Curve values (`PROPOSED`, first pass)

Chance for a reaction to fire on a given use of the parent ability:

| Presence | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|
| Reaction chance | **0% (vanilla)** | 22% | 34% | 46% | 58% | 70% |

`chance = 10% + 12% × presence`, and magnitude scalars (cloud spread rate, arc width, root duration)
scale on the same `presence / 5` fraction. Steady curve, no cliffs — matching the Q1 answer.

Encounter Presence is computed on boss aggro and **re-evaluated when a player joins or leaves** the
encounter — a Rank 5 player arriving mid-fight raises it; their death or departure lowers it again.

This also simplifies area effects: Bonemass's spreading cloud and Yagluth's fire patches are world
objects that can't be tuned per-target anyway. Under encounter scoping there's no special case — they
use the same number as everything else.

**The roster is complete at 7.** Confirmed 2026-08-06 against the server's current build: there is no
Deep North / 8th boss. If one ships in a future update, add a row and design its second act then —
until that happens, this table is the whole boss surface and Phase 7 is fully scoped.

## The reactions

| Boss | Biome | Existing ability extended | New skill | Effect |
|---|---|---|---|---|
| Eikthyr | Meadows | Lightning strikes | **Chain Bolt** | A strike has a chance to arc to a second nearby player/structure |
| The Elder | Black Forest | Root spikes in expanding rings | **Grasping Roots** | Spikes landing *near* (not on) a player briefly root them before the damage tick |
| Bonemass | Swamp | Lingering poison miasma cloud | **Festering Ground** | Cloud spreads outward if a player stands in it too long |
| Moder | Mountain | Frost-breath cone + sonic knockback scream | **Killing Frost** | Close-range breath stacks the existing cold/slow faster; occasionally briefly freezes swing speed |
| Yagluth | Plains | Meteor summon + ground-pound AoE | **Scorched Earth** | Ground-pound impact zones leave lingering fire patches instead of going inert |
| The Queen | Mistlands | Burrows and resurfaces at a random point | **Ambush Resurface** | Chance to resurface beneath the most recently damaged player instead of randomly |
| Fader | Ashlands | Straight-line firebreath, escalates at low HP | **Sweeping Flame** | At low HP, firebreath sweeps in a short arc instead of a fixed line |

## Design notes

- Each reaction must be **readable** — a player should be able to learn and counter it, not just eat
  surprise damage. Grasping Roots and Ambush Resurface are the two most at risk of feeling unfair;
  both need a telegraph.
- **Chain Bolt arcs to players only** — never to structures. `DECIDED 2026-08-06`. Removes the
  base-griefing risk on a shared server entirely.
- Bonemass's spreading cloud must have a hard cap on total area or it can lock out the arena.
- A group with **no** Proven player gets a completely vanilla fight. This must be verifiable — it's the
  main compatibility promise to server members who don't engage with the mod.

## Open items

- Per-boss curve overrides — Ambush Resurface at 70% may be far too punishing where Chain Bolt at 70%
  is fine. Expect per-reaction chance multipliers after playtest.
- Telegraph design for Grasping Roots and Ambush Resurface (the two least readable reactions).
- Bonemass cloud needs a hard area cap so it can't lock out the arena.

## Decision log

| Date | Decision |
|---|---|
| (handoff) | One extension per boss, on an ability the boss already has. |
| (handoff) | Reactions are mastery-gated, not a global difficulty setting. |
| 2026-08-06 | **Gate:** any one Rank 1+ player present switches reactions on for the encounter. |
| 2026-08-06 | ~~Intensity scaled per individual player~~ — superseded same day. |
| 2026-08-06 | **Intensity:** whole encounter scales off the **highest** Presence present; re-evaluated on join/leave. |
| 2026-08-06 | Chain Bolt targets players only, never structures. |
| 2026-08-06 | Roster confirmed complete at **7 bosses** — no Deep North boss exists. Phase 7 fully scoped. |
