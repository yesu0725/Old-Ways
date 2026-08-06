# Vendored: ServerSync

`ConfigSync.cs` is third-party code, vendored unmodified. **Do not edit it** — that would make
updating painful. Everything The Old Ways needs sits behind
[`OldWaysConfig`](../../Config/OldWaysConfig.cs).

| | |
|---|---|
| Upstream | https://github.com/blaxxun-boop/ServerSync |
| File | `ConfigSync.cs` |
| Retrieved | 2026-08-06 from `master` |
| SHA-256 | `0C6623EF756C1BF4B762D91BB42979BC6A7F123EFD313EE516B0F85CAEB4C445` |
| Size | 44,941 bytes / 1220 lines |
| License | **MIT-0** (public-domain-equivalent, no attribution required) |

ServerSync is distributed as source to vendor rather than as a NuGet package, which is why it lives
in the repo instead of in `PackageReference`.

## What it does

Server pushes config values to every connected client and can lock them so only admins may change
them. Also does mod version-matching on connect, refusing clients whose version doesn't match — which
is exactly what a server-required mod needs ([docs/07](../../../../docs/07-technical-architecture.md)).

## Review notes (2026-08-06)

Read end to end before committing:

- No external network access. All traffic rides Valheim's own `ZRoutedRpc`/`ZRpc` peer channel.
- No file I/O beyond BepInEx's `ConfigFile.Save()`. No process spawning, no external assembly loading.
- Extensive `AccessTools` reflection into Valheim private members — **this is why the build needs
  publicized assemblies**. Removing the publicizer will break the build here first.
- It deserializes types named in the incoming package (`Type.GetType` + `GetUninitializedObject`).
  Values are only assigned where the received type matches the expected config type, and a client
  only accepts this from the server it connected to. Bounded by "trust the server you join," which
  was already true of any modded server.

## Requirements it imposes on the build

- Publicized `assembly_valheim` (private field access, e.g. `FejdStartup.m_connectionFailedPanel`)
- `LangVersion` 12+ — uses file-scoped namespaces and primary constructors
- Nullable annotation context
- `JetBrains.Annotations` for `[PublicAPI]` / `[UsedImplicitly]`

## Updating

Re-download `ConfigSync.cs` from upstream, overwrite, update the hash above, rebuild. Nothing else
should need to change.
