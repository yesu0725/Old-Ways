using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// `oldways_dumpeffects [filter]` — writes every vanilla effect prefab name to a file.
    ///
    /// Effect prefabs live in ZNetScene, not in the assembly, so their names cannot be verified
    /// statically. This is the same tool-first approach as `oldways_dumpweapons`: read the real
    /// list at runtime rather than guessing and shipping an effect that silently never plays.
    ///
    /// Read-only.
    /// </summary>
    internal static class EffectDumpCommand
    {
        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static class Register
        {
            private static void Postfix()
            {
                new Terminal.ConsoleCommand("oldways_dumpeffects",
                    "[filter] - write vanilla effect prefab names to BepInEx/config/OldWays/effect_prefabs.txt",
                    Run,
                    isCheat: false, isNetwork: false, onlyServer: false, isSecret: false,
                    allowInDevBuild: false, optionsFetcher: null, alwaysRefreshTabOptions: false,
                    remoteCommand: false, onlyAdmin: false);
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                args.Context.AddString("ZNetScene not loaded — join a world first.");
                return;
            }

            string filter = args.Length >= 2 ? args[1] : null;

            List<string> names = scene.GetPrefabNames();
            var groups = new SortedDictionary<string, List<string>>();

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name)) continue;

                // Effect prefabs are conventionally prefixed. Anything else is a world object.
                string prefix = null;
                if (name.StartsWith("vfx_", StringComparison.OrdinalIgnoreCase)) prefix = "vfx_";
                else if (name.StartsWith("sfx_", StringComparison.OrdinalIgnoreCase)) prefix = "sfx_";
                else if (name.StartsWith("fx_", StringComparison.OrdinalIgnoreCase)) prefix = "fx_";
                if (prefix == null) continue;

                if (filter != null && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                if (!groups.TryGetValue(prefix, out List<string> list))
                {
                    list = new List<string>();
                    groups[prefix] = list;
                }
                list.Add(name);
            }

            var sb = new StringBuilder();
            sb.AppendLine("The Old Ways — vanilla effect prefabs");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm}" +
                          (filter != null ? $", filtered by '{filter}'" : ""));
            sb.AppendLine();
            sb.AppendLine("Put any of these names into the '5 - Visual Effects' config entries.");
            sb.AppendLine("Each entry takes a comma-separated list; the first name that exists is used.");
            sb.AppendLine();

            int total = 0;
            foreach (KeyValuePair<string, List<string>> kv in groups)
            {
                kv.Value.Sort();
                sb.AppendLine($"=== {kv.Key} ({kv.Value.Count}) ===");
                foreach (string n in kv.Value) sb.AppendLine("  " + n);
                sb.AppendLine();
                total += kv.Value.Count;
            }

            string dir = Path.Combine(Paths.ConfigPath, "OldWays");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "effect_prefabs.txt");

            try
            {
                File.WriteAllText(path, sb.ToString());
                args.Context.AddString($"Wrote {total} effect prefabs to {path}");
                Plugin.Log.LogInfo($"[Dump] wrote {total} effect prefabs to {path}");
            }
            catch (Exception e)
            {
                args.Context.AddString($"Failed to write: {e.Message}");
            }
        }
    }
}
