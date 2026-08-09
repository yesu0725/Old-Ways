using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// **Set Against the Charge** — Polearms, Proven rank 1 (docs/02).
    ///
    /// Hold block with an atgeir and a creature that charges into you impales itself.
    ///
    /// This is the actual historical purpose of a polearm and nothing in Valheim does it: boars,
    /// Lox and Fenrings all charge, and vanilla lets you do nothing but absorb it. The trigger is
    /// the block input the weapon already has.
    ///
    /// Damage scales off the charger's own speed, so it punishes commitment rather than adding a
    /// flat number — a creature that walks into your guard gets nothing.
    /// </summary>
    internal static class PolearmSetAgainstCharge
    {
        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        private static class ImpaleCharger
        {
            private static void Postfix(Humanoid __instance, HitData hit, Character attacker, bool __result)
            {
                if (!__result) return;                       // the block did not hold
                if (!OldWaysConfig.PolearmBraceEnabled.Value) return;
                if (__instance == null || attacker == null) return;
                if (!PowerGate.IsLocalPlayer(__instance)) return;
                if (attacker.IsPlayer() || attacker.IsDead()) return;

                if (PowerGate.CurrentWeaponSkill(__instance) != Skills.SkillType.Polearms) return;
                if (!PowerGate.Unlocked(Skills.SkillType.Polearms, 1)) return;

                // Only a real charge counts: measure how fast the attacker is closing on us, not
                // just how fast it happens to be moving.
                Vector3 toPlayer = __instance.transform.position - attacker.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < 0.01f) return;

                float closingSpeed = Vector3.Dot(attacker.GetVelocity(), toPlayer.normalized);
                float required = OldWaysConfig.PolearmBraceMinSpeed.Value;
                if (closingSpeed < required)
                {
                    if (OldWaysConfig.VerboseLogging.Value)
                        Plugin.Log.LogInfo($"[SetAgainstCharge] '{attacker.name}' closed at {closingSpeed:0.#} m/s, " +
                                           $"needs {required:0.#} — not a charge.");
                    return;
                }

                var impale = new HitData
                {
                    m_damage = { m_pierce = closingSpeed * OldWaysConfig.PolearmBraceDamagePerSpeed.Value },
                    m_point = attacker.transform.position,
                    m_dir = -toPlayer.normalized,
                    m_pushForce = OldWaysConfig.PolearmBracePushForce.Value,
                    m_skill = Skills.SkillType.Polearms,
                    m_ranged = false,
                };
                impale.SetAttacker(__instance);

                attacker.Damage(impale);
                attacker.Stagger(-toPlayer.normalized);

                if (OldWaysConfig.VerboseLogging.Value)
                    Plugin.Log.LogInfo($"[SetAgainstCharge] impaled '{attacker.name}' at {closingSpeed:0.#} m/s " +
                                       $"for {impale.m_damage.m_pierce:0.#} pierce.");
            }
        }
    }
}
