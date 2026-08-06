using System.Collections.Generic;

namespace OldWays
{
    /// <summary>
    /// The 11 Proven tracks. Verified against the game's Skills.SkillType enum — these are exactly
    /// the combat skills (values 1-14), which is why docs/03 tracks Proven per vanilla skill rather
    /// than per weapon family: the game already draws the line where we want it.
    ///
    /// Blocking carries three powers staged up its rank ladder (docs/02) because it is one vanilla
    /// skill covering parry, bash and brace.
    /// </summary>
    internal static class ProvenSkills
    {
        internal static readonly Skills.SkillType[] Tracked =
        {
            Skills.SkillType.Swords,
            Skills.SkillType.Knives,
            Skills.SkillType.Clubs,
            Skills.SkillType.Polearms,
            Skills.SkillType.Spears,
            Skills.SkillType.Blocking,
            Skills.SkillType.Axes,
            Skills.SkillType.Bows,
            Skills.SkillType.Crossbows,
            Skills.SkillType.ElementalMagic,
            Skills.SkillType.BloodMagic,
        };

        private static readonly HashSet<Skills.SkillType> TrackedSet = new(Tracked);

        internal static bool IsTracked(Skills.SkillType skill) => TrackedSet.Contains(skill);

        /// <summary>Rank names from docs/03. Index = rank.</summary>
        private static readonly string[] RankNames =
        {
            "Untested", "Blooded", "Tempered", "Hardened", "Veteran", "Old Ways",
        };

        internal const int MaxRank = 5;

        internal static string RankName(int rank)
        {
            if (rank < 0) rank = 0;
            if (rank > MaxRank) rank = MaxRank;
            return RankNames[rank];
        }

        /// <summary>
        /// The power a skill unlocks, and at which rank. Blocking is the only multi-entry skill.
        /// Used by the trial log tooltip so a player can see what they are working toward.
        /// </summary>
        internal static IEnumerable<KeyValuePair<int, string>> PowersFor(Skills.SkillType skill)
        {
            switch (skill)
            {
                case Skills.SkillType.Swords:
                    yield return new KeyValuePair<int, string>(1, "Riposte — a parry follow-up is a guaranteed critical stagger");
                    break;
                case Skills.SkillType.Knives:
                    yield return new KeyValuePair<int, string>(1, "Ghost Step — a sneak-attack kill refunds stamina and muffles your steps");
                    break;
                case Skills.SkillType.Clubs:
                    yield return new KeyValuePair<int, string>(1, "Shockwave — a charged heavy swing staggers in a small radius");
                    break;
                case Skills.SkillType.Axes:
                    yield return new KeyValuePair<int, string>(1, "Execution — a heavy swing finishes a staggered, wounded target");
                    break;
                case Skills.SkillType.Polearms:
                    yield return new KeyValuePair<int, string>(1, "Unbroken Spin — no stamina drain and poise through the spin");
                    break;
                case Skills.SkillType.Spears:
                    yield return new KeyValuePair<int, string>(1, "Skewer — a charged throw pierces into what stands behind");
                    break;
                case Skills.SkillType.Bows:
                    yield return new KeyValuePair<int, string>(1, "True Shot — holding past full draw ignores stagger resistance");
                    break;
                case Skills.SkillType.Crossbows:
                    yield return new KeyValuePair<int, string>(1, "Steady Hands — keep your pace while reloading");
                    break;
                case Skills.SkillType.Blocking:
                    yield return new KeyValuePair<int, string>(1, "Unbroken Guard — a perfect parry negates your stagger and punishes the attacker");
                    yield return new KeyValuePair<int, string>(2, "Shield Bash — cheaper, and staggers what bashes normally cannot");
                    yield return new KeyValuePair<int, string>(3, "Brace — hold block to build a buffer against a guard-breaking hit");
                    break;
                case Skills.SkillType.ElementalMagic:
                    yield return new KeyValuePair<int, string>(1, "Conservation — a fully-charged cast may cost no Eitr");
                    break;
                case Skills.SkillType.BloodMagic:
                    yield return new KeyValuePair<int, string>(1, "Blood Price — a costly cast lends lifesteal to your next hit");
                    break;
            }
        }

        /// <summary>Trials that earn Proven for this skill — shown in the trial log so the player knows what to go do.</summary>
        internal static string TrialsFor(Skills.SkillType skill)
        {
            switch (skill)
            {
                case Skills.SkillType.Blocking:
                    return "block a boss's heavy attack without breaking guard · parry a real threat";
                case Skills.SkillType.Knives:
                    return "sneak-attack kills · star-marked kills · kills during a boss fight";
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.BloodMagic:
                    return "land a spell's full effect on a real threat · kills during a boss fight";
                default:
                    return "kill star-marked creatures · kill during a boss fight";
            }
        }
    }
}
