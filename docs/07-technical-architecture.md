# Technical Architecture

Status: `DECIDED`, versions verified against the local install 2026-08-06.

## Stack

| Item | Decision |
|---|---|
| Loader | **BepInEx 5.4.23.x** — verified installed. *Not* the BepInEx 6 preview line; 5.4.23 is current for Valheim |
| Patching | **HarmonyX 2.9** (`0Harmony.dll`, bundled with BepInEx 5) |
| Config sync | **ServerSync** — server pushes config to clients, admin-locked. Vendored as source |
| Jötunn | **Not used.** Its value is custom prefab/asset/item registration; this mod adds no assets ([00](00-design-principles.md)). Skipping it removes a dependency and a version-coupling risk. |
| Assemblies | Valheim managed assemblies referenced from the local install, **never committed** |
| Target | `net48` via `Microsoft.NETFramework.ReferenceAssemblies` (builds on the .NET 9 SDK) |
| Distribution | Server-side required; Thunderstore packaging for client installs |

### Verified local environment (2026-08-06)

| | Path / version |
|---|---|
| Client | `C:\Program Files (x86)\Steam\steamapps\common\Valheim` — BepInEx **5.4.23.3**, build 21981559 |
| Dedicated server | `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server` — BepInEx **5.4.23.5**, build **21981590** |
| Content | Fader present → Ashlands confirmed |

**Note:** client and server BepInEx patch versions differ (.3 vs .5). Harmless — both are 5.4.23 — but
worth aligning when convenient.

**Deep North check:** the boss roster in [04](04-boss-reactions.md) lists 7 bosses per the handoff. If
the current build has shipped an 8th (Deep North) boss, that roster needs a row. Not confirmable from a
binary string scan — verify in-game during Phase 0.

## Authority model — `DECIDED` (confirmed 2026-08-06)

**Proven is server-authoritative.** A client-side counter is trivially editable, which reintroduces
exactly the problem Proven exists to solve ([03](03-proven-system.md)).

- Qualifying events are **validated and awarded server-side**. The client never writes PP.
- The client receives its own PP/rank values by RPC for display in the [trial log](06-ui-trial-log.md).
- **Old Ways Presence** is computed server-side. Encounter Presence (the max across participants,
  per [04](04-boss-reactions.md)) is resolved entirely server-side — clients never need each other's
  values, only the resolved encounter number where it affects local VFX.
- **The mod is server-required**, not optional-per-client. Confirmed acceptable for TaegukGaming.

## Storage — `DECIDED`

Per character, server-side, keyed by **player ID + character name**. A mod-owned save file alongside
the world save (not inside vanilla skills data — that's the entire point).

Must survive death, world reload, and server restart. Player-ID-keyed rather than ZDO-attached so a
character rebuild or ZDO churn can't wipe earned progress.

## Config — `DECIDED`

ServerSync'd, admin-locked, with toggles at three levels:

1. **Per category** — skill tweaks / weapon powers / boss reactions / creature reactions, each off-able
   wholesale.
2. **Per entry** — each of the 13 powers, 7 boss reactions, and 15 creature behaviors individually.
3. **Per value** — PP weights, rank thresholds, tier multipliers, DR curve, reaction chance formula.

Level 3 matters most: [03](03-proven-system.md)'s numbers are a first pass, and retuning must not
require a rebuild.

## Patch surface

Rough map. **Unverified** — needs a pass against decompiled Valheim source before it's trustworthy.

| Category | Likely hooks |
|---|---|
| [Skill tweaks](01-skill-mastery.md) | `Skills`, `Character.Damage`, `Player.UpdateStealth`, `Attack`, `ItemDrop.ItemData` durability |
| [Weapon powers](02-weapon-mastery.md) | `Attack`, `Humanoid.BlockAttack`, `Projectile`, `Character.Stagger` |
| [Proven](03-proven-system.md) | `Character.OnDeath` + damage attribution, boss aggro state |
| [Boss reactions](04-boss-reactions.md) | Per-boss AI components and ability spawners |
| [Creature reactions](05-creature-reactions.md) | `MonsterAI`, `BaseAI`, `Humanoid` |
| [Trial log](06-ui-trial-log.md) | `SkillsDialog` |

## Multiplayer

- Reaction decisions are made **once, server-side**, and replicated. A boss cannot sweep its firebreath
  on one client and not another.
- The gate/intensity split in [04](04-boss-reactions.md) means the server must know every nearby
  player's Presence — hence Presence replication above.
- Creature gating re-evaluates on **target change**; with large packs this needs a profiling pass
  before Phase 8 ships.

## Known risks

- **Sibling-mod compatibility.** BiomeLords, Lost Scrolls II, and this mod all patch
  `Character`/`Humanoid`. Needs an explicit three-mod compatibility test pass — scheduled as part of
  Phase 9 in [09-roadmap.md](09-roadmap.md).
- Damage attribution for PP awards is the single most failure-prone piece — DoTs, projectiles in
  flight, and pet/summon damage all complicate "which weapon landed the killing blow." Built first in
  Phase 1 for exactly this reason.
- Encounter Presence needs a clean definition of "in the encounter" for bosses (aggro list? radius?
  damage-dealt-recently?). Radius alone lets a distant Rank 5 player passively raise a fight.

## Repo

Git-initialized 2026-08-06, `main` branch, no remote yet.

## Decision log

| Date | Decision |
|---|---|
| 2026-08-06 | BepInEx 5.4.23.x + HarmonyX + ServerSync; **no Jötunn**. Versions verified locally. |
| 2026-08-06 | Proven is **server-authoritative**; mod is server-required, confirmed acceptable. |
| 2026-08-06 | Storage: mod-owned server-side save, keyed by player ID + character name. |
| 2026-08-06 | Config toggles at category, entry, and value level, all ServerSync'd. |
| 2026-08-06 | `net48` via NETFramework.ReferenceAssemblies so the .NET 9 SDK can build it. |
