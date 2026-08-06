# Build Roadmap

Status: `DECIDED` in shape (2026-08-06)

v1 **ships** all 13 weapon families together (handoff §4), but it is **built in phases** — not in one
go. `DECIDED 2026-08-06`. Each phase is independently testable in-game before the next starts.

## Phase 0 — Scaffold  `IN PROGRESS`

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
- [ ] Confirm the plugin actually loads in-game (launch the test profile, check the BepInEx log
      for the version line)
- [ ] Git remote

**Exit:** an empty plugin loads on the server and logs its version.

## Phase 1 — Proven core + trial log

The foundation everything else depends on. Nothing gates on nothing.

- PP tracking per vanilla skill, server-authoritative.
- Damage attribution → which skill gets credit for a killing blow (the riskiest piece; build it first).
- Tier multiplier from `ZoneSystem` global keys; DR window; hard exclusions.
- Rank ladder + Old Ways Presence computation and replication.
- Server-side storage, survives death/reload/restart.
- Skills-screen Proven bars and tooltips ([06](06-ui-trial-log.md)).

**Exit:** a player can earn Proven, see it in the skills screen, and it survives a server restart.
Console `raiseskill` does nothing to it. No powers exist yet.

## Phase 2 — Vertical slice: Swords

One family end to end, to prove the whole pipeline before committing to twelve more.

- Swords: parry → guaranteed critical stagger, gated on Swords R1.
- Validates: gate read, trigger off an existing input, multiplayer replication, config toggle.

**Exit:** the sword power works on the live server, correctly locked before R1 and unlocked after.

## Phase 3 — Remaining melee

Knives, Clubs, Axes, Polearms, Spears. Same pattern as Phase 2, now in parallel.

## Phase 4 — Ranged + shields

Bows, Crossbows, then Blocking's three staged unlocks (R1 parry → R2 bash → R3 brace). Shields last in
this phase — the brace buffer is the most novel mechanic in the mod and the only one with a charge-up.

## Phase 5 — Magic

Elemental (no-Eitr chance) and Blood (capped lifesteal). Needs the "spell landed its full effect on a
real threat" PP event, which is the fuzziest earning condition in [03](03-proven-system.md).

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
