# The Old Ways — Design Handoff

**Status:** Concept/design phase, not yet implemented. This document is the initial reference for starting implementation in Claude Code.

**Author's context:** Built for the TaegukGaming Valheim server, maintained alongside two existing mods by the same author:
- **BiomeLords** (https://github.com/yesu0725/BiomeLords) — one summonable named "Lord" boss per biome, hunted via a horn relic, drops a trophy that grants a passive Blessing + a Forsaken Power.
- **Lost Scrolls II** (https://github.com/yesu0725/Lost-Scrolls-II) — recruit corrupted Dvergr as levelable companion allies (chores, duels, totems).

The Old Ways must stay clear of both lanes: **no new named/summonable uniques, no companion/ally system.** Its territory is the player's own skill and weapon mastery, plus small, existing-moveset-only enhancements to normal creatures and bosses in response.

Build philosophy carried over from both existing mods: **vanilla assets only** — no custom models, textures, or asset bundles. Every effect should read as "finishing" something vanilla already started (an existing mechanic, animation, status effect, or AI behavior), not a bolted-on new system.

---

## 1. Lore framing

Not gifts summoned from a boss or a relic. The premise: the old warriors, hunters, and builders of Midgard knew techniques that have been lost — not through instruction, but through **being proven in trial**. A player's body doesn't "remember" a technique because they practiced it (that's what vanilla skill XP already represents); it remembers because they were *tested* by something real and came out ahead. This is the in-fiction justification for the two-layer gating system in section 3, and for why enemies get sharper in response — the land is reacting to a practitioner of the Old Ways, not spawning a new threat out of nowhere.

---

## 2. Two systems, kept separate

1. **Skill mastery tweaks** — small qualitative payoffs read off *existing* vanilla skill values (Blocking, Sneak, Jump, Run/Swim, Woodcutting, weapon skills). These are flavor/QoL, not gated behind Proven.
2. **Weapon/shield/magic mastery powers** — one new capability per weapon family (section 4), gated behind the **Proven** system (section 5), not vanilla skill level alone.

---

## 3. Skill mastery tweaks (vanilla skill values → qualitative payoff)

Threshold-gated (e.g. 30/50/75/100 skill), extending mechanics vanilla already has instead of adding new ones:

- **Blocking** — a well-timed parry at high mastery has a chance to fully avoid stagger on heavy hits, not just reduce it.
- **Sneak** — breaking stealth via a sneak-attack kill grants a few seconds of muffled footsteps afterward, instead of stealth simply ending.
- **Jump** — further reduced fall damage scaling, plus a brief stagger-immunity window on hard landings. No double-jump — stays grounded in vanilla physics.
- **Run/Swim** — stamina regen kicks in slightly earlier while sprinting/swimming at mastery, rather than a flat drain reduction.
- **Woodcutting/Pickaxes** — small chance at high mastery of a "clean hit" that doesn't consume tool durability.
- **Weapon skills generally** — a subtle windup cue or slightly faster recovery on the last attack in a combo at high mastery, rewarding control rather than adding flat damage.

---

## 4. Weapon/shield/magic mastery powers

One new capability per weapon family. **Design rule: trigger off an input the weapon already uses** (secondary attack, parry timing, charged attack, block-and-hold, stealth backstab) — no new hotkey, no cooldown-gauge UI. This also keeps it clear of BiomeLords' Forsaken Power slot.

| Weapon/School | Trigger (existing input) | New capability |
|---|---|---|
| Swords | Parry → immediate attack | Guaranteed critical stagger on the follow-up hit |
| Knives | Sneak-attack kill | Full stamina refund + brief near-silent footsteps |
| Clubs | Charged heavy swing | Small stagger-radius pulse on impact (extends existing knockback) |
| Axes | Heavy swing vs. already-staggered, low-HP target | Executes the target outright |
| Polearms/Atgeir | Spin secondary attack | No stamina drain mid-spin + poise resistance during the spin |
| Spears | Charged throw | Pierces through the first target into whatever's behind it |
| Bows | Holding past full draw (currently plateaus) | "True shot" — ignores target's stagger resistance |
| Crossbows | Reload | Near-full move speed retained while reloading |
| Shields (parry) | Perfect-timed parry | Fully negates own stagger + briefly punishes attacker's poise |
| Shields (bash) | Shield bash (secondary attack) | Reduced stamina cost, staggers enemy types normally immune to bash |
| Shields (brace) | Hold block without moving | Builds a buffer over time that can absorb one otherwise guard-breaking hit |
| Elemental Magic | Fully-charged cast | Chance to consume no Eitr |
| Blood Magic | Cast above a HP-cost threshold | Brief lifesteal on the next hit landed within a few seconds |

**Scope decision (confirmed):** v1 includes **all** of the above families — one-handed, two-handed/polearms, ranged, shields, and both magic schools — not a staggered rollout.

---

## 5. Proven — the anti-cheese gate

**Problem:** vanilla skill level is just an int in the save file (`raiseskill` sets it instantly), so gating mastery powers on skill level alone makes them free to console-command.

**Solution:** a separate, mod-tracked counter per weapon/school called **Proven**, earned only through qualifying combat events — vanilla skill commands cannot touch it. Vanilla skill level remains a soft prerequisite/flavor ("enough hands-on time before your body's ready"), but Proven is what actually unlocks the power.

**Confirmed: Proven is visible to the player** — an in-game trial log showing progress toward each weapon/school's mastery power (not hidden/mysterious).

**What earns Proven (weighted, not flat per-hit):**
- Killing blow on a 2★+ creature with that weapon — high weight
- A landed parry against a non-trivial enemy — high weight (Blocking)
- A kill during an active boss fight — highest weight
- A clean sneak-attack kill (Knives) — weighted by target tier
- Blocking a boss's telegraphed heavy attack without breaking guard — high weight (Shields)
- A spell landing its full effect on a real threat (not trash mobs) — Magic/Blood Magic

**What does NOT earn Proven (anti-farm):**
- Diminishing returns on repeated kills of the same creature type in a short window
- No credit from creatures well below the player's own progression tier
- No credit from AFK/training-dummy setups, tamed/passive creatures, or friendly fire

**Open implementation question:** does a mastery power require *every* player in a group to have sufficient Proven, or just one player present? Not yet decided.

---

## 6. Creature side — bosses

No new bosses, no reskins, no new movesets. Rule: **take one attack the boss already has and give it a second act**, gated to trigger only against players with active Old Ways mastery (a mastery-reactive layer, not a permanent difficulty change).

Current vanilla boss roster (7, as of the Ashlands update — Deep North/8th boss not yet released as of this doc):

| Boss | Biome | Existing ability being extended | New skill |
|---|---|---|---|
| Eikthyr | Meadows | Calls down lightning strikes | **Chain Bolt** — a lightning strike has a chance to arc to a second nearby player/structure |
| The Elder | Black Forest | Root spikes erupt in expanding rings | **Grasping Roots** — spikes landing near (not on) a player briefly root them before the damage tick |
| Bonemass | Swamp | Lingering poison miasma cloud | **Festering Ground** — cloud spreads outward if a player stands in it too long |
| Moder | Mountain | Frost-breath cone + sonic knockback scream | **Killing Frost** — close-range breath stacks the existing cold/slow effect faster, occasionally briefly freezing swing speed |
| Yagluth | Plains | Meteor summon + ground-pound AoE | **Scorched Earth** — ground-pound impact zones leave lingering fire patches instead of going inert |
| The Queen | Mistlands | Burrows and resurfaces at a random point | **Ambush Resurface** — chance to resurface beneath the most recently damaged player instead of randomly |
| Fader | Ashlands | Straight-line firebreath, moveset already escalates at low HP | **Sweeping Flame** — at low HP, firebreath sweeps in a short arc instead of staying a fixed line |

---

## 7. Creature side — 2★ creatures

Rule: **activate a behavior the creature already almost has**, rather than inventing a new elite affix (that's BiomeLords' territory). Grouped by biome:

**Meadows**
- **Boar (2★)** — a hit that doesn't kill triggers an immediate second charge instead of the normal recovery pause.
- **Neck (2★)** — tail-whip knockback becomes strong enough to actually shove a player off balance near edges/water.

**Black Forest**
- **Greydwarf Brute (2★)** — whiffing its heavy overhead smash leaves it briefly staggered (mirrors the recovery-frame concept already applied to player misses).
- **Skeleton archer (2★)** — actually repositions for sightlines/cover instead of standing still to shoot.

**Swamp**
- **Draugr (2★)** — properly raises its shield to block instead of tanking hits face-on.
- **Oozer/Blob (2★)** — splits into smaller Blobs at a health threshold *before* death, not only on the kill.
- **Leech (2★)** — uses its existing submerge mechanic to disengage/reposition after a failed lunge.

**Mountain**
- **Wolf (2★)** — pack members stagger their attacks (one lunges while others circle) instead of piling in simultaneously.
- **Fenring (2★)** — howl cancels its own recovery frame, allowing an immediate follow-up.

**Plains**
- **Lox (2★)** — stomp knockback radius properly matches its actual hitbox size (currently under-reaches).
- **Deathsquito (2★)** — breaks off and repositions after a failed dive instead of immediately re-diving.

**Mistlands**
- **Seeker Soldier (2★)** — carapace plating deflects frontal hits more consistently, rewarding flanking/backstab positioning more than vanilla currently does.
- **Tick (2★)** — latching also saps stamina regen in addition to health.

**Ashlands**
- **Charred Melee/Archer (2★)** — melee actively flanks while the archer holds range (uses AI logic already partially present for Charred squads).
- **Morgen (2★)** — a failed ambush burrow leads to an immediate second burrow-charge from a different angle.

---

## 8. Open questions (not yet decided)

- How aggressive should the boss/creature-reaction curve feel — subtle and rare, or a steady curve alongside player growth?
- Does a mastery power's boss/2★-trigger condition require every player in a group to have sufficient Proven, or just one player present?
- Exact Proven thresholds/weights per event type (numbers not yet set — the weighting *categories* above are decided, not the values).
- UI/UX for the visible trial log (in-game panel vs. existing inventory/skill-screen integration).

---

## 9. Explicit non-goals (to keep scope-checked against BiomeLords/Lost Scrolls II)

- No new named/summonable unique bosses.
- No recruitable/levelable ally creatures.
- No new hotkey-bound "power" (that's the Forsaken Power slot's role already).
- No custom models/textures/asset bundles — vanilla assets only.
