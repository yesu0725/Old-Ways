using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace OldWays
{
    /// <summary>
    /// `oldways_dumpweapons` — writes every weapon's primary and secondary attack to a file.
    ///
    /// Why this exists: a weapon's secondary attack lives in **prefab data**, not in
    /// assembly_valheim.dll, so no amount of static inspection can tell us what a weapon's
    /// secondary actually is. Designing R3 perks around a guessed secondary is how the Riposte
    /// mistake happens (docs/02). This reads the real values out of ObjectDB at runtime so the
    /// design works from ground truth.
    ///
    /// Read-only and purely informational — it writes a text file and touches nothing.
    /// </summary>
    internal static class WeaponDumpCommand
    {
        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static class Register
        {
            private static void Postfix()
            {
                new Terminal.ConsoleCommand("oldways_dumpweapons",
                    "write every weapon's primary/secondary attack to BepInEx/config/OldWays/weapon_attacks.txt",
                    Run,
                    isCheat: false, isNetwork: false, onlyServer: false, isSecret: false,
                    allowInDevBuild: false, optionsFetcher: null, alwaysRefreshTabOptions: false,
                    remoteCommand: false, onlyAdmin: false);
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items == null)
            {
                args.Context.AddString("ObjectDB not loaded yet — join a world first.");
                return;
            }

            var bySkill = new Dictionary<Skills.SkillType, List<string>>();
            int count = 0;

            foreach (GameObject prefab in ObjectDB.instance.m_items)
            {
                if (prefab == null) continue;
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                ItemDrop.ItemData.SharedData shared = drop?.m_itemData?.m_shared;
                if (shared == null) continue;

                // Weapons and fists only. Two filters are needed:
                //  - m_skillType defaults to Swords, so every material and armour piece claims to
                //    be a sword; require an actual weapon item type as well.
                //  - creature attacks ("Abomination_attack1") are real items in ObjectDB but carry
                //    no inventory icon, which is the cleanest way to tell them from player gear.
                if (!IsInteresting(shared.m_skillType)) continue;
                if (!IsWeaponType(shared.m_itemType)) continue;
                if (shared.m_icons == null || shared.m_icons.Length == 0) continue;

                if (!bySkill.TryGetValue(shared.m_skillType, out List<string> lines))
                {
                    lines = new List<string>();
                    bySkill[shared.m_skillType] = lines;
                }

                lines.Add($"  {prefab.name}  [{shared.m_itemType}]");
                lines.Add($"      primary   : {Describe(shared.m_attack)}");
                lines.Add($"      secondary : {Describe(shared.m_secondaryAttack)}");
                count++;
            }

            var sb = new StringBuilder();
            sb.AppendLine("The Old Ways — weapon attack dump");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm}, {count} weapons");
            sb.AppendLine();
            sb.AppendLine("'chain' is m_attackChainLevels: how many primary attacks link into a combo.");
            sb.AppendLine("A secondary with animation '(none)' means the weapon has no distinct secondary.");
            sb.AppendLine();

            foreach (KeyValuePair<Skills.SkillType, List<string>> kv in bySkill)
            {
                sb.AppendLine($"=== {kv.Key} ===");
                foreach (string line in kv.Value) sb.AppendLine(line);
                sb.AppendLine();
            }

            string dir = Path.Combine(Paths.ConfigPath, "OldWays");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "weapon_attacks.txt");

            try
            {
                File.WriteAllText(path, sb.ToString());
                args.Context.AddString($"Wrote {count} weapons to {path}");
                Plugin.Log.LogInfo($"[Dump] wrote {count} weapons to {path}");
            }
            catch (Exception e)
            {
                args.Context.AddString($"Failed to write: {e.Message}");
            }
        }

        private static bool IsInteresting(Skills.SkillType skill)
        {
            if (skill == Skills.SkillType.Unarmed) return true;
            return ProvenSkills.IsTracked(skill);
        }

        private static bool IsWeaponType(ItemDrop.ItemData.ItemType type)
        {
            return type == ItemDrop.ItemData.ItemType.OneHandedWeapon
                || type == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || type == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
                || type == ItemDrop.ItemData.ItemType.Bow
                || type == ItemDrop.ItemData.ItemType.Shield;
        }

        private static string Describe(Attack attack)
        {
            if (attack == null) return "(none)";

            string anim = string.IsNullOrEmpty(attack.m_attackAnimation) ? "(none)" : attack.m_attackAnimation;
            return $"anim='{anim}' type={attack.m_attackType} chain={attack.m_attackChainLevels} " +
                   $"range={attack.m_attackRange:0.##} stamina={attack.m_attackStamina:0.#} " +
                   $"eitr={attack.m_attackEitr:0.#} health={attack.m_attackHealth:0.#}" +
                   (attack.m_attackProjectile != null ? $" projectile={attack.m_attackProjectile.name}" : "");
        }
    }
}
