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
| Swords | Parry → immediate attack | Guaranteed **critical stagger** on the follow-up hit | Swords R1 · `IMPLEMENTED` |
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

### Swords — Riposte (`Powers/SwordRiposte.cs`)

Perfect block → the next sword hit within a window (default 3 s) calls `Character.Stagger()`
directly, so it lands regardless of the target's accumulated stagger damage or resistance.

Parry detection reads the same two fields vanilla uses to decide a perfect block — `m_blockTimer`
against the static `m_perfectBlockInterval` — rather than re-deriving the timing. One parry buys
exactly one riposte: the armed state is spent on the next qualifying hit whether or not the target
survives.

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
| 2026-08-08 | **Phase 2: Swords Riposte implemented.** Power pipeline established via `PowerGate`. |
