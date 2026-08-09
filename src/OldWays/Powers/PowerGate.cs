namespace OldWays
{
    /// <summary>
    /// The single place a weapon power asks "am I allowed to fire?". Every power in Phases 2-5
    /// goes through here, so the gate is defined once rather than thirteen times.
    ///
    /// A power is a gameplay effect, not a currency: the client evaluates its own gate against the
    /// Proven record the server pushed it. That record cannot be written client-side (docs/03), so
    /// the ordinary cheat routes — `raiseskill`, editing a save, editing config — do not open a
    /// power. A modified client could still fire one it has not earned; that is the same limit
    /// described for kill reports in docs/07 and is not separately fixable here.
    /// </summary>
    internal static class PowerGate
    {
        /// <summary>
        /// True when the local player has earned the given rank in a skill, and meets the vanilla
        /// skill prerequisite that goes with rank 1.
        /// </summary>
        internal static bool Unlocked(Skills.SkillType skill, int requiredRank)
        {
            if (!OldWaysConfig.ModEnabled.Value) return false;
            if (!OldWaysConfig.WeaponPowersEnabled.Value) return false;

            Player player = Player.m_localPlayer;
            if (player == null) return false;

            ProvenRecord record = ProvenRpc.RecordForLocalPlayer();
            if (record.GetRank(skill) < requiredRank) return false;

            // The soft prerequisite from docs/03: hands-on time as well as trials. Checked only for
            // the first rank — by rank 2 the vanilla skill is long past it.
            if (requiredRank <= 1)
            {
                float vanilla = player.GetSkills().GetSkillLevel(skill);
                if (vanilla < OldWaysConfig.SoftSkillPrerequisite.Value) return false;
            }

            return true;
        }

        /// <summary>Whether the given character is the local player. Powers are local-player-only.</summary>
        internal static bool IsLocalPlayer(Character character)
        {
            return character != null && Player.m_localPlayer != null &&
                   character == (Character)Player.m_localPlayer;
        }

        /// <summary>
        /// True when this hit came from the local player using the given skill, and that skill's
        /// power is unlocked at the required rank. The common shape of almost every power.
        ///
        /// Also excludes hits on other players — powers never fire in PvP.
        /// </summary>
        internal static bool LocalHit(HitData hit, Character victim, Skills.SkillType skill, int requiredRank)
        {
            if (hit == null) return false;
            if (hit.m_skill != skill) return false;
            if (victim != null && victim.IsPlayer()) return false;

            Player local = Player.m_localPlayer;
            if (local == null) return false;

            ZNetView view = local.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return false;
            if (hit.m_attacker != view.GetZDO().m_uid) return false;

            return Unlocked(skill, requiredRank);
        }

        /// <summary>The skill of the weapon the local player is currently holding, or None.</summary>
        internal static Skills.SkillType CurrentWeaponSkill(Humanoid humanoid)
        {
            ItemDrop.ItemData weapon = humanoid?.GetCurrentWeapon();
            return weapon?.m_shared == null ? Skills.SkillType.None : weapon.m_shared.m_skillType;
        }
    }
}
