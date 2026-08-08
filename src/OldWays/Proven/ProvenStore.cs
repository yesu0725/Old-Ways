using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;

namespace OldWays
{
    /// <summary>
    /// Server-side Proven storage and the award pipeline. Authoritative: clients never write here.
    ///
    /// Persists to a mod-owned file keyed by world UID, so Proven survives death, world reload and
    /// server restart (docs/03) and is not entangled with vanilla save data.
    /// </summary>
    internal static class ProvenStore
    {
        private static readonly Dictionary<long, ProvenRecord> Records = new();

        // Diminishing returns: per player, per creature prefab, within a rolling window.
        private class KillStreak
        {
            internal int Count;
            internal DateTime LastKill;
        }

        private static readonly Dictionary<long, Dictionary<string, KillStreak>> Streaks = new();

        private static bool _dirty;
        private static string _path;

        internal static ProvenRecord Get(long playerId)
        {
            if (!Records.TryGetValue(playerId, out ProvenRecord record))
            {
                record = new ProvenRecord();
                Records[playerId] = record;
            }
            return record;
        }

        /// <summary>
        /// Applies the full docs/03 award pipeline for a kill and returns the points actually
        /// granted (0 if the kill earned nothing). Server-side only.
        /// </summary>
        private static void Trace(string message)
        {
            if (OldWaysConfig.VerboseLogging.Value) Plugin.Log.LogInfo("[Proven] " + message);
        }

        internal static int AwardKill(long playerId, string victimPrefab, int victimLevel,
                                      Skills.SkillType skill, bool duringBossFight, bool sneakKill)
        {
            if (!ProvenSkills.IsTracked(skill))
            {
                Trace($"no award: skill {skill} is not a tracked Proven skill.");
                return 0;
            }

            // --- base weight -----------------------------------------------------------
            // Highest applicable weight wins rather than summing; stacking a boss-fight kill on
            // top of a 2-star kill would inflate far past the intended pacing.
            int basePoints = 0;
            if (victimLevel >= 3) basePoints = OldWaysConfig.PpTwoStarKill.Value;       // 2-star
            else if (victimLevel == 2) basePoints = OldWaysConfig.PpOneStarKill.Value;  // 1-star

            if (sneakKill && skill == Skills.SkillType.Knives)
                basePoints = Math.Max(basePoints, OldWaysConfig.PpSneakKill.Value);

            if (duringBossFight)
                basePoints = Math.Max(basePoints, OldWaysConfig.PpBossFightKill.Value);

            // Unstarred trash outside a boss fight is not a trial. No credit, by design.
            if (basePoints <= 0)
            {
                Trace($"no award: '{victimPrefab}' was level {victimLevel} (1=unstarred, 2=1-star, 3=2-star) " +
                      "outside a boss fight. Only starred kills and boss-fight kills are trials.");
                return 0;
            }

            // --- tier gate -------------------------------------------------------------
            // Tier is this player's own, not the world's: a newcomer on an established server must
            // still be able to earn (docs/03).
            ProvenRecord record = Get(playerId);
            int playerTier = record.Tier;
            int creatureTier = ProgressionTier.CreatureTier(victimPrefab, playerTier);

            float tierMult = ProgressionTier.Multiplier(creatureTier, playerTier);
            if (tierMult <= 0f)
            {
                Trace($"no award: '{victimPrefab}' is tier {creatureTier} and you are tier {playerTier} " +
                      $"({playerTier - creatureTier} tiers above it). Creatures two or more tiers below " +
                      "you earn nothing — this is the anti-farm rule, not a bug.");
                return 0;
            }

            // Facing something new raises this player's tier. Done after the gate so the kill that
            // promotes you is still paid at the rate you earned it.
            if (record.RaiseTier(creatureTier))
            {
                _dirty = true;
                Trace($"player {playerId} progression tier is now {record.Tier} (killed '{victimPrefab}').");
            }

            // --- diminishing returns ---------------------------------------------------
            // Boss fights are a discrete trial, not a farm loop, so they are exempt.
            float drMult = duringBossFight ? 1f : ConsumeDiminishingReturns(playerId, victimPrefab);

            int award = (int)Math.Round(basePoints * tierMult * drMult, MidpointRounding.AwayFromZero);
            if (award <= 0)
            {
                Trace($"no award: {basePoints} base x {tierMult:0.##} tier x {drMult:0.##} diminishing " +
                      "returns rounded to zero. Kill something you have not killed recently.");
                return 0;
            }

            int before = record.GetPoints(skill);
            int rankBefore = record.GetRank(skill);
            int after = record.AddPoints(skill, award);
            int rankAfter = record.GetRank(skill);
            _dirty = true;

            if (rankAfter > rankBefore)
            {
                Plugin.Log.LogInfo($"[Proven] player {playerId} reached rank {rankAfter} " +
                                   $"({ProvenSkills.RankName(rankAfter)}) in {skill} at {after} PP.");
            }
            else
            {
                Trace($"player {playerId} +{award} {skill} ({before} -> {after}) " +
                      $"[{victimPrefab} lvl{victimLevel} tier{creatureTier}/{playerTier} " +
                      $"tier x{tierMult:0.##} dr x{drMult:0.##}]");
            }

            return award;
        }

        /// <summary>
        /// Direct grant, bypassing the earning rules. Only the admin test command reaches this
        /// (see ProvenGrantCommand) — the normal path is AwardKill.
        /// </summary>
        internal static int GrantPoints(long playerId, Skills.SkillType skill, int points)
        {
            if (!ProvenSkills.IsTracked(skill) || points <= 0) return 0;
            int total = Get(playerId).AddPoints(skill, points);
            _dirty = true;
            Save();     // an admin grant is rare and worth persisting immediately
            return total;
        }

        /// <summary>
        /// Sets an exact value for one skill. Admin only. Returns the previous value so the caller
        /// can report what was overwritten — these commands are destructive and the admin should
        /// see what they replaced.
        /// </summary>
        internal static int SetPoints(long playerId, Skills.SkillType skill, int points)
        {
            if (!ProvenSkills.IsTracked(skill)) return 0;
            ProvenRecord record = Get(playerId);
            int before = record.GetPoints(skill);
            record.SetPoints(skill, points);
            _dirty = true;
            Save();
            return before;
        }

        /// <summary>
        /// Clears one skill, or everything (including progression tier) when skill is null.
        /// Returns a short description of what was cleared, for the admin's confirmation.
        /// </summary>
        internal static string ResetPlayer(long playerId, Skills.SkillType? skill)
        {
            ProvenRecord record = Get(playerId);

            if (skill.HasValue)
            {
                int before = record.GetPoints(skill.Value);
                record.SetPoints(skill.Value, 0);
                _dirty = true;
                Save();
                return $"{skill.Value} cleared (was {before} PP)";
            }

            int tierBefore = record.Tier;
            var cleared = new List<string>();
            foreach (Skills.SkillType s in ProvenSkills.Tracked)
            {
                int pts = record.GetPoints(s);
                if (pts > 0) cleared.Add($"{s} {pts}");
            }

            record.ResetAll();
            _dirty = true;
            Save();

            return cleared.Count == 0
                ? $"nothing to clear (tier was {tierBefore}, now 1)"
                : $"cleared {string.Join(", ", cleared.ToArray())}; tier {tierBefore} -> 1";
        }

        /// <summary>Every player the store knows about, for the admin listing.</summary>
        internal static IEnumerable<KeyValuePair<long, ProvenRecord>> AllRecords() => Records;

        private static float ConsumeDiminishingReturns(long playerId, string prefab)
        {
            if (string.IsNullOrEmpty(prefab)) prefab = "unknown";

            if (!Streaks.TryGetValue(playerId, out Dictionary<string, KillStreak> byPrefab))
            {
                byPrefab = new Dictionary<string, KillStreak>();
                Streaks[playerId] = byPrefab;
            }

            if (!byPrefab.TryGetValue(prefab, out KillStreak streak))
            {
                streak = new KillStreak();
                byPrefab[prefab] = streak;
            }

            DateTime now = DateTime.UtcNow;
            double windowMinutes = OldWaysConfig.DimReturnsWindowMinutes.Value;
            if ((now - streak.LastKill).TotalMinutes > windowMinutes) streak.Count = 0;

            float mult = 1f;
            for (int i = 0; i < streak.Count; i++) mult *= OldWaysConfig.DimReturnsFactor.Value;

            float floor = OldWaysConfig.DimReturnsFloor.Value;
            if (mult < floor) mult = floor;

            streak.Count++;
            streak.LastKill = now;
            return mult;
        }

        // ---- persistence ---------------------------------------------------------------

        private static string StorePath()
        {
            if (_path != null) return _path;

            long worldUid = ZNet.instance != null ? ZNet.instance.GetWorldUID() : 0L;
            string dir = Path.Combine(Paths.ConfigPath, "OldWays");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"proven_{worldUid}.dat");
            return _path;
        }

        internal static void Load()
        {
            Records.Clear();
            Streaks.Clear();
            _path = null;

            string path = StorePath();
            if (!File.Exists(path))
            {
                Plugin.Log.LogInfo($"[Proven] no store at {path} — starting fresh.");
                return;
            }

            try
            {
                int loaded = 0;
                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;

                    // playerId|displayName|1:150,6:920
                    string[] parts = line.Split('|');
                    if (parts.Length < 3) continue;
                    if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long playerId)) continue;

                    Records[playerId] = ProvenRecord.Deserialize(parts[2]);
                    // Names come back too, so an admin can target a player who has not logged in
                    // since the server started.
                    if (parts[1] != "?") PlayerRegistry.RememberName(playerId, parts[1]);
                    loaded++;
                }
                Plugin.Log.LogInfo($"[Proven] loaded {loaded} record(s) from {path}");
            }
            catch (Exception e)
            {
                // Never let a corrupt store take the server down; start fresh and keep the old file.
                Plugin.Log.LogError($"[Proven] failed to read {path}: {e.Message}. Starting fresh; the file is left in place.");
                Records.Clear();
            }
        }

        internal static void Save(bool force = false)
        {
            if (!_dirty && !force) return;

            string path = StorePath();
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# The Old Ways — Proven store. Server-authoritative; do not hand-edit while the server runs.");
                sb.AppendLine("# format: playerId|displayName|skillId:points,...");
                foreach (KeyValuePair<long, ProvenRecord> kv in Records)
                {
                    string data = kv.Value.Serialize();
                    if (string.IsNullOrEmpty(data)) continue;
                    sb.Append(kv.Key.ToString(CultureInfo.InvariantCulture)).Append('|')
                      .Append(NameFor(kv.Key)).Append('|').AppendLine(data);
                }

                // Write to a temp file and swap, so a crash mid-write cannot truncate the store.
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, sb.ToString());
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);

                _dirty = false;
                Plugin.Log.LogDebug($"[Proven] saved {Records.Count} record(s) to {path}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Proven] failed to save {path}: {e.Message}");
            }
        }

        internal static void RememberName(long playerId, string name)
        {
            if (!string.IsNullOrEmpty(name)) PlayerRegistry.RememberName(playerId, name.Replace("|", ""));
        }

        private static string NameFor(long playerId) => PlayerRegistry.NameFor(playerId);
    }
}
