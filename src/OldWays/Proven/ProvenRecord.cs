using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OldWays
{
    /// <summary>
    /// One player's Proven Points across the tracked skills. Deliberately NOT stored in vanilla
    /// skill data — that is the entire point of the system (docs/03): console skill commands must
    /// not be able to reach it.
    /// </summary>
    internal class ProvenRecord
    {
        private readonly Dictionary<Skills.SkillType, int> _points = new();

        /// <summary>
        /// This player's own progression tier: the highest-tier creature they have personally
        /// killed. Deliberately NOT read from world boss keys — those are world state, so on an
        /// established server a newcomer would inherit max tier and could never earn Proven from
        /// anything (docs/03). Advances only, never falls.
        /// </summary>
        internal int Tier { get; private set; } = ProgressionTier.Meadows;

        /// <summary>Raises the player's tier if this kill was higher than anything they had faced. Returns true if it moved.</summary>
        internal bool RaiseTier(int creatureTier)
        {
            if (creatureTier <= Tier) return false;
            Tier = creatureTier > ProgressionTier.Ashlands ? ProgressionTier.Ashlands : creatureTier;
            return true;
        }

        internal int GetPoints(Skills.SkillType skill)
        {
            return _points.TryGetValue(skill, out int v) ? v : 0;
        }

        /// <summary>Adds points and returns the new total. Proven never decreases (docs/03: permanent, no decay).</summary>
        internal int AddPoints(Skills.SkillType skill, int amount)
        {
            if (amount <= 0) return GetPoints(skill);
            int total = GetPoints(skill) + amount;
            _points[skill] = total;
            return total;
        }

        internal int GetRank(Skills.SkillType skill) => RankForPoints(GetPoints(skill));

        /// <summary>Highest rank across all tracked skills — a player's "Old Ways Presence" (docs/04).</summary>
        internal int Presence()
        {
            int best = 0;
            foreach (Skills.SkillType skill in ProvenSkills.Tracked)
            {
                int r = GetRank(skill);
                if (r > best) best = r;
            }
            return best;
        }

        internal static int RankForPoints(int points)
        {
            if (points >= OldWaysConfig.RankThreshold5.Value) return 5;
            if (points >= OldWaysConfig.RankThreshold4.Value) return 4;
            if (points >= OldWaysConfig.RankThreshold3.Value) return 3;
            if (points >= OldWaysConfig.RankThreshold2.Value) return 2;
            if (points >= OldWaysConfig.RankThreshold1.Value) return 1;
            return 0;
        }

        /// <summary>Points needed for the next rank, or 0 at max rank. For the trial log.</summary>
        internal static int NextThreshold(int rank)
        {
            switch (rank)
            {
                case 0: return OldWaysConfig.RankThreshold1.Value;
                case 1: return OldWaysConfig.RankThreshold2.Value;
                case 2: return OldWaysConfig.RankThreshold3.Value;
                case 3: return OldWaysConfig.RankThreshold4.Value;
                case 4: return OldWaysConfig.RankThreshold5.Value;
                default: return 0;
            }
        }

        // ---- serialization: "T:3,1:150,6:920" -----------------------------------------
        // "T:<n>" carries the player's tier. Records written before per-player tiering simply
        // lack it and load at tier 1, which is the safe direction: they earn more, not less.

        private const string TierKey = "T";

        internal string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append(TierKey).Append(':').Append(Tier.ToString(CultureInfo.InvariantCulture));
            foreach (KeyValuePair<Skills.SkillType, int> kv in _points)
            {
                if (kv.Value <= 0) continue;
                sb.Append(',').Append((int)kv.Key).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        internal static ProvenRecord Deserialize(string data)
        {
            var record = new ProvenRecord();
            if (string.IsNullOrEmpty(data)) return record;

            foreach (string pair in data.Split(','))
            {
                if (pair.StartsWith(TierKey + ":", StringComparison.Ordinal))
                {
                    if (int.TryParse(pair.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int t))
                        record.Tier = t < ProgressionTier.Meadows ? ProgressionTier.Meadows
                                    : t > ProgressionTier.Ashlands ? ProgressionTier.Ashlands : t;
                    continue;
                }

                string[] parts = pair.Split(':');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int skillId)) continue;
                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int points)) continue;

                var skill = (Skills.SkillType)skillId;
                // Drop anything no longer tracked rather than carrying dead data forward.
                if (ProvenSkills.IsTracked(skill) && points > 0) record._points[skill] = points;
            }
            return record;
        }
    }
}
