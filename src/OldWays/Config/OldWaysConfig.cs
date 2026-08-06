using BepInEx.Configuration;
using ServerSync;

namespace OldWays
{
    /// <summary>
    /// Config with the three toggle levels from docs/07-technical-architecture.md:
    ///   1. per category  — an entire system off
    ///   2. per entry     — one power / boss reaction / creature behavior off
    ///   3. per value     — PP weights, thresholds, curve
    ///
    /// Level 3 is the important one: the numbers in docs/03-proven-system.md are an explicit
    /// first pass and must be retunable without a rebuild.
    ///
    /// Every entry goes through Bind(), which registers it with ServerSync — the server is the
    /// source of truth and pushes values to clients, admin-locked. Since Proven is
    /// server-authoritative, config that clients could diverge on would be a hole in the same
    /// wall.
    ///
    /// Level-2 entry toggles are registered by their own phases via Toggle(), not all up front.
    /// </summary>
    internal static class OldWaysConfig
    {
        private const string SecGeneral = "1 - General";
        private const string SecProven = "2 - Proven";
        private const string SecReactions = "3 - Enemy Reactions";

        internal static readonly ConfigSync SyncManager = new(Plugin.PluginGuid)
        {
            DisplayName = Plugin.PluginName,
            CurrentVersion = Plugin.PluginVersion,
            // Server-required (docs/07): clients on a different version are refused at connect
            // rather than silently desyncing.
            MinimumRequiredVersion = Plugin.PluginVersion,
            ModRequired = true,
        };

        /// <summary>
        /// When true, non-admin clients cannot override server-pushed values.
        /// Registered as ServerSync's locking entry.
        /// </summary>
        internal static ConfigEntry<bool> LockConfig;

        // ---- Level 1: category toggles ------------------------------------------------

        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<bool> SkillTweaksEnabled;
        internal static ConfigEntry<bool> WeaponPowersEnabled;
        internal static ConfigEntry<bool> BossReactionsEnabled;
        internal static ConfigEntry<bool> CreatureReactionsEnabled;

        // ---- Level 3: Proven tuning (docs/03-proven-system.md) -------------------------

        internal static ConfigEntry<int> RankThreshold1;
        internal static ConfigEntry<int> RankThreshold2;
        internal static ConfigEntry<int> RankThreshold3;
        internal static ConfigEntry<int> RankThreshold4;
        internal static ConfigEntry<int> RankThreshold5;
        internal static ConfigEntry<int> SoftSkillPrerequisite;

        internal static ConfigEntry<int> PpBossFightKill;
        internal static ConfigEntry<int> PpTwoStarKill;
        internal static ConfigEntry<int> PpOneStarKill;
        internal static ConfigEntry<int> PpBossHeavyBlocked;
        internal static ConfigEntry<int> PpParry;
        internal static ConfigEntry<int> PpSneakKill;
        internal static ConfigEntry<int> PpSpellFullEffect;

        internal static ConfigEntry<float> TierMultOneBelow;
        internal static ConfigEntry<float> TierMultSame;
        internal static ConfigEntry<float> TierMultAbove;

        internal static ConfigEntry<float> DimReturnsFactor;
        internal static ConfigEntry<float> DimReturnsFloor;
        internal static ConfigEntry<float> DimReturnsWindowMinutes;

        // ---- Level 3: reaction curve (docs/04-boss-reactions.md) -----------------------

        internal static ConfigEntry<float> ReactionChanceBase;
        internal static ConfigEntry<float> ReactionChancePerRank;

        internal static void Bind(ConfigFile cfg)
        {
            LockConfig = cfg.Bind(SecGeneral, "Lock Configuration", true,
                "If on, only server admins can change these settings; everyone else receives the " +
                "server's values. Server-authoritative Proven is pointless if clients can retune " +
                "the weights that feed it.");
            SyncManager.AddLockingConfigEntry(LockConfig);

            ModEnabled = Bind(cfg, SecGeneral, "Enabled", true,
                "Master switch. False disables every system in the mod.");
            SkillTweaksEnabled = Bind(cfg, SecGeneral, "Skill Mastery Tweaks", true,
                "Vanilla-skill threshold payoffs. Not gated behind Proven. See docs/01.");
            WeaponPowersEnabled = Bind(cfg, SecGeneral, "Weapon Mastery Powers", true,
                "One Proven-gated power per weapon/school. See docs/02.");
            BossReactionsEnabled = Bind(cfg, SecGeneral, "Boss Reactions", true,
                "Bosses gain a second act on an existing ability. See docs/04.");
            CreatureReactionsEnabled = Bind(cfg, SecGeneral, "Creature Reactions", true,
                "2-star creatures activate latent behaviors. See docs/05.");

            RankThreshold1 = Bind(cfg, SecProven, "Rank 1 Threshold (Blooded)", 150,
                "Proven Points for rank 1. This is the rank that unlocks a skill's power.");
            RankThreshold2 = Bind(cfg, SecProven, "Rank 2 Threshold (Tempered)", 400, "");
            RankThreshold3 = Bind(cfg, SecProven, "Rank 3 Threshold (Hardened)", 800, "");
            RankThreshold4 = Bind(cfg, SecProven, "Rank 4 Threshold (Veteran)", 1400, "");
            RankThreshold5 = Bind(cfg, SecProven, "Rank 5 Threshold (Old Ways)", 2200, "");
            SoftSkillPrerequisite = Bind(cfg, SecProven, "Vanilla Skill Prerequisite", 30,
                "Vanilla skill level also required for rank 1. Soft gate; Proven is the real one.");

            PpBossFightKill = Bind(cfg, SecProven, "PP - Kill During Boss Fight", 25, "");
            PpTwoStarKill = Bind(cfg, SecProven, "PP - Kill 2-Star Creature", 10, "");
            PpOneStarKill = Bind(cfg, SecProven, "PP - Kill 1-Star Creature", 4, "");
            PpBossHeavyBlocked = Bind(cfg, SecProven, "PP - Block Boss Heavy Attack", 20, "");
            PpParry = Bind(cfg, SecProven, "PP - Landed Parry", 6, "");
            PpSneakKill = Bind(cfg, SecProven, "PP - Sneak-Attack Kill", 6, "");
            PpSpellFullEffect = Bind(cfg, SecProven, "PP - Spell Full Effect", 8, "");

            TierMultOneBelow = Bind(cfg, SecProven, "Tier Multiplier - One Below", 0.5f,
                "Targets two or more tiers below the player always award 0 and are not configurable.");
            TierMultSame = Bind(cfg, SecProven, "Tier Multiplier - Same Tier", 1.0f, "");
            TierMultAbove = Bind(cfg, SecProven, "Tier Multiplier - Above Player", 1.5f, "");

            DimReturnsFactor = Bind(cfg, SecProven, "Diminishing Returns Factor", 0.6f,
                "PP multiplier applied per repeat kill of the same creature type in the window.");
            DimReturnsFloor = Bind(cfg, SecProven, "Diminishing Returns Floor", 0.1f, "");
            DimReturnsWindowMinutes = Bind(cfg, SecProven, "Diminishing Returns Window (minutes)", 10f,
                "Boss-fight kills are exempt from diminishing returns.");

            ReactionChanceBase = Bind(cfg, SecReactions, "Reaction Chance Base", 0.10f,
                "chance = base + perRank * encounterPresence, where presence is the HIGHEST " +
                "Proven rank among players in the encounter. Presence 0 means fully vanilla.");
            ReactionChancePerRank = Bind(cfg, SecReactions, "Reaction Chance Per Rank", 0.12f, "");
        }

        /// <summary>
        /// Single seam for every config entry: binds it and registers it with ServerSync so the
        /// server's value wins. Nothing in this mod should call cfg.Bind directly.
        /// </summary>
        private static ConfigEntry<T> Bind<T>(ConfigFile cfg, string section, string key, T defaultValue, string description)
        {
            ConfigEntry<T> entry = cfg.Bind(section, key, defaultValue, description);
            SyncManager.AddConfigEntry(entry);
            return entry;
        }

        /// <summary>
        /// Level-2 per-entry toggle. Phases 2-8 call this to register their own powers,
        /// boss reactions and creature behaviors as they are implemented.
        /// </summary>
        internal static ConfigEntry<bool> Toggle(ConfigFile cfg, string section, string key, string description)
        {
            return Bind(cfg, section, key, true, description);
        }

        /// <summary>Reaction chance for a given encounter presence (0-5).</summary>
        internal static float ReactionChance(int encounterPresence)
        {
            if (encounterPresence <= 0) return 0f;
            return ReactionChanceBase.Value + ReactionChancePerRank.Value * encounterPresence;
        }
    }
}
