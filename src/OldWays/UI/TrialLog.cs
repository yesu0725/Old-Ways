using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// The trial log (docs/06): Proven progress shown inside the vanilla skills screen rather than
    /// in a panel of its own. No new keybind, no new UI surface — the player already looks here for
    /// skill information.
    ///
    /// Deliberately additive and defensive. It appends to text that already exists on each skill
    /// row; if the row layout is not what we expect it logs once and does nothing. A cosmetic
    /// feature must never be able to break the skills screen.
    /// </summary>
    internal static class TrialLog
    {
        private static bool _layoutWarningLogged;

        [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
        private static class DecorateSkillRows
        {
            private static void Postfix(SkillsDialog __instance, Player player)
            {
                if (!OldWaysConfig.ModEnabled.Value) return;
                if (__instance == null || player == null) return;

                try
                {
                    Decorate(__instance, player);
                }
                catch (Exception e)
                {
                    // Never take the skills screen down over a cosmetic overlay.
                    Plugin.Log.LogError($"[TrialLog] failed to decorate skills screen: {e}");
                }
            }
        }

        private static void Decorate(SkillsDialog dialog, Player player)
        {
            var elements = Traverse.Create(dialog).Field<List<GameObject>>("m_elements").Value;
            if (elements == null) return;

            List<Skills.Skill> skills = player.GetSkills().GetSkillList();
            if (skills == null) return;

            ProvenRecord record = ProvenRpc.RecordForLocalPlayer();

            // Vanilla builds one element per entry of GetSkillList(), in order.
            int count = Math.Min(elements.Count, skills.Count);
            for (int i = 0; i < count; i++)
            {
                Skills.Skill skill = skills[i];
                if (skill?.m_info == null) continue;

                Skills.SkillType type = skill.m_info.m_skill;
                if (!ProvenSkills.IsTracked(type)) continue;   // untracked skills stay exactly vanilla

                AppendProvenLine(elements[i], type, record);
            }
        }

        private static void AppendProvenLine(GameObject element, Skills.SkillType type, ProvenRecord record)
        {
            if (element == null) return;

            int points = record.GetPoints(type);
            int rank = record.GetRank(type);
            int next = ProvenRecord.NextThreshold(rank);

            TMP_Text levelText = FindLevelText(element);
            if (levelText == null)
            {
                if (!_layoutWarningLogged)
                {
                    _layoutWarningLogged = true;
                    Plugin.Log.LogWarning("[TrialLog] could not find the level text on a skill row; " +
                                          "Proven progress will not be shown. Row children: " +
                                          DescribeChildren(element));
                }
                return;
            }

            // Strip a previous decoration so reopening the screen does not stack them.
            string baseText = levelText.text ?? "";
            int marker = baseText.IndexOf(MarkerStart, StringComparison.Ordinal);
            if (marker >= 0) baseText = baseText.Substring(0, marker);

            levelText.text = baseText + MarkerStart + BuildSuffix(points, rank, next);
        }

        private const string MarkerStart = "  <color=#c9a227>";

        private static string BuildSuffix(int points, int rank, int nextThreshold)
        {
            var sb = new StringBuilder();
            sb.Append(ProvenSkills.RankName(rank));
            if (nextThreshold > 0) sb.Append(' ').Append(points).Append('/').Append(nextThreshold);
            else sb.Append(' ').Append(points);
            sb.Append("</color>");
            return sb.ToString();
        }

        private static readonly string[] LevelTextNames = { "leveltext", "level", "levelText", "skilllevel" };

        private static TMP_Text FindLevelText(GameObject element)
        {
            TMP_Text[] texts = element.GetComponentsInChildren<TMP_Text>(true);
            if (texts == null || texts.Length == 0) return null;

            foreach (string candidate in LevelTextNames)
            {
                foreach (TMP_Text t in texts)
                {
                    if (string.Equals(t.gameObject.name, candidate, StringComparison.OrdinalIgnoreCase)) return t;
                }
            }

            // Fall back to any child whose name mentions "level".
            foreach (TMP_Text t in texts)
            {
                if (t.gameObject.name.IndexOf("level", StringComparison.OrdinalIgnoreCase) >= 0) return t;
            }
            return null;
        }

        private static string DescribeChildren(GameObject element)
        {
            var sb = new StringBuilder();
            foreach (TMP_Text t in element.GetComponentsInChildren<TMP_Text>(true))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(t.gameObject.name);
            }
            return sb.Length == 0 ? "(no TMP_Text children)" : sb.ToString();
        }
    }
}
