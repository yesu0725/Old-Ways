using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// **Vanish** — Knives, Proven rank 1 (docs/02).
    ///
    /// A sneak-attack kill clears the alert state of nearby creatures, letting you drop back into
    /// stealth mid-fight.
    ///
    /// Vanilla tracks alert state on every AI and un-sets it on its own terms, never on yours — so
    /// knives give you one opening per fight and then become a poor melee weapon. This turns them
    /// into an actual assassin loop: strike from hiding, disappear, strike again.
    ///
    /// Replaced the handoff's "stamina refund + muffled footsteps", which was cut because the Sneak
    /// tweak in docs/01 already grants muffled footsteps — we would have collided with ourselves.
    /// </summary>
    internal static class KnifeVanish
    {
        [HarmonyPatch(typeof(Character), "OnDeath")]
        private static class VanishOnSneakKill
        {
            private static void Prefix(Character __instance)
            {
                if (!OldWaysConfig.KnifeVanishEnabled.Value) return;
                if (__instance == null || __instance.IsPlayer()) return;

                if (!KillAttribution.TryGetKillingBlow(__instance, out Skills.SkillType skill, out bool sneak))
                    return;

                if (skill != Skills.SkillType.Knives || !sneak) return;
                if (!PowerGate.Unlocked(Skills.SkillType.Knives, 1)) return;

                Player local = Player.m_localPlayer;
                if (local == null) return;

                int cleared = ClearAlertsAround(local.transform.position,
                                                OldWaysConfig.KnifeVanishRadius.Value,
                                                __instance);

                if (OldWaysConfig.VerboseLogging.Value)
                    Plugin.Log.LogInfo($"[Vanish] sneak kill on '{__instance.name}' — {cleared} creature(s) lost track of you.");
            }
        }

        /// <summary>
        /// Drops alert state and forgets the current target on every creature in range, so they
        /// return to searching rather than continuing to chase.
        /// </summary>
        private static int ClearAlertsAround(Vector3 origin, float radius, Character exclude)
        {
            float sqr = radius * radius;
            int cleared = 0;

            List<Character> all = Character.GetAllCharacters();
            for (int i = 0; i < all.Count; i++)
            {
                Character c = all[i];
                if (c == null || c == exclude || c.IsPlayer() || c.IsDead()) continue;
                if (c.IsTamed()) continue;
                if ((c.transform.position - origin).sqrMagnitude > sqr) continue;

                BaseAI ai = c.GetComponent<BaseAI>();
                if (ai == null) continue;

                // Only the owner of a creature may steer its AI; on someone else's client this
                // would be overwritten on the next sync anyway.
                ZNetView view = c.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || !view.IsOwner()) continue;

                ai.SetAlerted(false);
                if (ai is MonsterAI monster) monster.m_targetCreature = null;
                cleared++;
            }

            return cleared;
        }
    }
}
