using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// **Impale** — Spears, Proven rank 1 (docs/02).
    ///
    /// A thrown spear pins its target where it stands for a few seconds.
    ///
    /// Vanilla's spear throw is a damage option with an annoying retrieval cost and nothing else;
    /// pinning turns it into a decision — commit your weapon to stop something. Bosses are exempt,
    /// because a pinned boss would trivialise every fight in the game.
    ///
    /// Implemented by refusing the creature's AI update while pinned and holding it still, rather
    /// than inventing a status effect. `BaseAI.StopMoving()` is vanilla's own "stand still" call.
    /// </summary>
    internal static class SpearImpale
    {
        private class Pin
        {
            internal float Until;
        }

        private static readonly Dictionary<int, Pin> Pinned = new();

        internal static bool IsPinned(Character character)
        {
            if (character == null) return false;
            if (!Pinned.TryGetValue(character.GetInstanceID(), out Pin pin)) return false;

            if (Time.time >= pin.Until)
            {
                Pinned.Remove(character.GetInstanceID());
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class PinOnThrownHit
        {
            private static void Postfix(Character __instance, HitData hit)
            {
                if (!OldWaysConfig.SpearImpaleEnabled.Value) return;
                if (__instance == null || hit == null || __instance.IsDead()) return;

                // Only the throw pins — a melee poke is not a commitment.
                if (!hit.m_ranged) return;
                if (!PowerGate.LocalHit(hit, __instance, Skills.SkillType.Spears, 1)) return;

                if (__instance.IsBoss())
                {
                    if (OldWaysConfig.VerboseLogging.Value)
                        Plugin.Log.LogInfo($"[Impale] '{__instance.name}' is a boss — not pinned, by design.");
                    return;
                }

                float duration = OldWaysConfig.SpearImpaleDuration.Value;
                Pinned[__instance.GetInstanceID()] = new Pin { Until = Time.time + duration };

                if (OldWaysConfig.VerboseLogging.Value)
                    Plugin.Log.LogInfo($"[Impale] pinned '{__instance.name}' for {duration:0.#}s.");
            }
        }

        /// <summary>
        /// Hold the creature in place. Patching BaseAI covers MonsterAI and AnimalAI alike, since
        /// both inherit this entry point.
        /// </summary>
        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.UpdateAI))]
        private static class HoldStill
        {
            private static bool Prefix(BaseAI __instance, ref bool __result)
            {
                if (!OldWaysConfig.SpearImpaleEnabled.Value) return true;
                if (__instance == null) return true;

                Character character = __instance.m_character;
                if (!IsPinned(character)) return true;

                __instance.StopMoving();
                __result = false;
                return false;       // skip the AI's own movement and attack decisions this tick
            }
        }

        /// <summary>Pins do not survive a world change.</summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
        private static class ClearOnShutdown
        {
            private static void Postfix() => Pinned.Clear();
        }
    }
}
