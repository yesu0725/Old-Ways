# Technical Architecture

Status: `DECIDED`, versions verified against the local install 2026-08-06.

## Stack

| Item | Decision |
|---|---|
| Loader | **BepInEx 5.4.23.x** — verified installed. *Not* the BepInEx 6 preview line; 5.4.23 is current for Valheim |
| Patching | **HarmonyX 2.9** (`0Harmony.dll`, bundled with BepInEx 5) |
| Config sync | **ServerSync** — vendored 2026-08-06 at `src/OldWays/Util/ServerSync/ConfigSync.cs`. See its [PROVENANCE.md](../src/OldWays/Util/ServerSync/PROVENANCE.md) |
| Publicizer | **BepInEx.AssemblyPublicizer.MSBuild** — required; ServerSync reflects into Valheim privates |
| Jötunn | **Not used.** Its value is custom prefab/asset/item registration; this mod adds no assets ([00](00-design-principles.md)). Skipping it removes a dependency and a version-coupling risk. |
| Assemblies | Valheim managed assemblies referenced from the local install, **never committed** |
| Target | `net48` via `Microsoft.NETFramework.ReferenceAssemblies` (builds on the .NET 9 SDK) |
| Distribution | Server-side required; Thunderstore packaging for client installs |

### Test deployment — `IMPLEMENTED`

**Every build copies itself into the r2modman "Mod Test Profile" automatically.** The `.pdb` goes too,
so BepInEx stack traces carry line numbers.

```
%AppData%\r2modmanPlus-local\Valheim\profiles\Mod Test Profile\BepInEx\plugins\OldWays\
```

r2modman keeps a **separate BepInEx tree per profile** — dropping the dll into the Steam install would
not be picked up by a profile launch. That's why this, not the Steam folder, is the default target.
`-p:DeployToTestProfile=false` skips it; `-p:DeployToClient=true` additionally copies into the raw
Steam install.

Already installed in that profile and useful here: **ConfigurationManager** (inspect the synced config
in-game) and **server_devcommands** (`raiseskill` — the exact thing Proven must be immune to, per
[03](03-proven-system.md)).

### Verified local environment (2026-08-06)

| | Path / version |
|---|---|
| Client | `C:\Program Files (x86)\Steam\steamapps\common\Valheim` — BepInEx **5.4.23.3**, build 21981559 |
| Dedicated server | `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server` — BepInEx **5.4.23.5**, build **21981590** |
| Content | Fader present → Ashlands confirmed |

**Note:** client and server BepInEx patch versions differ (.3 vs .5). Harmless — both are 5.4.23 — but
worth aligning when convenient.

**Deep North:** confirmed 2026-08-06 that no 8th boss exists on this build. The
[04](04-boss-reactions.md) roster of 7 is complete.

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

## Config — `IMPLEMENTED` (Phase 0)

ServerSync'd, admin-locked, with toggles at three levels. Every entry routes through a single
`Bind()` seam in `src/OldWays/Config/OldWaysConfig.cs` that registers it with ServerSync — nothing
in this mod should call `cfg.Bind` directly.

Client version-matching is on (`ModRequired`, `MinimumRequiredVersion` = current version), so a client
with a mismatched build is refused at connect rather than silently desyncing. That is the mechanism
that makes "server-required" real rather than aspirational.

**Build gotcha:** do not add a `JetBrains.Annotations` package — ServerSync's `[PublicAPI]` /
`[UsedImplicitly]` resolve from `UnityEngine.CoreModule`, which already ships them. Adding the package
produces CS0433 ambiguity errors. `Unity.TextMeshPro` must be referenced (ServerSync's connect-error
panel touches `TMP_Text`).

Toggle levels:

1. **Per category** — skill tweaks / weapon powers / boss reactions / creature reactions, each off-able
   wholesale.
2. **Per entry** — each of the 13 powers, 7 boss reactions, and 15 creature behaviors individually.
3. **Per value** — PP weights, rank thresholds, tier multipliers, DR curve, reaction chance formula.

Level 3 matters most: [03](03-proven-system.md)'s numbers are a first pass, and retuning must not
require a rebuild.

## Patch surface

Phase 1 rows are **verified against the shipped assembly**; later rows are still a rough map.

| Category | Hooks | Status |
|---|---|---|
| [Proven](03-proven-system.md) | `Character.Damage(HitData)` postfix, `Character.OnDeath` prefix, `ZNet.Awake`, `ZNet.SaveWorldAndPlayerProfiles`, `ZNet.Shutdown` | verified |
| [Trial log](06-ui-trial-log.md) | `SkillsDialog.Setup`, `Terminal.InitTerminal` | verified |
| [Skill tweaks](01-skill-mastery.md) | `Skills`, `Character.Damage`, `Player.UpdateStealth`, `Attack`, durability | unverified |
| [Weapon powers](02-weapon-mastery.md) | `Attack`, `Humanoid.BlockAttack`, `Projectile`, stagger | unverified |
| [Boss reactions](04-boss-reactions.md) | Per-boss AI components and ability spawners | unverified |
| [Creature reactions](05-creature-reactions.md) | `MonsterAI`, `BaseAI`, `Humanoid` | unverified |

### Verified API facts worth not re-deriving

- `Skills.SkillType` combat values are exactly our 11 tracks: Swords 1, Knives 2, Clubs 3,
  Polearms 4, Spears 5, Blocking 6, Axes 7, Bows 8, ElementalMagic 9, BloodMagic 10, Crossbows 14.
- `HitData.m_skill` carries the crediting skill — attribution needs no weapon inference.
- `HitData.m_backstabBonus > 1` marks a sneak attack.
- `Humanoid` does **not** override `Character.Damage` or `Character.OnDeath`, so patching the base
  catches all creatures. `Player` *does* override `OnDeath`.
- `Character.GetLevel()` returns 1 for unstarred, 2 for 1★, 3 for 2★.
- `GlobalKeys` enum only covers the first five boss defeats; `defeated_queen` / `defeated_fader`
  must be read with the string overload of `GetGlobalKey`.
- Useful members: `Character.IsBoss()`, `IsTamed()`, `IsPlayer()`, `GetAllCharacters()`,
  `ZNet.GetWorldUID()`, `SkillsDialog.m_elements` (parallel to `GetSkillList()`).

A Mono.Cecil-based inspector was used to confirm all of the above against
`assembly_valheim.dll` rather than working from memory.

## Multiplayer

- Reaction decisions are made **once, server-side**, and replicated. A boss cannot sweep its firebreath
  on one client and not another.
- The gate/intensity split in [04](04-boss-reactions.md) means the server must know every nearby
  player's Presence — hence Presence replication above.
- Creature gating re-evaluates on **target change**; with large packs this needs a profiling pass
  before Phase 8 ships.

## Admin test command — a deliberate hole, fenced

`proven_grant <skill> <points>` exists so a power can be verified without the ~15 starred kills that
rank 1 legitimately costs. It is the only path that writes Proven without earning it, so it is
fenced on every side:

| Fence | Why |
|---|---|
| `isCheat: true` | disabled entirely on servers that disallow cheats |
| `onlyAdmin: true` (client) | hides it from ordinary players — **convenience, not security** |
| **Server re-verifies the sender is on the admin list** | the real boundary. A modified client can call the RPC directly, so the server never trusts the client-side flag |
| Applied server-side through the same store as a real award | client and server cannot desync |
| Every use logged at **warning** level with the granting peer and player id | grants are auditable after the fact |
| Grants **points, not ranks** | the rank ladder and thresholds stay the single source of truth |

Refusals are logged too, so an attempt to bypass the client-side flag leaves a trace.

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

Git-initialized 2026-08-06, `main` branch. `origin` → `github.com/yesu0725/Old-Ways`.

**No LICENSE file.** `DECIDED 2026-08-06` — matches all seven of the author's existing public repos,
including BiomeLords and Lost Scrolls II. Default copyright applies: all rights reserved. The author
may distribute freely (they own it); third parties may not redistribute, fork-and-maintain, or bundle
it into modpacks without permission.

Two things this does **not** block:
- Thunderstore packaging — its `manifest.json` has no license field, so Phase 9 is unaffected.
- The vendored ServerSync — MIT-0 imposes no obligations at all, so there is no conflict with an
  otherwise-unlicensed project.

Don't re-raise this without a reason; it's a settled preference, not an oversight.

## Decision log

| Date | Decision |
|---|---|
| 2026-08-06 | BepInEx 5.4.23.x + HarmonyX + ServerSync; **no Jötunn**. Versions verified locally. |
| 2026-08-06 | Proven is **server-authoritative**; mod is server-required, confirmed acceptable. |
| 2026-08-06 | Storage: mod-owned server-side save, keyed by player ID + character name. |
| 2026-08-06 | Config toggles at category, entry, and value level, all ServerSync'd. |
| 2026-08-06 | `net48` via NETFramework.ReferenceAssemblies so the .NET 9 SDK can build it. |
| 2026-08-06 | ServerSync **vendored** (MIT-0), reviewed, and wired through the `Bind()` seam. Version-matching on. |
| 2026-08-06 | Assembly publicizer added — a hard requirement of ServerSync, and our own patches will want it. |
| 2026-08-06 | Every build auto-deploys to the r2modman "Mod Test Profile" (dll + pdb). |
