using System.Collections.Generic;

namespace OldWays
{
    /// <summary>
    /// The 12 Proven tracks. Verified against the game's Skills.SkillType enum — these are exactly
    /// the combat skills, which is why docs/03 tracks Proven per vanilla skill rather than per
    /// weapon family: the game already draws the line where we want it.
    ///
    /// Each track follows the ladder in docs/02 — R1 signature power, R3 secondary-attack perk,
    /// R5 chain-into-secondary. Blocking is the exception: it has no attack, so it carries three
    /// powers of its own up the same rungs.
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
            Skills.SkillType.Unarmed,
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
                    yield return new KeyValuePair<int, string>(1, "Duelist's Guard — your sword blocks and parries like a shield");
                    yield return new KeyValuePair<int, string>(3, "Thread the Gap — your thrust cannot be blocked or deflected");
                    yield return new KeyValuePair<int, string>(5, "your attack chain flows into the thrust");
                    break;
                case Skills.SkillType.Knives:
                    yield return new KeyValuePair<int, string>(1, "Vanish — a sneak-attack kill lets you slip out of sight again");
                    yield return new KeyValuePair<int, string>(3, "Falling Fang — the leaping stab, begun unseen, strikes as a full backstab");
                    yield return new KeyValuePair<int, string>(5, "your attack chain flows into the leap");
                    break;
                case Skills.SkillType.Clubs:
                    yield return new KeyValuePair<int, string>(1, "Guard Crusher — your blows break through a raised guard");
                    yield return new KeyValuePair<int, string>(3, "Uplift — the rising swing throws your target off its feet");
                    yield return new KeyValuePair<int, string>(5, "your attack chain flows into the rising swing");
                    break;
                case Skills.SkillType.Axes:
                    yield return new KeyValuePair<int, string>(1, "Hook — your charged attack drags the target to you");
                    yield return new KeyValuePair<int, string>(3, "Cleave — the secondary carries into what stands behind");
                    yield return new KeyValuePair<int, string>(5, "your attack chain flows into the secondary");
                    break;
                case Skills.SkillType.Polearms:
                    yield return new KeyValuePair<int, string>(1, "Set Against the Charge — a braced guard impales what charges you");
                    yield return new KeyValuePair<int, string>(3, "Whirlwind — the spin doubles and carries you forward, unbroken");
                    yield return new KeyValuePair<int, string>(5, "your attack chain flows into the whirlwind");
                    break;
                case Skills.SkillType.Spears:
                    yield return new KeyValuePair<int, string>(1, "Impale — a charged throw pins your quarry where it stands");
                    yield return new KeyValuePair<int, string>(3, "Recall — hold block to call a thrown spear back to your hand");
                    yield return new KeyValuePair<int, string>(5, "the throw leaves your hand the instant you call for it");
                    break;
                case Skills.SkillType.Bows:
                    yield return new KeyValuePair<int, string>(1, "Piercing Shot — holding past full draw drives the arrow through");
                    yield return new KeyValuePair<int, string>(3, "Snap Kick — a fast kick that keeps your nock");
                    yield return new KeyValuePair<int, string>(5, "pierced targets no longer blunt the arrow");
                    break;
                case Skills.SkillType.Crossbows:
                    yield return new KeyValuePair<int, string>(1, "Steady Aim — a braced shot ignores armour and cannot be turned");
                    yield return new KeyValuePair<int, string>(3, "Snap Kick — a fast kick that keeps your bolt");
                    yield return new KeyValuePair<int, string>(5, "reload without breaking stride");
                    break;
                case Skills.SkillType.Unarmed:
                    yield return new KeyValuePair<int, string>(1, "Flow — each landed blow quickens the next");
                    yield return new KeyValuePair<int, string>(3, "Snap Kick — a fast kick that throws them back");
                    yield return new KeyValuePair<int, string>(5, "punch, punch, kick — one motion");
                    break;
                case Skills.SkillType.Blocking:
                    yield return new KeyValuePair<int, string>(1, "Deflection — a perfect parry sends the shot back");
                    yield return new KeyValuePair<int, string>(3, "Immovable — nothing shifts you while you hold your guard");
                    yield return new KeyValuePair<int, string>(5, "Unbreakable — no single blow breaks your guard");
                    break;
                case Skills.SkillType.ElementalMagic:
                    yield return new KeyValuePair<int, string>(1, "Elemental Detonation — one element sets off another");
                    yield return new KeyValuePair<int, string>(3, "Snap Kick — a fast kick that keeps your cast");
                    yield return new KeyValuePair<int, string>(5, "detonations spread from target to target");
                    break;
                case Skills.SkillType.BloodMagic:
                    yield return new KeyValuePair<int, string>(1, "your summons answer in full, at once");
                    yield return new KeyValuePair<int, string>(2, "bear a weapon in your off hand while the staff works");
                    yield return new KeyValuePair<int, string>(3, "Snap Kick — a fast kick that keeps your cast");
                    yield return new KeyValuePair<int, string>(5, "your off hand strikes at full strength");
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
                case Skills.SkillType.Unarmed:
                    return "kill star-marked creatures bare-handed · kill during a boss fight";
                default:
                    return "kill star-marked creatures · kill during a boss fight";
            }
        }
    }
}
