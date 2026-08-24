using System.Collections.Generic;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// Spawns vanilla effect prefabs so a Proven power is visible when it fires.
    ///
    /// **Vanilla assets only** (CLAUDE.md): nothing here creates art. Every effect is an existing
    /// prefab looked up from ZNetScene by name.
    ///
    /// Effect names live in prefab data, not in the assembly, so they cannot be verified statically —
    /// the same problem that produced the Riposte and shield-bash mistakes. Two defences:
    ///
    ///  1. Each power configures a **comma-separated candidate list**; the first name that actually
    ///     exists wins. A wrong guess costs nothing as long as one candidate is real.
    ///  2. A list where *nothing* resolves logs once, naming every candidate tried, and then stays
    ///     silent. A missing effect must never spam or throw — it is decoration.
    ///
    /// Run `oldways_dumpeffects` to get the real prefab list and put verified names in config.
    /// </summary>
    internal static class Effects
    {
        // Resolved prefab per candidate-list string. Null means "already looked, found nothing".
        private static readonly Dictionary<string, GameObject> Cache = new();

        /// <summary>
        /// Spawns the first candidate prefab that exists, at a position. Purely cosmetic and
        /// local — effects are not networked, so other players will not see them. That is
        /// acceptable for now: every power is local-player-only anyway.
        /// </summary>
        internal static void Spawn(string candidates, Vector3 position, Quaternion? rotation = null)
        {
            if (!OldWaysConfig.VisualEffectsEnabled.Value) return;
            if (Plugin.IsHeadless) return;                 // no point rendering on a dedicated server
            if (string.IsNullOrEmpty(candidates)) return;

            GameObject prefab = Resolve(candidates);
            if (prefab == null) return;

            Object.Instantiate(prefab, position, rotation ?? Quaternion.identity);
        }

        /// <summary>Spawns on a character, lifted to roughly chest height so it reads on screen.</summary>
        internal static void SpawnOn(string candidates, Character character, float heightFraction = 0.6f)
        {
            if (character == null) return;

            float height = character.GetHeight();
            Vector3 pos = character.transform.position + Vector3.up * (height * heightFraction);
            Spawn(candidates, pos);
        }

        private static GameObject Resolve(string candidates)
        {
            if (Cache.TryGetValue(candidates, out GameObject cached)) return cached;

            ZNetScene scene = ZNetScene.instance;
            if (scene == null) return null;               // too early — do not cache, try again later

            var tried = new List<string>();
            foreach (string raw in candidates.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0) continue;
                tried.Add(name);

                GameObject prefab = scene.GetPrefab(name);
                if (prefab != null)
                {
                    Cache[candidates] = prefab;
                    if (OldWaysConfig.VerboseLogging.Value)
                        Plugin.Log.LogInfo($"[Effects] '{name}' resolved for candidates [{candidates}].");
                    return prefab;
                }
            }

            Cache[candidates] = null;
            Plugin.Log.LogWarning($"[Effects] no prefab found among [{string.Join(", ", tried.ToArray())}] — " +
                                  "that effect will not show. Run 'oldways_dumpeffects' and set a real name in config.");
            return null;
        }

        /// <summary>Prefab lookups are per-world; drop them so a different world re-resolves.</summary>
        internal static void Clear() => Cache.Clear();
    }
}
