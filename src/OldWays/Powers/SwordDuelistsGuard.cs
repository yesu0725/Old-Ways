using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// **Duelist's Guard** — Swords, Proven rank 1 (docs/02).
    ///
    /// Blocking and parrying with a sword works at shield strength. Vanilla already lets you block
    /// with a sword; it is simply so weak that nobody does, so the input exists and goes unused —
    /// exactly the kind of thing this mod finishes rather than invents. Mastery makes shieldless
    /// sword play viable: the old duelists did not carry a shield.
    ///
    /// REPLACED the original "Riposte" (guaranteed critical stagger on a parry follow-up), which was
    /// cut on 2026-08-08 as a no-op: vanilla already staggers an attacker on a perfect block and
    /// already applies a damage bonus against staggered targets, so the power did nothing a player
    /// could perceive. See docs/02.
    ///
    /// Implementation note: this boosts the *methods* `GetBlockPower` and `GetDeflectionForce`
    /// rather than the `m_blockPower` / `m_deflectionForce` fields. Those fields live on SharedData,
    /// which is shared by every instance of an item type — writing to them would permanently buff
    /// every sword in the world, for every player, and persist past the power being disabled.
    /// </summary>
    internal static class SwordDuelistsGuard
    {
        private static void Trace(string message)
        {
            if (OldWaysConfig.VerboseLogging.Value) Plugin.Log.LogInfo("[Duelist] " + message);
        }

        /// <summary>
        /// True when this item is a sword and the local player has earned the power.
        ///
        /// Note the item has no owner context, so this cannot distinguish whose sword it is. That
        /// is acceptable: other players' blocks are resolved on their own machines, so in practice
        /// the only blocks this affects locally are the local player's own.
        /// </summary>
        private static bool AppliesTo(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return false;
            if (item.m_shared.m_skillType != Skills.SkillType.Swords) return false;
            if (!OldWaysConfig.SwordDuelistsGuardEnabled.Value) return false;
            return PowerGate.Unlocked(Skills.SkillType.Swords, 1);
        }

        /// <summary>How hard the guard holds — the difference between a token block and a real one.</summary>
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(int), typeof(float))]
        private static class BoostBlockPower
        {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (!AppliesTo(__instance)) return;
                __result *= OldWaysConfig.SwordDuelistsGuardBlockMult.Value;
            }
        }

        /// <summary>
        /// Deflection force decides whether a parry actually throws the attacker off. Without this
        /// the sword would absorb the hit but fail to stagger anything substantial, which is half a
        /// parry and would feel broken.
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDeflectionForce), typeof(int))]
        private static class BoostDeflection
        {
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
            {
                if (!AppliesTo(__instance)) return;
                __result *= OldWaysConfig.SwordDuelistsGuardBlockMult.Value;
            }
        }

        /// <summary>
        /// Diagnostic only — reports when a sword block or parry actually benefited, so the power
        /// is verifiable in play rather than taken on faith.
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        private static class ReportBlock
        {
            private static void Postfix(Humanoid __instance, bool __result)
            {
                if (!__result) return;
                if (!OldWaysConfig.VerboseLogging.Value) return;
                if (!PowerGate.IsLocalPlayer(__instance)) return;

                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if (weapon?.m_shared == null) return;
                if (weapon.m_shared.m_skillType != Skills.SkillType.Swords) return;

                float blockTimer = Traverse.Create(__instance).Field<float>("m_blockTimer").Value;
                float perfectWindow = Traverse.Create(typeof(Humanoid)).Field<float>("m_perfectBlockInterval").Value;
                bool parry = perfectWindow > 0f && blockTimer <= perfectWindow;

                Trace($"sword {(parry ? "PARRY" : "block")} with '{weapon.m_shared.m_name}' — " +
                      $"Duelist's Guard {(AppliesTo(weapon) ? "ACTIVE" : "inactive")}.");
            }
        }
    }
}
