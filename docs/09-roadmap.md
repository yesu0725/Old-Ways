# Build Roadmap

Status: `DECIDED` in shape (2026-08-06)

v1 **ships** all 13 weapon families together (handoff §4), but it is **built in phases** — not in one
go. `DECIDED 2026-08-06`. Each phase is independently testable in-game before the next starts.

## Phase 0 — Scaffold  `COMPLETE 2026-08-06`

- [x] Verify Valheim build and BepInEx version — build 21981590, BepInEx 5.4.23.x ([07](07-technical-architecture.md))
- [x] Git init, `main` branch, `.gitignore` excluding game assemblies and build output
- [x] Project structure + `Directory.Build.props` with overridable local game path
- [x] `net48` build on the .NET 9 SDK — **builds clean**
- [x] Plugin loads, logs version + client/server environment, applies Harmony
- [x] Config skeleton with the three toggle levels; all Proven weights, thresholds and the
      reaction curve config-exposed
- [x] `DeployToClient` build flag copies the dll into `BepInEx/plugins/OldWays`
- [x] **ServerSync vendored** (MIT-0, reviewed, provenance recorded) and routed through `Bind()`;
      assembly publicizer added; client version-matching on
- [x] Every build auto-deploys to the r2modman "Mod Test Profile" (dll + pdb)
- [x] Deep North boss confirmed **not to exist** — [04](04-boss-reactions.md) roster complete at 7
- [x] **Plugin confirmed loading in the test profile** — Phase 0 exit criterion met
- [x] Git repo, `.gitattributes`, `origin` set to `github.com/yesu0725/Old-Ways`

**Exit:** an empty plugin loads on the server and logs its version. ✅

Outstanding admin, not blocking Phase 1: the GitHub repo itself still needs creating and a first
push. **No LICENSE by decision** ([07](07-technical-architecture.md)) — not a todo.

## Phase 1 — Proven core + trial log  `VERIFIED IN-GAME 2026-08-08`

The foundation everything else depends on. Nothing gates on nothing.

- [x] **Damage attribution** — `Patches/KillAttributionPatch.cs`. Far less painful than feared:
      vanilla's `HitData.m_skill` already carries the skill, so no weapon inference is needed.
- [x] Storage + server authority — `Proven/ProvenStore.cs`, `Proven/ProvenRpc.cs`. Atomic writes,
      saved on world save and shutdown, keyed per world UID.
- [x] PP awards, tier multiplier, DR window, hard exclusions — `ProvenStore.AwardKill`,
      `Proven/ProgressionTier.cs`.
- [x] Rank ladder + Old Ways Presence — `Proven/ProvenRecord.cs`.
- [x] Skills-screen trial log — `UI/TrialLog.cs`, plus a `proven` console command
      (`UI/ProvenCommand.cs`) for verification.
- [x] **In-game verification, 2026-08-08.** Trial log renders on the skills screen; `raiseskill`
      moves the vanilla skill and leaves Proven untouched; a starred kill awards points.

Discovered during implementation and written up in [03](03-proven-system.md): kill reports are
necessarily client-originated (ZDO ownership) and unstarred kills award nothing.

**Verification also surfaced a live problem, since fixed:** the first starred kill awarded nothing
until the tester reset the world's boss keys. The tier gate was doing exactly what it was specified
to do — which is how we learned the specification was wrong for a shared server. Progression tier is
now per-player rather than world state; see "Player tier source" in [03](03-proven-system.md).

**Acceptance test** (run these in the test profile):

1. `proven` — trial log prints, everything Untested.
2. Kill a 1★ or 2★ creature with a tracked weapon. **Unstarred creatures award nothing** — use
   `spawn Greydwarf 1 2` for a 2★ if none are handy.
3. `proven` again — points moved on that weapon's skill only.
4. `raiseskill swords 100`, then `proven`. **Vanilla skill jumps to 100; Proven does not move.**
   This is the whole reason the system exists.
5. Open the skills screen — Proven shows on the tracked rows.
6. Restart the server, `proven` again — the value survived.

Log lines to watch for in the BepInEx console: `[Proven] server authority online`, and
`[Proven] player <id> +N <skill>` on each qualifying kill.

- PP tracking per vanilla skill, server-authoritative.
- Damage attribution → which skill gets credit for a killing blow (the riskiest piece; build it first).
- Tier multiplier from `ZoneSystem` global keys; DR window; hard exclusions.
- Rank ladder + Old Ways Presence computation and replication.
- Server-side storage, survives death/reload/restart.
- Skills-screen Proven bars and tooltips ([06](06-ui-trial-log.md)).

**Exit:** a player can earn Proven, see it in the skills screen, and it survives a server restart.
Console `raiseskill` does nothing to it. No powers exist yet.

## Phase 2 — Vertical slice: Swords  `CODE COMPLETE, UNTESTED IN-GAME`

One family end to end, to prove the whole pipeline before committing to twelve more.

- [x] `Powers/PowerGate.cs` — the shared gate every later power reuses
- [x] `Powers/SwordDuelistsGuard.cs` — sword blocks/parries at shield strength, gated on Swords R1
- [x] Level-2 config toggle + tunable multiplier
- [x] `proven_grant` admin test command so rank 1 can be reached without grinding
- [ ] **In-game verification**

**Exit:** the sword power works, correctly locked before R1 and unlocked after.

### Acceptance test

Equip a sword and **no shield** throughout.

1. `proven` — Swords should read Untested.
2. Block a Greydwarf swing with the sword. Your guard should break or drain heavily; log reads
   `Duelist's Guard inactive`. **This half matters as much as the unlock.**
3. `proven_grant Swords 150` — reaches rank 1 (needs vanilla Swords ≥ 30 too).
4. `proven` — Swords reads Blooded, Duelist's Guard UNLOCKED.
5. Block the same attack again — the guard holds, and a well-timed parry throws the attacker off.
   Log reads `sword PARRY … Duelist's Guard ACTIVE`.
6. Try a troll or something heavier to see where the ceiling sits, and tune
   `Swords - Duelist's Guard Multiplier` if 2.5 is too weak or too strong.

## Phase 3 — Remaining melee  `R1 CODE COMPLETE, UNTESTED IN-GAME`

Knives, Clubs, Axes, Polearms, Spears — plus **Unarmed**, added to the roster 2026-08-09.

Build **R1 signatures first, across all six**, then R3, then R5. Ranks are independent of each
other, and doing a whole rung at once means the shared machinery (chain interception, secondary-attack
patching) is written once rather than six times.

| Family | R1 | R3 | R5 |
|---|---|---|---|
| Knives | ✅ Vanish | Falling Fang | chain → leap |
| Clubs | ✅ Guard Crusher | Uplift | chain → rising swing |
| Axes | ✅ Hook | Cleave / Splitting Blow | chain → secondary |
| Polearms | ✅ Set Against the Charge | Whirlwind | chain → whirlwind |
| Spears | ✅ Impale | Recall | instant throw |
| Unarmed | ✅ Flow (chain-based) | Snap Kick | punch → punch → kick |

Full detail in [02](02-weapon-mastery.md).

### R1 acceptance test

Full step-by-step plan, with exact commands and what each failure mode looks like:
**[10-test-plans.md](10-test-plans.md)**.

## Phase 4 — Ranged + shields

Bows (Piercing Shot), Crossbows (Steady Aim), then Blocking's own ladder — Deflection → Immovable →
Unbreakable. **Shield bash does not exist in Valheim** and was cut; see [02](02-weapon-mastery.md).

Deflection (returning a projectile to its sender) is the most novel mechanic left in the mod, so it
goes last in this phase.

## Phase 5 — Magic

Elemental detonation combos and the per-staff Blood Magic ladder. Needs the "spell landed its full
effect on a real threat" PP event, which is the fuzziest earning condition in
[03](03-proven-system.md).

Also resolve whether `StaffGreenRoots` counts as ElementalMagic or BloodMagic — it lives in prefab
data and decides which track it ladders under.

## Phase 6 — Skill mastery tweaks

All six from [01](01-skill-mastery.md). **No Proven dependency** — this phase can be pulled forward at
any time if something needs to ship early, or done by a second contributor in parallel.

## Phase 7 — Boss reactions

All 7, plus the gate/intensity split from [04](04-boss-reactions.md). Per-boss curve overrides expected
after playtest.

## Phase 8 — Creature reactions

All 15 from [05](05-creature-reactions.md), grouped by biome so each biome is testable as a batch.
Includes the retarget-cost profiling pass.

## Phase 9 — Balance, compatibility, release

- Retune PP weights and the reaction curve against real play — the numbers in
  [03](03-proven-system.md) and [04](04-boss-reactions.md) are explicitly a first pass.
- **Turn `Verbose Logging` off.** It defaults on during development and logs every hit.
- **Three-mod compatibility test** with BiomeLords and Lost Scrolls II.
- Verify the vanilla promise: a group with no Proven players gets an unmodified game.
- Thunderstore packaging, README, changelog.

## Sequencing notes

- Phases 3–5 are the same work repeated; if Phase 2 reveals a structural problem, it's cheap to fix
  before it's replicated twelve times. That's the whole reason for the slice.
- Phase 6 is the only phase with no dependency on Phase 1.
- Phases 7–8 depend on Presence replication from Phase 1, not on any power from Phases 2–5 — they could
  move earlier if enemy-side work is more interesting to test.

## Decision log

| Date | Decision |
|---|---|
| 2026-08-06 | Phased build; Proven core + trial log first, Swords as the vertical slice. |
| 2026-08-06 | Skill tweaks (Phase 6) kept dependency-free so they can move freely. |
