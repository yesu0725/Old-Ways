# The Old Ways

A BepInEx mod for Valheim. Built for the TaegukGaming server.

Vanilla tracks skill as a number that goes up when you swing. The Old Ways adds a second track —
**Proven** — earned only by surviving real trials and untouchable by console commands, and spends it on
one new capability per weapon family that fires off an input the weapon *already* uses. In response,
bosses and 2★ creatures get a second act on abilities they already have.

No custom models, textures, or asset bundles. Every effect finishes something vanilla started.

> **Status: Phase 0 — scaffolding.** The plugin loads and binds config. No gameplay systems are
> implemented yet. See [docs/09-roadmap.md](docs/09-roadmap.md).

## Server-required

Proven is **server-authoritative** — a client-side counter would be trivially editable, which defeats
the entire point. Every player on the server needs the mod installed.

## Group play

Enemy reactions switch on when **any one** player in an encounter has reached Rank 1, and the whole
encounter scales off the **highest-mastery player present**. Bringing a newcomer to their first boss
means they face your version of that fight, not theirs. This is deliberate — but worth knowing before
you invite someone along.

## Building

Requires the .NET SDK and a local Valheim install.

```bash
dotnet build src/OldWays/OldWays.csproj -c Release
```

Output lands in `src/OldWays/bin/Release/OldWays.dll`. Copy it to `BepInEx/plugins/OldWays/`, or build
with `-p:DeployToClient=true` to have it copied automatically.

If Valheim isn't at the default Steam path, create `Directory.Build.props.user` (gitignored):

```xml
<Project>
  <PropertyGroup>
    <ValheimDir>D:\SteamLibrary\steamapps\common\Valheim</ValheimDir>
  </PropertyGroup>
</Project>
```

Game assemblies are referenced from that install and are **never committed** — they're copyrighted game
files.

### Verified environment

| | Version |
|---|---|
| Valheim (dedicated server) | build 21981590 |
| BepInEx | 5.4.23.x |
| Harmony | 2.9 |
| Target framework | net48 |

## Documentation

Design lives in [CLAUDE.md](CLAUDE.md) and [docs/](docs/).
[old-ways-handoff.md](old-ways-handoff.md) is the frozen original concept document.

## Related mods

Same author, deliberately non-overlapping lanes:

- [BiomeLords](https://github.com/yesu0725/BiomeLords) — summonable named biome bosses
- [Lost Scrolls II](https://github.com/yesu0725/Lost-Scrolls-II) — recruitable Dvergr companions
