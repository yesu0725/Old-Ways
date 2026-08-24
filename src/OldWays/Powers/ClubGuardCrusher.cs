using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// **Guard Crusher** — Clubs, Proven rank 1 (docs/02).
    ///
    /// Club blows break through a raised guard: a creature that would have blocked the hit does not.
    ///
    /// Vanilla creatures use the same <see cref="Humanoid.BlockAttack"/> path players do, so this
    /// needs no new machinery — it refuses the block that was about to happen. Blunt weapons going
    /// through a shield is intuitive, and it answers the shield-raising 2-star Draugr that
    /// docs/05 adds.
    ///
    /// This became available only because shield bash turned out not to exist (docs/02), which left
    /// "break an enemy's guard" unclaimed.
    /// </summary>
    internal static class ClubGuardCrusher
    {
        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        private static class RefuseBlock
        {
            private static bool Prefix(Humanoid __instance, HitData hit, ref bool __result)
            {
                if (!OldWaysConfig.ClubGuardCrusherEnabled.Value) return true;
                if (__instance == null || hit == null) return true;

                // Only creatures' guards break — never another player's.
                if (__instance.IsPlayer()) return true;

                if (!PowerGate.LocalHit(hit, __instance, Skills.SkillType.Clubs, 1)) return true;

                // Skip vanilla's block entirely: the hit lands as though no guard were raised.
                __result = false;
                Effects.SpawnOn(OldWaysConfig.FxGuardCrusher.Value, __instance);

                if (OldWaysConfig.VerboseLogging.Value)
                    Plugin.Log.LogInfo($"[GuardCrusher] broke '{__instance.name}' guard.");

                return false;
            }
        }
    }
}
