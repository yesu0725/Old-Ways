# Trial Log UI

Status: `DECIDED` — skills-screen integration. Layout details `PROPOSED`.

## Decision

**The trial log lives in the existing skills screen.** `DECIDED 2026-08-06` — no dedicated panel, no
new keybind, no new UI surface. This is the strongest fit for the "vanilla finishes itself" philosophy
in [00](00-design-principles.md), and it sits exactly where a player already looks for skill info.

Rejected: a dedicated in-game panel. It would need a key or menu entry, brushing against the "no new
hotkey" constraint, and it would read as bolted on.

## Layout (`PROPOSED`)

Each vanilla skill row in `SkillsDialog` gains a **second, thinner bar beneath the existing XP bar** —
the Proven bar — plus a rank pip or numeral. Eleven skills have one; the rest are unchanged.

Hovering the row extends the existing tooltip with:

```
Blocking — Tempered (Rank 2)
  Proven  512 / 800

  Unlocked:  Unbroken Guard   — a perfect parry negates your stagger and punishes the attacker
  Unlocked:  Shield Bash      — cheaper, and staggers what bashes normally can't
  Rank 3:    Brace            — hold block to build a buffer against a guard-breaking hit

  Trials:  block a boss's heavy attack without breaking guard · land a parry
           on a real threat · kill in a boss fight with a shield raised
```

The tooltip is doing three jobs: current standing, what's unlocked vs. next, and **what to go do**.
That last part matters most — Proven is only fair if the player knows how to earn it.

## Constraints

- **Never shows live power state.** The Proven bar shows progress toward unlocking; it must not become
  a cooldown or charge meter — that includes the Shields brace buffer in [02](02-weapon-mastery.md),
  which stays diegetic.
- Vanilla UI assets and styling only. No custom sprites.
- Skills with no Proven track render exactly as vanilla — no empty bars.
- Must render correctly on a dedicated-server client with values pushed from the server
  ([07](07-technical-architecture.md)), including before the first sync arrives (show "—", not 0).

## Display format

**Numeric with rank name** — `512 / 800 · Tempered`. `PROPOSED`. Rank names alone ("Blooded",
"Tempered") are more atmospheric, but the player needs to see whether they're 10 PP or 300 PP from the
next unlock or the anti-farm rules feel arbitrary. Rank name carries the flavor, the number carries the
information.

## Notifications

**On rank-up only** — one line in the existing top-left message area ("*Your hands remember. Blocking —
Tempered.*"), plus the vanilla skill-up sound. **No per-event toast** — with kill-weighted PP that
would spam constantly. `PROPOSED`.

## Open items

- Whether the Proven bar is visible for a skill at Rank 0 (shows the path) or hidden until the first PP
  is earned (discovery). **PROPOSED:** visible from the start — Proven is explicitly not mysterious.
- Exact tooltip trial wording per skill.

## Decision log

| Date | Decision |
|---|---|
| (handoff) | Proven is visible, not hidden. |
| 2026-08-06 | **Skills-screen integration**, not a dedicated panel. |
| 2026-08-06 | Numeric + rank name; rank-up notification only. |
