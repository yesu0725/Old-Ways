using System.Collections.Generic;

namespace OldWays
{
    /// <summary>
    /// Progression tiers for the anti-farm rule in docs/03: a player gets no credit for creatures
    /// well below their own progression, reduced credit one tier below, and bonus credit above.
    ///
    /// Player tier comes from vanilla boss-defeat global keys. Creature tier comes from a prefab
    /// table. Both are server-side.
    /// </summary>
    internal static class ProgressionTier
    {
        internal const int Meadows = 1;
        internal const int BlackForest = 2;
        internal const int Swamp = 3;
        internal const int Mountain = 4;
        internal const int Plains = 5;
        internal const int Mistlands = 6;
        internal const int Ashlands = 7;

        /// <summary>
        /// Boss-defeat keys in progression order. Read as strings rather than the GlobalKeys enum:
        /// the enum only covers the first five defeats, while queen/fader exist as string keys.
        /// </summary>
        private static readonly string[] DefeatKeysInOrder =
        {
            "defeated_eikthyr",     // -> tier 1 cleared
            "defeated_gdking",      // Elder
            "defeated_bonemass",
            "defeated_dragon",      // Moder
            "defeated_goblinking",  // Yagluth
            "defeated_queen",
            "defeated_fader",
        };

        /// <summary>
        /// The player's progression tier: 1 until they kill Eikthyr, then one per boss defeated.
        /// Global keys are world-wide in vanilla, so on a shared server this is effectively the
        /// server's progression, not the individual's. Accepted for now — see docs/03.
        /// </summary>
        internal static int PlayerTier()
        {
            var zone = ZoneSystem.instance;
            if (zone == null) return Meadows;

            int tier = Meadows;
            foreach (string key in DefeatKeysInOrder)
            {
                if (!zone.GetGlobalKey(key)) break;
                tier++;
            }
            return tier > Ashlands ? Ashlands : tier;
        }

        private static readonly Dictionary<string, int> CreatureTiers = new()
        {
            // Meadows
            { "Boar", Meadows }, { "Neck", Meadows }, { "Deer", Meadows }, { "Greyling", Meadows },
            { "Eikthyr", Meadows },
            // Black Forest
            { "Greydwarf", BlackForest }, { "Greydwarf_Elite", BlackForest }, { "Greydwarf_Shaman", BlackForest },
            { "Skeleton", BlackForest }, { "Skeleton_NoArcher", BlackForest }, { "Ghost", BlackForest },
            { "Troll", BlackForest }, { "gd_king", BlackForest },
            // Swamp
            { "Draugr", Swamp }, { "Draugr_Elite", Swamp }, { "Draugr_Ranged", Swamp },
            { "Blob", Swamp }, { "BlobElite", Swamp }, { "Leech", Swamp }, { "Surtling", Swamp },
            { "Wraith", Swamp }, { "Skeleton_Poison", Swamp }, { "Abomination", Swamp }, { "Bonemass", Swamp },
            { "Serpent", Swamp },
            // Mountain
            { "Wolf", Mountain }, { "Fenring", Mountain }, { "Fenring_Cultist", Mountain },
            { "Hatchling", Mountain }, { "StoneGolem", Mountain }, { "Ulv", Mountain },
            { "Bat", Mountain }, { "Dragon", Mountain },
            // Plains
            { "Goblin", Plains }, { "GoblinBrute", Plains }, { "GoblinShaman", Plains },
            { "Deathsquito", Plains }, { "Lox", Plains }, { "BlobTar", Plains }, { "GoblinKing", Plains },
            // Mistlands
            { "Seeker", Mistlands }, { "SeekerBrute", Mistlands }, { "SeekerBrood", Mistlands },
            { "Tick", Mistlands }, { "Gjall", Mistlands }, { "Hare", Mistlands },
            { "DvergrRogue", Mistlands }, { "DvergrMage", Mistlands }, { "DvergrMageFire", Mistlands },
            { "DvergrMageIce", Mistlands }, { "DvergrMageSupport", Mistlands }, { "SeekerQueen", Mistlands },
            // Ashlands
            { "Charred_Melee", Ashlands }, { "Charred_Archer", Ashlands }, { "Charred_Mage", Ashlands },
            { "Charred_Twitcher", Ashlands }, { "Morgen", Ashlands }, { "Asksvin", Ashlands },
            { "BonemawSerpent", Ashlands }, { "Volture", Ashlands }, { "FallenValkyrie", Ashlands },
            { "Fader", Ashlands },
        };

        private static readonly HashSet<string> UnknownPrefabsLogged = new();

        /// <summary>
        /// Tier of a creature by prefab name. Unknown prefabs (modded creatures, including the
        /// author's own BiomeLords) return the player's tier so they neither farm nor are excluded;
        /// each unknown is logged once so the table can be extended.
        /// </summary>
        internal static int CreatureTier(string prefabName, int playerTier)
        {
            if (string.IsNullOrEmpty(prefabName)) return playerTier;

            if (CreatureTiers.TryGetValue(prefabName, out int tier)) return tier;

            // Prefabs are often suffixed (e.g. "Boar_piggy", "Skeleton_Friendly"). Prefix-match
            // before giving up.
            foreach (KeyValuePair<string, int> kv in CreatureTiers)
            {
                if (prefabName.StartsWith(kv.Key, System.StringComparison.Ordinal)) return kv.Value;
            }

            if (UnknownPrefabsLogged.Add(prefabName))
            {
                Plugin.Log.LogInfo($"[Proven] Unknown creature prefab '{prefabName}' — treating as player tier. " +
                                   "Add it to ProgressionTier.CreatureTiers to tier it properly.");
            }
            return playerTier;
        }

        /// <summary>Tier multiplier from docs/03. Two or more tiers below the player earns nothing.</summary>
        internal static float Multiplier(int creatureTier, int playerTier)
        {
            int delta = creatureTier - playerTier;
            if (delta <= -2) return 0f;
            if (delta == -1) return OldWaysConfig.TierMultOneBelow.Value;
            if (delta == 0) return OldWaysConfig.TierMultSame.Value;
            return OldWaysConfig.TierMultAbove.Value;
        }
    }
}
