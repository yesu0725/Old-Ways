using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// **Flow** — Unarmed, Proven rank 1 (docs/02).
    ///
    /// While you keep landing bare-handed blows, your attack chain never drops. Vanilla resets the
    /// chain to its first level after `m_chainAttackMaxTime`, which for fists means you rarely reach
    /// the chain finisher and its `m_lastChainDamageMultiplier`. Sustained, accurate aggression now
    /// keeps the combo alive — and it feeds directly into the R5 punch → punch → kick.
    ///
    /// **Why not attack speed.** Flow was first specified as shortening the next punch's recovery.
    /// Valheim has no per-attack speed control: timing is animation-driven, and the only lever is
    /// `ZSyncAnimation.SetSpeed`, which scales the character's entire animator (walking, blocking,
    /// everything) and is network-synced. `Attack.m_speedFactor` is movement speed *during* an
    /// attack, not the attack's own. Keeping the chain alive expresses the same intent — reward
    /// unbroken aggression — through a mechanism vanilla actually has.
    ///
    /// The streak is deliberately **not** extended by `m_chainAttackMaxTime` itself, which is a
    /// private *static*: writing it would change chain timing for every weapon in the game, for
    /// every player. `CanStartChainAttack` is per-attack, so the effect stays where it belongs.
    /// </summary>
    internal static class UnarmedFlow
    {
        private static float _lastLandedHit = float.NegativeInfinity;

        private static void Trace(string message)
        {
            if (OldWaysConfig.VerboseLogging.Value) Plugin.Log.LogInfo("[Flow] " + message);
        }

        private static bool StreakAlive =>
            Time.time - _lastLandedHit <= OldWaysConfig.UnarmedFlowWindow.Value;

        /// <summary>Landing a bare-handed blow keeps the streak alive; being hit ends it.</summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class TrackStreak
        {
            private static void Postfix(Character __instance, HitData hit)
            {
                if (!OldWaysConfig.UnarmedFlowEnabled.Value) return;
                if (__instance == null || hit == null) return;

                // Taking a hit breaks the flow, whatever caused it.
                if (PowerGate.IsLocalPlayer(__instance))
                {
                    if (StreakAlive) Trace("streak broken — you were hit.");
                    _lastLandedHit = float.NegativeInfinity;
                    return;
                }

                if (!PowerGate.LocalHit(hit, __instance, Skills.SkillType.Unarmed, 1)) return;

                _lastLandedHit = Time.time;
            }
        }

        /// <summary>
        /// Hold the chain open. A miss does not break the streak outright — it simply fails to
        /// refresh it, so the window lapses. That is a deliberate simplification: Valheim has no
        /// clean "this swing hit nothing" signal, and a lapse reads the same in play.
        /// </summary>
        [HarmonyPatch(typeof(Attack), nameof(Attack.CanStartChainAttack))]
        private static class HoldChainOpen
        {
            private static void Postfix(Attack __instance, ref bool __result)
            {
                if (__result) return;                        // vanilla already allows it
                if (!OldWaysConfig.UnarmedFlowEnabled.Value) return;
                if (__instance == null) return;
                if (!StreakAlive) return;

                if (!PowerGate.IsLocalPlayer(__instance.m_character)) return;

                ItemDrop.ItemData weapon = __instance.m_weapon;
                if (weapon?.m_shared == null) return;
                if (weapon.m_shared.m_skillType != Skills.SkillType.Unarmed) return;

                if (!PowerGate.Unlocked(Skills.SkillType.Unarmed, 1)) return;

                __result = true;
                Trace("chain held open.");
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
        private static class ClearOnShutdown
        {
            private static void Postfix() => _lastLandedHit = float.NegativeInfinity;
        }
    }
}
