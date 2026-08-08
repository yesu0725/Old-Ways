using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// **Riposte** — Swords, Proven rank 1 (docs/02).
    ///
    /// Trigger: a perfect block (parry), then an immediate sword attack. The follow-up hit is a
    /// guaranteed critical stagger — it staggers regardless of how much stagger damage the target
    /// has accumulated or how resistant it is.
    ///
    /// This is the Phase 2 vertical slice: the first power built end to end, deliberately alone, so
    /// that any structural problem in the power pipeline is found once instead of thirteen times
    /// (docs/09).
    ///
    /// It reuses an input the weapon already has — no new key, nothing bolted on. Vanilla already
    /// rewards a parry with a stagger on the *attacker*; this finishes that thought by letting the
    /// riposte itself land as a guaranteed break.
    /// </summary>
    internal static class SwordRiposte
    {
        private static float _parryTime = float.NegativeInfinity;

        private static void Trace(string message)
        {
            if (OldWaysConfig.VerboseLogging.Value) Plugin.Log.LogInfo("[Riposte] " + message);
        }

        /// <summary>
        /// Vanilla decides a perfect block inside BlockAttack. Rather than re-deriving the timing,
        /// read the same two fields it uses: the block timer and the perfect-block interval.
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        private static class MarkParry
        {
            private static void Postfix(Humanoid __instance, bool __result)
            {
                if (!__result) return;                          // block failed entirely
                if (!OldWaysConfig.SwordRiposteEnabled.Value) return;
                if (!PowerGate.IsLocalPlayer(__instance)) return;

                float blockTimer = Traverse.Create(__instance).Field<float>("m_blockTimer").Value;
                float perfectWindow = Traverse.Create(typeof(Humanoid)).Field<float>("m_perfectBlockInterval").Value;
                if (perfectWindow <= 0f) perfectWindow = 0.25f;  // defensive: never treat every block as a parry

                bool isParry = blockTimer <= perfectWindow;
                Trace($"block: timer={blockTimer:0.###} perfectWindow={perfectWindow:0.###} -> " +
                      (isParry ? "PARRY" : "ordinary block"));

                if (!isParry) return;

                if (!PowerGate.Unlocked(Skills.SkillType.Swords, 1))
                {
                    ProvenRecord record = ProvenRpc.RecordForLocalPlayer();
                    Trace($"parry landed but Riposte is locked: Swords rank {record.GetRank(Skills.SkillType.Swords)} " +
                          $"({record.GetPoints(Skills.SkillType.Swords)} PP), vanilla skill " +
                          $"{Player.m_localPlayer.GetSkills().GetSkillLevel(Skills.SkillType.Swords):0} " +
                          $"(need rank 1 and skill {OldWaysConfig.SoftSkillPrerequisite.Value}).");
                    return;
                }

                _parryTime = Time.time;
                Trace($"parry landed — riposte armed for {OldWaysConfig.SwordRiposteWindow.Value:0.##}s.");
            }
        }

        /// <summary>
        /// The follow-up. Applied as a postfix so the damage lands first: staggering a corpse is
        /// pointless, and a target killed outright does not need it.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class ApplyRiposteStagger
        {
            private static void Postfix(Character __instance, HitData hit)
            {
                if (__instance == null || hit == null) return;
                if (!OldWaysConfig.SwordRiposteEnabled.Value) return;
                if (hit.m_skill != Skills.SkillType.Swords) return;
                if (Time.time - _parryTime > OldWaysConfig.SwordRiposteWindow.Value) return;

                Player local = Player.m_localPlayer;
                if (local == null) return;

                // Only our own riposte, and never against another player.
                if (__instance.IsPlayer()) return;
                ZNetView localView = local.GetComponent<ZNetView>();
                if (localView == null || !localView.IsValid()) return;
                if (hit.m_attacker != localView.GetZDO().m_uid) return;

                // Spend the parry whether or not the target survives, so one parry buys one riposte.
                _parryTime = float.NegativeInfinity;

                if (__instance.IsDead()) return;

                Vector3 direction = hit.m_dir != Vector3.zero
                    ? hit.m_dir
                    : (__instance.transform.position - local.transform.position).normalized;

                __instance.Stagger(direction);
                Trace($"riposte staggered '{__instance.name}'.");
            }
        }

        /// <summary>Dropping the armed parry on world change avoids it surviving into a new session.</summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
        private static class ClearOnShutdown
        {
            private static void Postfix() => _parryTime = float.NegativeInfinity;
        }
    }
}
