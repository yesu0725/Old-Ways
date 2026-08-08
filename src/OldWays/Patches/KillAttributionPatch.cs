using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// Works out which vanilla skill deserves credit for a killing blow, then reports it.
    ///
    /// This is the piece the whole Proven system rests on, which is why it was built first
    /// (docs/09 Phase 1). The mechanism is simpler than feared: vanilla's own HitData carries
    /// m_skill — the skill the game itself will award XP to — so we do not have to infer the
    /// weapon from equipment or animation state. We record the last hit that landed on a creature
    /// and read it back when the creature dies.
    ///
    /// Cases this deliberately gets right:
    ///   - damage-over-time and environmental kills carry no attacker/skill, so they award nothing
    ///   - a projectile in flight still carries the skill of the weapon that fired it
    ///   - kills by other players, tames or summons are not credited to the local player
    /// </summary>
    internal static class KillAttribution
    {
        private class LastHit
        {
            internal ZDOID Attacker;
            internal Skills.SkillType Skill;
            internal float Time;
            internal bool Sneak;
        }

        // Keyed by the victim's instance id. Cleared on death; swept periodically so a creature
        // that despawns without dying cannot leak an entry.
        private static readonly Dictionary<int, LastHit> LastHits = new();
        private static float _lastSweep;
        private const float HitMemorySeconds = 10f;

        private static void Trace(string message)
        {
            if (OldWaysConfig.VerboseLogging.Value) Plugin.Log.LogInfo("[Attribution] " + message);
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class RecordLastHit
        {
            private static void Postfix(Character __instance, HitData hit)
            {
                if (__instance == null || hit == null) return;
                if (!OldWaysConfig.ModEnabled.Value) return;
                if (__instance.IsPlayer()) return;              // we only care about creature deaths

                if (!hit.HaveAttacker())
                {
                    Trace($"hit on '{__instance.name}' has no attacker (DoT/environment) — not a trial.");
                    return;
                }
                if (!ProvenSkills.IsTracked(hit.m_skill))
                {
                    Trace($"hit on '{__instance.name}' used skill {hit.m_skill}, which is not tracked.");
                    return;
                }

                LastHits[__instance.GetInstanceID()] = new LastHit
                {
                    Attacker = hit.m_attacker,
                    Skill = hit.m_skill,
                    Time = Time.time,
                    // Vanilla sets a backstab bonus for sneak attacks; reuse it rather than
                    // re-deriving stealth state.
                    Sneak = hit.m_backstabBonus > 1f,
                };

                Trace($"recorded {hit.m_skill} hit on '{__instance.name}' (level {__instance.GetLevel()}).");
                Sweep();
            }
        }

        [HarmonyPatch(typeof(Character), "OnDeath")]
        private static class ReportOnDeath
        {
            private static void Prefix(Character __instance)
            {
                if (__instance == null) return;
                if (!OldWaysConfig.ModEnabled.Value) return;
                if (__instance.IsPlayer()) return;

                int id = __instance.GetInstanceID();
                if (!LastHits.TryGetValue(id, out LastHit last))
                {
                    Trace($"'{__instance.name}' died with no recorded tracked hit — nothing to credit.");
                    return;
                }
                LastHits.Remove(id);

                // Stale killing blow — something else finished it (drowning, fire, another player's
                // DoT). Not a trial.
                if (Time.time - last.Time > HitMemorySeconds)
                {
                    Trace($"'{__instance.name}' died {Time.time - last.Time:0.#}s after your last hit — " +
                          "something else finished it.");
                    return;
                }

                Player local = Player.m_localPlayer;
                if (local == null) return;

                // Only the player who actually landed the blow reports it, so a kill witnessed by
                // several clients is not reported several times.
                ZNetView localView = local.GetComponent<ZNetView>();
                if (localView == null || !localView.IsValid()) return;
                if (last.Attacker != localView.GetZDO().m_uid)
                {
                    Trace($"'{__instance.name}' was killed by someone else — not reporting.");
                    return;
                }

                // Anti-farm hard exclusions (docs/03).
                if (__instance.IsTamed())
                {
                    Trace($"'{__instance.name}' was tamed — no credit.");
                    return;
                }

                string prefab = PrefabName(__instance);
                int level = __instance.GetLevel();

                Trace($"reporting kill: prefab='{prefab}' level={level} skill={last.Skill} " +
                      $"bossFight={IsBossFightActive(local)} sneak={last.Sneak}");

                ProvenRpc.ReportKill(
                    local.GetPlayerID(),
                    local.GetPlayerName(),
                    prefab,
                    level,
                    last.Skill,
                    IsBossFightActive(local),
                    last.Sneak);
            }
        }

        /// <summary>
        /// A boss fight is active when a living boss is close enough to be fighting this player.
        /// Kills of a boss's adds count too — the point is rewarding combat inside the trial, not
        /// only the final blow.
        /// </summary>
        private static bool IsBossFightActive(Player player)
        {
            const float bossFightRadius = 60f;
            float sqr = bossFightRadius * bossFightRadius;
            Vector3 pos = player.transform.position;

            List<Character> all = Character.GetAllCharacters();
            for (int i = 0; i < all.Count; i++)
            {
                Character c = all[i];
                if (c == null || c.IsDead() || !c.IsBoss()) continue;
                if ((c.transform.position - pos).sqrMagnitude <= sqr) return true;
            }
            return false;
        }

        private static string PrefabName(Character character)
        {
            ZNetView view = character.GetComponent<ZNetView>();
            if (view != null && view.IsValid())
            {
                string name = view.GetPrefabName();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            // Instantiated objects are suffixed "(Clone)".
            return character.gameObject.name.Replace("(Clone)", "");
        }

        private static void Sweep()
        {
            if (Time.time - _lastSweep < 30f) return;
            _lastSweep = Time.time;

            var stale = new List<int>();
            foreach (KeyValuePair<int, LastHit> kv in LastHits)
            {
                if (Time.time - kv.Value.Time > HitMemorySeconds) stale.Add(kv.Key);
            }
            foreach (int id in stale) LastHits.Remove(id);
        }
    }
}
