# Design Principles

Status: `DECIDED` (from handoff §1, §2, §9)

## Lore framing

The old warriors, hunters, and builders of Midgard knew techniques that have been lost — not through
instruction, but through **being proven in trial**. A player's body does not remember a technique
because they practiced it (vanilla skill XP already represents practice); it remembers because they
were *tested* by something real and came out ahead.

This is the in-fiction justification for two things:
- The two-layer gate (vanilla skill as soft prerequisite, Proven as the real unlock) — see
  [Proven system](03-proven-system.md).
- Why enemies sharpen in response — the land is reacting to a practitioner of the Old Ways, not
  spawning a new threat out of nowhere. See [Boss reactions](04-boss-reactions.md) and
  [Creature reactions](05-creature-reactions.md).

## Build philosophy

**Every effect should read as "finishing" something vanilla already started** — an existing mechanic,
animation, status effect, or AI behavior — not a bolted-on new system.

Practical test for any proposed feature: *can a player who doesn't read the changelog describe what
happened using vanilla vocabulary?* If the answer needs a new noun, it's out of scope.

## Two systems, kept separate

1. **Skill mastery tweaks** ([01](01-skill-mastery.md)) — small qualitative payoffs read off *existing*
   vanilla skill values. Flavor/QoL. **Not** gated behind Proven.
2. **Weapon/shield/magic mastery powers** ([02](02-weapon-mastery.md)) — one new capability per weapon
   family, gated behind **Proven** ([03](03-proven-system.md)), not vanilla skill level alone.

Keeping these separate matters: system 1 can ship and be tuned independently of the Proven
infrastructure, and a player who never engages with Proven still gets a better-feeling game.

## Explicit non-goals

- No new named/summonable unique bosses. *(BiomeLords)*
- No recruitable/levelable ally creatures. *(Lost Scrolls II)*
- No new hotkey-bound "power" — the Forsaken Power slot's role is already taken. *(BiomeLords)*
- No custom models/textures/asset bundles — **vanilla assets only**.
- No flat stat inflation (raw +damage, +health) as a reward. Rewards are *qualitative*.
- No permanent global difficulty change — enemy reactions are mastery-reactive layers only.

## Decision log

| Date | Decision |
|---|---|
| (handoff, pre-2026-08-06) | v1 ships **all** weapon families at once, not a staggered rollout. |
| (handoff, pre-2026-08-06) | Proven is **visible** to the player, not hidden/mysterious. |
| (handoff, pre-2026-08-06) | Enemy reactions are gated to players with active mastery, not global. |
