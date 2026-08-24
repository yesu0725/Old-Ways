# Weapon / Shield / Magic Mastery Powers

Status: `DECIDED` — full five-rank ladder, revised 2026-08-09 against verified attack data.
Swords `IMPLEMENTED`; everything else pending Phases 3–5.

## Design rule

**Trigger off an input the weapon already uses.** No new hotkey, no cooldown-gauge UI. This keeps
the mod clear of BiomeLords' Forsaken Power slot.

A second rule, learned the hard way (see *Cuts* below): **check what vanilla already does at that
trigger before designing anything.** A power that reads well in a table can be invisible in play.

## The ladder

Every weapon family follows the same shape. This replaces the earlier model where ranks 2–5 existed
only to drive the enemy reaction curve — a player grinding to Rank 5 now gets something at each step
that matters to them, not only to the enemies.

| Rank | What it gives |
|---|---|
| **R1** | the family's signature power |
| **R2** | reaction-curve step only |
| **R3** | a perk on the weapon's **secondary attack** |
| **R4** | reaction-curve step only |
| **R5** | the full primary chain **connects into the secondary attack**, and that finisher carries its R3 perk |

Blocking has no attack and keeps its own ladder. Five families cannot support the R5 chain at all —
see *Where R5 cannot apply*.

## The powers

| Family | R1 signature | R3 secondary perk | R5 |
|---|---|---|---|
| **Swords** | **Duelist's Guard** — sword blocks/parries at shield strength | **Thread the Gap** — the thrust cannot be blocked or deflected | chain → thrust |
| **Knives** | **Vanish** — a sneak-attack kill clears nearby alert state, letting you re-enter stealth | **Falling Fang** — the leaping stab, begun unseen, lands as a full backstab regardless of facing, at normal stamina | chain → leap |
| **Clubs** | **Guard Crusher** — hits break through a blocking enemy's guard | **Uplift** — the rising swing throws the target off its feet; it lands staggered | chain → uppercut |
| **Axes** | **Hook** — the charged attack *pulls* the target toward you | **Cleave** (2H thrust, passes into a target behind) / **Splitting Blow** (1H chop, staggers target and adjacent) | chain → secondary |
| **Polearms** | **Set Against the Charge** — a braced block impales a charging creature | **Whirlwind** — the spin becomes a double rotation that advances forward, uninterruptible | chain → whirlwind |
| **Spears** | **Impale** — a charged throw pins a non-boss target briefly | **Recall** — press block to return a thrown spear to your hand | **instant throw** — see below |
| **Bows** | **Piercing Shot** — holding past full draw pierces targets; count scales with rank (1→5) | **Snap Kick** | no damage falloff between pierced targets |
| **Crossbows** | **Steady Aim** — a braced, stationary shot ignores armour and cannot be deflected | **Snap Kick** | reload at full movement speed |
| **Unarmed** | **Flow** — landing blows keeps your attack chain from resetting | **Snap Kick** | **punch → punch → kick** |
| **Elemental** | **Elemental Detonation** — see the combo table | **Snap Kick** | detonations chain to further afflicted targets |
| **Blood** | per-staff ladder — see below | **Snap Kick** | per-staff ladder |
| **Blocking** | **Deflection** — a perfect parry returns a projectile or spell to its sender | R3 **Immovable** — cannot be knocked back or displaced while braced | R5 **Unbreakable** — a single hit can no longer break your guard |

### Snap Kick

Shared R3 for every family whose secondary is the kick — **Unarmed, Bows, Crossbows, Elemental,
Blood**. Confirmed in play: weapons that define no secondary fall back to the unarmed kick.

Sharply reduced wind-up and stronger knockback, and it can be used mid-draw, mid-reload or mid-cast
**without losing the nock or charge**. That last clause is the point: today a Deathsquito reaching a
drawing archer costs the whole shot. One honest perk covering five families beats five thin ones.

### Elemental Detonation (R1)

| Target state | Struck by | Result |
|---|---|---|
| Burning | **Frost** | explodes; applies frost to creatures around the target |
| Frozen | **Fire** | explodes, damages nearby, applies burning to them |
| Frosted / frozen | **Lightning** | amplified lightning damage (no explosion) |

The intended loop is **Fire → Frost → Lightning**: ignite one, detonate with frost to freeze the
pack, then lightning the frozen pack for amplified damage. Each explosion propagates the *opposite*
status outward, so every detonation seeds the next. It is self-teaching — a player finds the third
step by accident.

`StaffGreenRoots` has no direct attack (the poison comes from the summoned
`staff_greenroots_tentaroot`), so it cannot detonate. It ladders as a summon staff instead: chance
to summon multiple tentaroots per cast, scaling with rank. **TBD:** whether it counts as
ElementalMagic or BloodMagic — that lives in prefab data.

### Blood Magic — per staff

| Staff | Ladder |
|---|---|
| **StaffSkeleton** | R1 one cast summons the full count instantly · R2 may hold a one-hander in the right hand, secondary attacks only, 40% damage · R3 60% · R4 80% · R5 100% |
| **StaffShield** | the ward also heals players in range; radius and rate scale with rank |
| **StaffRedTroll** | chance of an extra `Troll_Summoned`, scaling by rank, capped below 100% at R5 (10/20/30/40/50%) |
| **StaffGreenRoots** | chance of multiple tentaroots per cast, scaling by rank |

**Lane check (deliberate):** Lost Scrolls II owns *recruited, persistent, levelable companions*.
These are disposable combat summons that already exist in vanilla, and the StaffSkeleton weapon perk
is about the player, not the summons. Judged far enough apart — recorded so it is a considered call,
not an accident.

## Where R5 cannot apply

Verified from the attack dump. The chain finisher needs both a primary chain and a secondary:

| Family | Chain | Secondary | R5 |
|---|---|---|---|
| Swords, Knives, Clubs (maces), Axes, Polearms, Unarmed | 3–4 | yes | chain → secondary ✅ |
| **Spears** | **0** | throw | **instant throw** — after primary pokes, choosing secondary throws with no wind-up. Same intent, expressed on a weapon with no chain |
| **Sledges** | 0 | none | ❌ nothing at R5. Accepted gap |
| **Bows, Crossbows** | 0 | kick only | R1 scaling instead |
| **Staves** | 2 (fireball type) | kick only | R1 scaling instead |

## Verified attack data (2026-08-09)

From `oldways_dumpweapons`. Worth not re-deriving:

| Family | Secondary | Type | Notes |
|---|---|---|---|
| Swords 1H | `sword_secondary` | Horizontal | thrust, range 2.7 vs 2.4 primary |
| Greatswords | `greatsword_secondary` | Horizontal | range 3.0 vs 2.6 |
| Knives | `knife_secondary` | Horizontal | **leaping downward stab**, 3× stamina (12 → 36) |
| Clubs (maces) | `mace_secondary` | Vertical | **rising swing**, range 2.5 |
| Sledges | none | — | Area primary, chain 0 |
| Axes 1H | `axe_secondary` | Vertical | overhead chop |
| Dual axes | `dualaxes_secondary` | Horizontal | **quick thrust**; primary chains 4 |
| Battleaxe | `battleaxe_secondary` | Horizontal | **quick thrust**; costs *half* the primary |
| Atgeir | `atgeir_secondary` | Horizontal | the spin |
| Spears | `spear_throw` | Projectile | the throw; primary chain **0** |
| Fists | `unarmed_kick` | Horizontal | the kick |
| Bows, Crossbows, Staves | `(none)` defined | — | **fall back to the unarmed kick in play** |

Attack `type` is the motion plane, not the direction — `Vertical` covers both the axe's downward
chop and the mace's upward swing. Reading it as "overhead" led to two wrong perk proposals.

## Implementation — the power pipeline

- **`Powers/PowerGate.cs`** — the single place a power asks "am I allowed to fire?". Checks the
  master switch, the category switch, Proven rank, and (for rank 1 only) the vanilla skill
  prerequisite.
- **One file per power** in `Powers/`, each with its own level-2 config toggle plus tuning values.
- Powers are **local-player-only** and never fire against another player.
- The client evaluates its own gate against the server-pushed Proven record. That record is not
  client-writable, so `raiseskill`, save editing and config editing do not open a power. A modified
  client could still fire one — the same limit as kill reports ([07](07-technical-architecture.md)).

### Unarmed — Flow (`Powers/UnarmedFlow.cs`) `IMPLEMENTED`

While you keep landing bare-handed blows, the attack chain never resets. Vanilla drops the chain
back to its first level after `m_chainAttackMaxTime`, which for fists means rarely reaching the
finisher and its `m_lastChainDamageMultiplier`.

**Not attack speed, and why.** Flow was first specified as shortening the next punch's recovery.
Valheim has **no per-attack speed lever** — timing is animation-driven, and the only control is
`ZSyncAnimation.SetSpeed`, which scales the character's whole animator (walking, blocking,
everything) and is network-synced. `Attack.m_speedFactor` turned out to be movement speed *during*
an attack. Chain Flow expresses the same intent through a mechanism vanilla actually has, and it
feeds straight into the R5 punch → punch → kick.

**Implementation constraint:** patches `Attack.CanStartChainAttack` (per-attack), **not**
`m_chainAttackMaxTime` — that is a private *static*, so writing it would change chain timing for
every weapon in the game for every player. Same class of trap as `SharedData` in Duelist's Guard.

**Known simplification:** a miss does not break the streak outright, it just fails to refresh it, so
the window lapses. Valheim has no clean "this swing hit nothing" signal, and a lapse reads the same
in play. Taking a hit ends the streak immediately.

### Swords — Duelist's Guard (`Powers/SwordDuelistsGuard.cs`) `IMPLEMENTED`

Block power and deflection force ×2.5. Both are needed: block power alone absorbs the hit but fails
to throw the attacker off, which is half a parry.

**Patches the *methods* `GetBlockPower`/`GetDeflectionForce`, never the `m_blockPower` /
`m_deflectionForce` fields.** Those live on `SharedData`, shared by every instance of an item type —
writing them would permanently buff every sword in the world for every player and persist after the
power was disabled.

## Visual feedback — `IMPLEMENTED 2026-08-09`

Every power that fires plays a **vanilla** effect prefab, plus a rank-up effect and message when a
Proven rank is earned. `Util/Effects.cs`; names configured under `5 - Visual Effects`.

**Vanilla assets only** (CLAUDE.md) — nothing here creates art. Effects are existing prefabs looked
up from `ZNetScene` by name.

Effect names live in prefab data and cannot be verified statically, which is exactly the trap that
produced Riposte and shield bash. Two defences:

- Each entry is a **comma-separated candidate list**; the first name that exists wins, so a wrong
  guess costs nothing as long as one candidate is real.
- A list where nothing resolves **logs once, naming every candidate tried**, then stays silent. A
  missing effect must never spam or throw — it is decoration.

Run **`oldways_dumpeffects`** for the real prefab list and put verified names in config.

| Trigger | Config entry |
|---|---|
| Proven rank earned | `Rank Up` — also prints *"Your hands remember. &lt;Skill&gt; — &lt;Rank&gt;."* |
| Sword parry carried by Duelist's Guard | `Swords - Duelists Guard` |
| A club breaking a raised guard | `Clubs - Guard Crusher` |
| A target dragged in by Hook | `Axes - Hook` |
| Slipping out of sight with Vanish | `Knives - Vanish` |
| A pinned target | `Spears - Impale` |
| A charger impaling itself | `Polearms - Set Against The Charge` |

Deliberate restraints:

- **Duelist's Guard fires on parries only.** A held block firing on every incoming hit would be
  noise, and the power is about the parry.
- **Vanish fires only when it actually cleared someone.** An effect on a no-op reads as a lie.
- **Flow has no effect.** It triggers on every held chain link; anything visual would strobe.

**Known limitation:** effects are spawned locally and are **not networked**, so other players do not
see them. Acceptable while every power is local-player-only. Revisit if a power ever needs to read
to a group.

## Cuts

Powers that were designed, found wanting, and removed. Kept because the reasons generalise.

| Cut | Was | Why |
|---|---|---|
| **Riposte** (Swords) | parry → guaranteed critical stagger | Vanilla already staggers an attacker on a perfect block *and* applies `c_StaggerDamageBonus` against staggered targets. It staggered something already staggered and took credit for a bonus the game was going to give anyway. **Implemented, playtested, invisible.** |
| **Shield Bash** (Blocking R2) | bash staggers bash-immune types | **There is no shield bash in Valheim** — zero occurrences of "bash" in `assembly_valheim.dll`. It would have required inventing a new input, breaking the design rule. |
| Knives' muffled footsteps | sneak kill → quiet steps | Already granted by the Sneak tweak in [01](01-skill-mastery.md). We would have collided with ourselves. |
| Clubs' stagger pulse | charged heavy → AoE stagger | The sledgehammer already *is* a charged AoE stagger (`DoAreaAttack`). |
| Axes' execute | heavy vs staggered low-HP → kill | The stagger damage bonus usually kills that target anyway — the Riposte trap again. |
| Shields' stagger negation | perfect parry → negate own stagger | Vanilla perfect block already does this. |
| Polearms' free spin | spin costs no stamina | A cost reduction, not a capability. The poise half survived into Whirlwind. |
| Crossbows' mobile reload | reload at speed | Too thin for a signature; survives as the R5 perk. |
| Elemental's free cast | chance to consume no Eitr | RNG cost reduction, not a technique. |

## Open items

- Axe execute threshold — moot, the power was cut.
- `StaffGreenRoots` skill type (ElementalMagic vs BloodMagic).
- Whether powers need internal cooldowns. **PROPOSED:** no — stamina cost and input precision are
  the rate limit. Revisit after playtest.
- Sledges have no R5. Accepted for now.

## Decision log

| Date | Decision |
|---|---|
| (handoff) | All families ship in v1 together; no new hotkey. |
| 2026-08-06 | Gates are per-vanilla-skill Proven ranks. |
| 2026-08-06 | A player's own power is never gated on other players' Proven. |
| 2026-08-08 | Power pipeline established via `PowerGate`. |
| 2026-08-08 | Riposte cut as a no-op; Swords power is now Duelist's Guard. |
| 2026-08-09 | **Five-rank ladder adopted** — R1 signature, R3 secondary perk, R5 chain→secondary. Ranks 2–5 are no longer curve-only. |
| 2026-08-09 | **Shield bash cut** — it does not exist in Valheim. Blocking ladder rebuilt on parry and held block. |
| 2026-08-09 | Nine of thirteen powers replaced after auditing the handoff table against vanilla behaviour. |
| 2026-08-09 | R3 perks re-derived from the real attack dump after two were designed against misread animations. |
| 2026-08-09 | Spear R5 is an instant throw rather than a chain finisher — spears have no primary chain. |
| 2026-08-09 | Snap Kick shared across the five kick-fallback families. |
| 2026-08-09 | **Phase 3 R1 implemented** for all six melee families. |
| 2026-08-09 | Flow is **Chain Flow**, not attack speed — Valheim has no per-attack speed lever. |
