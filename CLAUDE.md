# The Old Ways — Project Overview

BepInEx mod for Valheim, built for the TaegukGaming server. **Status: design phase — no code written yet.**
The authoritative source of the original concept is [old-ways-handoff.md](old-ways-handoff.md); the `docs/`
files below are the living, maintained version of it.

## One-paragraph pitch

Vanilla Valheim tracks skill as an int that goes up when you swing. The Old Ways adds a second,
un-cheatable track — **Proven** — earned only by surviving real trials, and spends it on one new
capability per weapon family that fires off an input the weapon *already* uses. In response, bosses and
2★ creatures get a "second act" on abilities they already have, but only against players carrying active
mastery. Nothing is bolted on: every effect finishes something vanilla started.

## Hard constraints (non-negotiable)

- **Vanilla assets only.** No custom models, textures, sounds, or asset bundles.
- **No new named/summonable bosses** — that is BiomeLords' lane.
- **No recruitable/levelable allies** — that is Lost Scrolls II's lane.
- **No new hotkey or cooldown-gauge UI** — the Forsaken Power slot is already spoken for.
- Every mastery power triggers off an **existing weapon input** (parry, charged attack, secondary,
  sneak-attack, block-hold, reload).

## Sibling mods (same author, must not overlap)

| Mod | Lane | Repo |
|---|---|---|
| BiomeLords | One summonable named boss per biome, horn relic, trophy → Blessing + Forsaken Power | https://github.com/yesu0725/BiomeLords |
| Lost Scrolls II | Corrupted Dvergr as levelable companion allies (chores, duels, totems) | https://github.com/yesu0725/Lost-Scrolls-II |
| **The Old Ways** | Player skill/weapon mastery + mastery-reactive enemy behavior | *(this repo)* |

## Documentation map

| Doc | Covers |
|---|---|
| [Design principles](docs/00-design-principles.md) | Lore framing, build philosophy, non-goals, scope boundaries |
| [Skill mastery tweaks](docs/01-skill-mastery.md) | Threshold payoffs read off vanilla skill values (ungated) |
| [Weapon mastery powers](docs/02-weapon-mastery.md) | One capability per weapon/magic family (Proven-gated) |
| [Proven system](docs/03-proven-system.md) | The anti-cheese counter: earning, weighting, anti-farm, persistence |
| [Boss reactions](docs/04-boss-reactions.md) | "Second act" on an existing ability, per vanilla boss |
| [Creature reactions](docs/05-creature-reactions.md) | 2★ behaviors that are latent in vanilla AI, per biome |
| [Trial log UI](docs/06-ui-trial-log.md) | Visible Proven progress surface |
| [Technical architecture](docs/07-technical-architecture.md) | BepInEx/Harmony layout, save data, multiplayer sync, config |
| [Open questions](docs/08-open-questions.md) | Pending decisions + answered log |
| [Build roadmap](docs/09-roadmap.md) | Phase plan, Phase 0 → 9 |

## Core mechanics at a glance

- **Proven** is tracked **per vanilla skill** (11 tracks), server-authoritative, permanent, on a 5-rank
  ladder. **Rank 1 = 150 PP + vanilla skill 30** unlocks that skill's power. Ranks 2–5 exist to drive
  the enemy reaction curve. Full detail: [03](docs/03-proven-system.md).
- **Old Ways Presence** = a player's highest Proven rank across all skills (0–5). One number, drives all
  enemy reactions.
- **Enemy reactions** split gate from intensity: *any one* Rank 1+ player present switches a reaction
  on; the **whole encounter then scales off the highest Presence present**. A veteran raises the fight
  for everyone in it — deliberate, see [04](docs/04-boss-reactions.md).
- **Server-authoritative and server-required.** Clients never write Proven.
- **Current status: [Phase 0](docs/09-roadmap.md) complete** — plugin builds, loads in the test
  profile, config is ServerSync'd. No gameplay systems yet. **Phase 1 (Proven core) is next.**

## Build

```bash
dotnet build src/OldWays/OldWays.csproj -c Release
```

**Every build auto-deploys to the r2modman "Mod Test Profile"** — that profile has its own BepInEx
tree, so the Steam install's plugins folder is not what a profile launch reads. Game assemblies come
from the local Steam install via [Directory.Build.props](Directory.Build.props) and are never
committed.

## Maintenance rule (read this before editing anything)

These docs are the working reference, not a one-time snapshot.

1. **Any change that touches a category above goes into that category's doc in the same session** —
   a decision made, a value tuned, a patch written, a mechanic cut.
2. **A new part with no doc gets a new doc** in `docs/`, numbered next in sequence, and a row added to
   the map table above.
3. **When an open question is answered**, move it out of [08-open-questions.md](docs/08-open-questions.md)
   into the relevant doc as a decision, and log it in that file's decision log with the date.
4. **Never edit [old-ways-handoff.md](old-ways-handoff.md)** — it is the frozen origin document. All
   evolution happens in `docs/`.
5. Mark anything not yet decided as `**TBD**` inline rather than inventing a value silently.

## Status legend used across docs

- `DECIDED` — locked, safe to implement against
- `PROPOSED` — Claude's suggestion, needs author sign-off
- `TBD` — genuinely undecided, blocking or near-blocking
- `IMPLEMENTED` — code exists and is verified in-game
