# Test Plans

Step-by-step in-game verification for each phase. Written to be followed literally — every command
is exact, and every check says what "working" looks like *and* what failure looks like.

A new plan is added per phase. Results get recorded here so a re-test after a change is cheap.

## Before anything

### Choose a setup

| Setup | Use when |
|---|---|
| **Client + local dedicated server** | default. This is the only setup that exercises the real client/server split — kill reports, Proven sync and the identity handshake actually cross the wire |
| **r2modman "Mod Test Profile"** alone | isolating a problem from the other 20 mods |

Every build deploys to both, plus the Gale **HB Test** client profile.

### Client + dedicated server

1. **Restart the dedicated server after every build.** A running server keeps the old dll loaded.
   This is the single easiest mistake to make: the client gets the new build, the server does not,
   and version matching then refuses the connection — which at least fails loudly.
2. Start it: `start_headless_server.bat` in the dedicated server folder.
3. Launch the client through the **Gale "HB Test"** profile and connect.
4. **Admin commands need your Steam ID in `adminlist.txt`**
   (`%AppData%\..\LocalLow\IronGate\Valheim\adminlist.txt`). Two IDs are already listed. If
   `proven_grant` replies *"Refused: you are not a server admin"*, that file is why.

Two things live **on the server**, not the client:

- **Config.** It is ServerSync'd and admin-locked, so the server's values win — editing the client's
  `OldWays.cfg` does nothing. Edit the server's copy and restart it.
- **The Proven store**, at `BepInEx/config/OldWays/proven_<worldUID>.dat` on the server. Delete that
  file for a truly clean slate.

### Then, either setup

Load in and press **F5** for the console:

```
devcommands
```

Cheats must be on — `proven_grant`, `proven_set` and `proven_reset` are all cheat-flagged.

Confirm the mod is live:

```
proven
```

You should see the trial log with all 12 skills. If the command is not recognised, the plugin did
not load — check the log before going further.

**Keep the console open while testing.** Verbose logging is on by default and every power announces
itself. The log lines matter as much as what you see on screen: several effects are subtle, and the
log is what distinguishes "not working" from "not triggering".

Logs:
- client — `%AppData%\com.kesomannen.gale\valheim\profiles\HB Test\BepInEx\LogOutput.log`
- server — `<dedicated server>\BepInEx\LogOutput.log`

On a dedicated server the two halves log different things. Anything prefixed `[Proven]`, `[Admin]`
or `[AdminAuth]` about awards and grants happens **server-side**; power effects (`[Hook]`,
`[Impale]`, `[Flow]`…) are **client-side**. If a power seems dead, check the client log; if Proven
is not moving, check the server's.

---

## Phase 3 — R1 melee signatures

### Step 0 — the locked control (do this first)

This half matters as much as the unlock. Before granting anything:

```
proven_reset me
```

Then equip a **mace** and hit anything. Nothing unusual should happen and **no `[GuardCrusher]` line
should appear**. If a power fires before you have earned it, the gate is broken and nothing below is
meaningful.

### Step 1 — unlock everything

Each power needs Proven rank 1 **and** vanilla skill 30.

```
raiseskill clubs 30
```
```
raiseskill axes 30
```
```
raiseskill knives 30
```
```
raiseskill polearms 30
```
```
raiseskill spears 30
```
```
raiseskill unarmed 30
```

Then grant Proven for each:

```
proven_grant Clubs 150
```
```
proven_grant Axes 150
```
```
proven_grant Knives 150
```
```
proven_grant Polearms 150
```
```
proven_grant Spears 150
```
```
proven_grant Unarmed 150
```

Confirm with `proven` — each of those six should read **Blooded** with its power **UNLOCKED**.

---

### Test 1 — Hook (Axes)

**Easiest to see. Start here.**

```
spawn Greydwarf 1
```

Equip any axe. Stand a few metres back and hit it.

| | |
|---|---|
| ✅ Expect | The Greydwarf is dragged **toward you** instead of knocked away |
| 📋 Log | `[Hook] pulling 'Greydwarf(Clone)' (force N)` |
| ⚠️ If the log appears but nothing visibly moves | The force is too small to see. Raise `Axes - Hook Pull Multiplier` to 2 or 3 and retry — tell me the value that reads well |
| ❌ If no log line | The gate failed. Run `proven` and check Axes says Blooded |

### Test 2 — Impale (Spears)

```
spawn Greydwarf 1
```

Equip a spear, back off ~10 m, and **throw it** (secondary attack).

| | |
|---|---|
| ✅ Expect | On hit, the Greydwarf freezes in place for ~3 s — it cannot walk toward you, though it may still turn or swing if you are in reach |
| 📋 Log | `[Impale] pinned 'Greydwarf(Clone)' for 3s` |
| ⚠️ Also test | A **melee poke** with the spear should NOT pin. Only the throw counts |

Then the boss exemption:

```
spawn Eikthyr 1
```

Throw a spear at it. It should **not** be pinned, and the log should say
`'Eikthyr(Clone)' is a boss — not pinned, by design`. Kill or `removedrops` afterwards.

### Test 3 — Set Against the Charge (Polearms)

```
spawn Boar 1
```

Equip an **atgeir**, hold block, and let the boar charge into your guard.

| | |
|---|---|
| ✅ Expect | The boar takes pierce damage and is thrown backwards |
| 📋 Log | `[SetAgainstCharge] impaled 'Boar(Clone)' at N m/s for N pierce` |
| ⚠️ Most likely failure | `'Boar(Clone)' closed at 2.3 m/s, needs 4.0 — not a charge`. **This line is the fix.** Tell me the number you see and I will set the threshold from real data instead of my guess |

Also confirm the negative case: let a boar **walk** into your guard. It should do nothing.

If boars are too slow, try `spawn Fenring 1` or `spawn Lox 1` — both charge harder.

### Test 4 — Vanish (Knives)

```
spawn Greydwarf 3
```

Back away until they lose you, **crouch** to sneak, approach one from behind, and kill it with a
knife backstab. It must be a **sneak kill** — a fair fight will not trigger this.

| | |
|---|---|
| ✅ Expect | The other two stop chasing and return to wandering |
| 📋 Log | `[Vanish] sneak kill on 'Greydwarf(Clone)' — 2 creature(s) lost track of you` |
| ⚠️ If it says 0 creatures | They were outside the 20 m radius, or you did not own their AI. Try again with them closer |
| ❌ If no log at all | The kill was not registered as a sneak attack. You need the backstab bonus — approach fully undetected from behind |

A weak knife helps here: you want the *sneak* kill to land, not a normal swing to finish it.

### Test 5 — Flow (Unarmed)

Unequip everything, including your shield.

```
spawn Greydwarf 1
```

Punch it repeatedly, leaving roughly **1–2 seconds between swings** — long enough that vanilla would
normally reset your combo.

| | |
|---|---|
| ✅ Expect | The punch combo keeps cycling instead of restarting from the first punch |
| 📋 Log | `[Flow] chain held open.` each time it kicks in |
| ⚠️ Then get hit | The next line should be `[Flow] streak broken — you were hit.` and the combo should reset |

This one is genuinely subtle on screen — **the log is the real verdict here**.

### Test 6 — Guard Crusher (Clubs)

**Expect this one to be awkward, and that is useful information.**

Guard Crusher only does anything when a creature is *actually blocking*, and it is not certain how
often vanilla creatures block at all. Best candidates:

```
spawn Goblin 1
```
```
spawn Draugr 1
```

Equip a mace and attack them repeatedly, especially head-on.

| | |
|---|---|
| ✅ Expect | `[GuardCrusher] broke 'X' guard` whenever they would have blocked |
| ⚠️ If the line never appears | Not necessarily a bug — it may mean vanilla creatures rarely block. **Tell me either way**, because it decides whether Guard Crusher is worth keeping as the Clubs power, or whether it only becomes real once the shield-raising 2★ Draugr in [05](05-creature-reactions.md) lands |

---

### Test 7 — Visual effects

Do this **first**, before the power tests — it takes one command and tells you whether the guessed
prefab names resolved:

```
oldways_dumpeffects
```

Writes every vanilla effect prefab to `BepInEx/config/OldWays/effect_prefabs.txt` **on the client**.

Then watch for this in the client log while testing the powers above:

| | |
|---|---|
| ✅ Working | `[Effects] 'vfx_...' resolved for candidates [...]` — that effect will play |
| ⚠️ Guessed wrong | `[Effects] no prefab found among [a, b, c]` — harmless, just invisible. Pick a real name from the dump and put it in the `5 - Visual Effects` config section |

Each entry takes a **comma-separated list**; the first name that exists is used, so you can leave
several candidates in place.

Also confirm the rank-up moment: `proven_grant Swords 150` on a character that does not have it
should print *"Your hands remember. Swords — Blooded."* top-left, with an effect.

**Send me the `no prefab found` lines** (or the dump itself) and I will set verified names as the
defaults.

### Cleaning up

To put a character back to untested:

```
proven_reset me
```

To silence the log spam once you are done:
set `Verbose Logging` to `false` in `BepInEx/config/OldWays/yesu0725.oldways.cfg`, or via
ConfigurationManager in-game (F1).

### What to report back

For each test: **the log line you saw** (or that you saw none), plus anything that looked wrong on
screen. The two numbers I most want are the **boar's closing speed** and whether **Hook's pull is
visible at 1.0** — both are guesses in the config right now.

## Results log

| Date | Phase | Outcome |
|---|---|---|
| 2026-08-08 | Phase 1 | Passed. `raiseskill` confirmed unable to move Proven. Tier gate found too strict — fixed. |
| 2026-08-09 | Phase 2 | Duelist's Guard confirmed working in-game. |
| 2026-08-09 | Infrastructure | Passed. Client + dedicated server both load; version match OK; `proven_reset` works against the real admin path. |
| 2026-08-09 | Phase 1 (networked) | Passed. Kill reports, Proven sync and the identity handshake verified across a real client/server boundary for the first time. |
| | Phase 3 R1 | *pending* |
