# Open Questions

Questions blocking or shaping implementation. When one is answered, it moves into the relevant doc as a
decision and is logged there, then moves to Answered below.

## Open

### Q19 — Defining "in the encounter"
Encounter Presence takes the max across players *in the encounter*, but that set needs a definition:
boss aggro list, a radius, or recent damage dealt. A radius alone lets a distant Rank 5 player passively
raise a fight they aren't part of. Proposed: on the boss's aggro list **or** dealt/taken damage in the
last 30s.
→ [04](04-boss-reactions.md), [07](07-technical-architecture.md).

### Q12 — Skill tweak tier behavior
Do the [01](01-skill-mastery.md) values **snap** at 30/50/75/100, or interpolate smoothly between them?
Proposed: snap, so crossing a tier is noticeable.
→ [01](01-skill-mastery.md).

### Q13 — Axe execute threshold
The execute needs a hard, readable condition or it reads as a random one-shot. Proposed: target
staggered **and** below 15% HP.
→ [02](02-weapon-mastery.md).

### Q14 — Power chaining cooldowns
Should powers have an internal cooldown (e.g. sword parry-crit looping)? Proposed: no — stamina cost
and input precision are the rate limit. Deferred to playtest.
→ [02](02-weapon-mastery.md).

### Q15 — Proven bar visible at Rank 0?
Visible from the start (shows the path) or hidden until first PP (discovery)? Proposed: visible —
Proven is explicitly not mysterious.
→ [06](06-ui-trial-log.md).

### Q16 — 3★+ creatures
If another mod adds 3★+, do creature reactions apply? Proposed: yes, treat as 2★.
→ [05](05-creature-reactions.md).

*Q12–Q16 all have proposals on record and are non-blocking — they can ride along with their phase.
Q19 blocks Phase 7.*

### Q6 (reopened, then held) — Proven granularity
Per-vanilla-skill vs. per-weapon-family was re-examined 2026-08-06. Finding: the two are **identical
for 10 of 11 tracks** — only shields differ (one Blocking skill carrying three powers vs. three
independent tracks). Per-family additionally requires a mod-defined family enum, a weapon→family
mapping table, and three sub-bars crammed into one skills-screen row. **Held at per-vanilla-skill.**
→ [03](03-proven-system.md).

## Answered

| # | Question | Answer | Date |
|---|---|---|---|
| Q1 | Reaction curve intensity | **Steady curve** scaling with mastery. Intensity revised same day: the **whole encounter scales off the highest-mastery player present**, not per-individual → [04](04-boss-reactions.md) | 2026-08-06 |
| Q11 | Server-required acceptable? | **Yes** — Proven stays server-authoritative → [07](07-technical-architecture.md) | 2026-08-06 |
| Q17 | Vendor ServerSync? | **Yes** — vendored, reviewed, wired through the `Bind()` seam → [PROVENANCE](../src/OldWays/Util/ServerSync/PROVENANCE.md) | 2026-08-06 |
| Q18 | Deep North boss | **Does not exist.** Roster complete at 7; Phase 7 fully scoped → [04](04-boss-reactions.md) | 2026-08-06 |
| Q20 | LICENSE file? | **None** — matches all seven existing repos; all rights reserved → [07](07-technical-architecture.md) | 2026-08-06 |
| — | Valheim / BepInEx versions | Verified locally: build 21981590, BepInEx 5.4.23.x → [07](07-technical-architecture.md) | 2026-08-06 |
| Q2 | Group gating | Reactions switch on when **any one** Proven player is present → [04](04-boss-reactions.md) | 2026-08-06 |
| Q3 | Proven thresholds and weights | Elaborated in full: 5-rank ladder, base weights, tier multiplier, DR curve → [03](03-proven-system.md) | 2026-08-06 |
| Q4 | Trial log UI form | **Skills screen** integration → [06](06-ui-trial-log.md) | 2026-08-06 |
| Q5 | Technical stack | BepInEx 5.4.x + HarmonyX + ServerSync, no Jötunn, server-authoritative → [07](07-technical-architecture.md) | 2026-08-06 |
| Q6 | Proven granularity | **Per vanilla skill** → 11 tracks; shields stage across Blocking R1/R2/R3 → [03](03-proven-system.md) | 2026-08-06 |
| Q7 | Proven permanence | **Permanent**, no decay → [03](03-proven-system.md) | 2026-08-06 |
| Q8 | Skill tweak thresholds | **Scale across all four tiers** → [01](01-skill-mastery.md) | 2026-08-06 |
| Q9 | Chain Bolt and structures | **Players only**, never structures → [04](04-boss-reactions.md) | 2026-08-06 |
| Q10 | Build order | Phased, Proven core first, Swords as vertical slice → [09](09-roadmap.md) | 2026-08-06 |
