# Weapon / Shield / Magic Mastery Powers

Status: `DECIDED` in concept (handoff §4), all values `TBD`

One new capability per weapon family. **v1 includes all families listed below** — confirmed, not a
staggered rollout.

## Design rule

**Trigger off an input the weapon already uses** — secondary attack, parry timing, charged attack,
block-and-hold, sneak backstab, reload. No new hotkey. No cooldown-gauge UI. This keeps the mod clear
of BiomeLords' Forsaken Power slot.

## The powers

| Weapon / School | Trigger (existing input) | New capability | Gate |
|---|---|---|---|
| Swords | Block / parry with the sword itself | **Duelist's Guard** — sword blocks and parries at shield strength | Swords R1 · `IMPLEMENTED` |
| Knives | Sneak-attack kill | **Full stamina refund** + brief near-silent footsteps | Knives R1 |
| Clubs | Charged heavy swing | Small **stagger-radius pulse** on impact (extends existing knockback) | Clubs R1 |
| Axes | Heavy swing vs. already-staggered, low-HP target | **Executes** the target outright | Axes R1 |
| Polearms / Atgeir | Spin secondary attack | **No stamina drain** mid-spin + poise resistance during the spin | Polearms R1 |
| Spears | Charged throw | **Pierces through** the first target into whatever's behind it | Spears R1 |
| Bows | Holding past full draw (currently plateaus) | **"True shot"** — ignores target's stagger resistance | Bows R1 |
| Crossbows | Reload | Near-full **move speed retained** while reloading | Crossbows R1 |
| Shields (parry) | Perfect-timed parry | Fully negates own stagger + briefly **punishes attacker's poise** | **Blocking R1** |
| Shields (bash) | Shield bash (secondary attack) | Reduced stamina cost; **staggers enemy types normally immune** to bash | **Blocking R2** |
| Shields (brace) | Hold block without moving | Builds a **buffer over time** that absorbs one otherwise guard-breaking hit | **Blocking R3** |
| Elemental Magic | Fully-charged cast | Chance to **consume no Eitr** | ElementalMagic R1 |
| Blood Magic | Cast above an HP-cost threshold | Brief **lifesteal** on the next hit landed within a few seconds | BloodMagic R1 |

Gates are Proven ranks per **vanilla skill** ([03](03-proven-system.md)). Rank 1 = 150 PP + vanilla
skill ≥ 30. **Shields are the exception**: Blocking is a single vanilla skill, so its three powers
stage up the rank ladder (R1 → R2 → R3) rather than all unlocking at once. `DECIDED 2026-08-06`

## Implementation — the power pipeline

Established by the Phase 2 vertical slice (Swords) and reused by every later power:

- **`Powers/PowerGate.cs`** — the single place a power asks "am I allowed to fire?". Checks the
  master switch, the category switch, Proven rank, and (for rank 1 only) the vanilla skill
  prerequisite. Defined once rather than thirteen times.
- **One file per power** in `Powers/`, each with its own level-2 config toggle plus any tuning value
  it needs.
- Powers are **local-player-only** and never fire against another player.
- The client evaluates its own gate against the Proven record the server pushed it. That record is
  not client-writable, so `raiseskill`, save editing and config editing do not open a power. A
  modified client could still fire one — the same limit as kill reports ([07](07-technical-architecture.md)).

### Swords — Duelist's Guard (`Powers/SwordDuelistsGuard.cs`)

Blocking and parrying with a sword works at shield strength: block power and deflection force are
multiplied (default ×2.5). Vanilla already lets you block with a sword — it is simply so weak that
nobody does, so the input exists and goes unused. Mastery makes shieldless sword play viable.

Both `GetBlockPower` and `GetDeflectionForce` are boosted. Block power alone would absorb the hit
but fail to throw the attacker off, which is half a parry and would feel broken.

**Implementation constraint worth remembering:** this patches the *methods*, never the
`m_blockPower` / `m_deflectionForce` fields. Those live on `SharedData`, which is shared by every
instance of an item type — writing to them would permanently buff every sword in the world for every
player, and would persist after the power was disabled.

### Cut: Riposte (the original Swords power)

The handoff specified "parry → guaranteed critical stagger on the follow-up hit." **Cut 2026-08-08
after implementation, as a no-op.** Vanilla already staggers an attacker on a perfect block, and
already applies `c_StaggerDamageBonus` to hits against a staggered target — so the power staggered
something already staggered and claimed credit for a bonus the game was going to give anyway. A
player could not perceive it.

Kept here as a warning: a power that *reads* well in a design table can still be invisible in play.
The test for the remaining twelve is not "does this sound good" but "what does vanilla already do
here, and is there anything left for us to add?"

## Notes per family

- **Bows** — the "hold past full draw" window currently does nothing in vanilla; this power gives that
  dead input a purpose without changing draw timing for anyone who doesn't hold.
- **Axes** — the execute needs a hard, readable condition (staggered AND below an HP%) or it becomes an
  invisible random one-shot. Threshold **TBD**.
- **Shields (brace)** — the only power with a charge-up. It must have **no UI meter** per the design
  rule; the feedback has to be diegetic (shield visual/audio state) or nothing.
- **Blood Magic** — lifesteal must not scale with the HP cost paid, or it becomes a self-damage loop
  exploit. Fixed or capped value.
- **Knives** — overlaps with the Sneak skill tweak in [01](01-skill-mastery.md); confirm the two stack
  cleanly rather than double-applying the muffled-footsteps effect.

## Multiplayer

A power belongs to the player who earned it and works whenever that player wields the weapon —
regardless of what anyone else in the group has. The "any one Proven player present" rule
`DECIDED 2026-08-06` governs whether **enemy reactions** switch on for the encounter, not whether a
player's own power fires. See [04](04-boss-reactions.md).

## Open items

- Whether powers have an internal cooldown to prevent chaining (e.g. sword parry-crit spam).
  **PROPOSED:** no hard cooldown — every trigger already costs stamina and requires a precise input,
  which is the intended rate limit. Revisit if playtest shows parry-crit looping.
- Axe execute HP threshold (**TBD**, needs a readable number — proposed ≤15% and staggered).
- Blood Magic lifesteal must be fixed/capped, not scaled to HP paid, or self-damage loops exploit it.

## Decision log

| Date | Decision |
|---|---|
| (handoff) | All 13 families ship in v1 together. |
| (handoff) | No new hotkey; every trigger reuses an existing input. |
| 2026-08-06 | Gates are per-vanilla-skill Proven ranks; shields stage across Blocking R1/R2/R3. |
| 2026-08-06 | A player's own power is never gated on other players' Proven. |
| 2026-08-08 | **Phase 2 power pipeline established** via `PowerGate`. |
| 2026-08-08 | **Riposte cut as a no-op** — vanilla already staggers on parry and already rewards hitting staggered targets. |
| 2026-08-08 | **Swords power is now Duelist's Guard** — sword blocks/parries at shield strength, ×2.5 configurable. |
