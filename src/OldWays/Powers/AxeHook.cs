using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// **Hook** — Axes, Proven rank 1 (docs/02).
    ///
    /// An axe drags its target toward you instead of driving it away. Hooking with the beard of the
    /// axe is a real technique, and tactically it is the opposite of everything else in the game:
    /// vanilla knockback only ever pushes.
    ///
    /// Deliberately **not a strength increase** — it reuses the knockback force the hit already had
    /// and only reverses its direction. Pulling an archer out of position or stopping something
    /// fleeing is the payoff, not extra damage.
    ///
    /// Implemented by flipping the hit direction before vanilla reads it, so the existing pushback
    /// path does the work. Applying our own force afterwards would stack on top of vanilla's and
    /// double the effect.
    /// </summary>
    internal static class AxeHook
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class ReverseKnockback
        {
            private static void Prefix(Character __instance, HitData hit)
            {
                if (!OldWaysConfig.AxeHookEnabled.Value) return;
                if (__instance == null || hit == null) return;
                if (!PowerGate.LocalHit(hit, __instance, Skills.SkillType.Axes, 1)) return;

                Player local = Player.m_localPlayer;
                if (local == null) return;

                // Point the impulse back at the attacker. Kept horizontal so a hooked creature is
                // dragged along the ground rather than flung upward.
                Vector3 toAttacker = local.transform.position - __instance.transform.position;
                toAttacker.y = 0f;
                if (toAttacker.sqrMagnitude < 0.01f) return;

                hit.m_dir = toAttacker.normalized;
                hit.m_pushForce *= OldWaysConfig.AxeHookPullMultiplier.Value;

                if (OldWaysConfig.VerboseLogging.Value)
                    Plugin.Log.LogInfo($"[Hook] pulling '{__instance.name}' (force {hit.m_pushForce:0.#}).");
            }
        }
    }
}
