using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// Admin commands that target a specific player:
    ///
    ///   proven_players                              list players the server knows about
    ///   proven_set   &lt;player&gt; &lt;skill&gt; &lt;points&gt;      set an exact value
    ///   proven_reset &lt;player&gt; [skill]               clear one skill, or everything incl. tier
    ///
    /// `player` accepts a name, a unique partial name, a raw player id, or `me`.
    ///
    /// All three are destructive or disclosive, so they follow the same rules as `proven_grant`
    /// (docs/07): cheat-flagged, admin-only client-side for convenience, and — the actual boundary
    /// — **re-verified server-side** through <see cref="AdminAuth"/>, because a modified client can
    /// call the RPC directly. Every use and every refusal is logged at warning level.
    ///
    /// The server does the work and sends back a reply, so the admin sees what actually happened
    /// rather than what their client assumed.
    /// </summary>
    internal static class ProvenAdminCommands
    {
        private const string RpcSet = "OldWays_AdminSet";
        private const string RpcReset = "OldWays_AdminReset";
        private const string RpcList = "OldWays_AdminList";
        private const string RpcReply = "OldWays_AdminReply";

        /// <summary>Where to print the server's reply. Set when a command runs.</summary>
        private static Terminal _lastContext;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpcs
        {
            private static void Postfix()
            {
                if (ZRoutedRpc.instance == null) return;
                ZRoutedRpc.instance.Register<ZPackage>(RpcSet, RPC_Set);
                ZRoutedRpc.instance.Register<ZPackage>(RpcReset, RPC_Reset);
                ZRoutedRpc.instance.Register<ZPackage>(RpcList, RPC_List);
                ZRoutedRpc.instance.Register<ZPackage>(RpcReply, RPC_Reply);
            }
        }

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static class Register
        {
            private static void Postfix()
            {
                Command("proven_players", "list players the server has Proven records for", RunList);
                Command("proven_set", "[player] [skill] [points] - set a player's Proven Points", RunSet);
                Command("proven_reset", "[player] [skill?] - clear a player's Proven (all skills and tier if no skill given)", RunReset);
            }

            private static void Command(string name, string help, Terminal.ConsoleEvent action)
            {
                new Terminal.ConsoleCommand(name, help, action,
                    isCheat: true, isNetwork: false, onlyServer: false, isSecret: false,
                    allowInDevBuild: false, optionsFetcher: null, alwaysRefreshTabOptions: false,
                    remoteCommand: false, onlyAdmin: true);
            }
        }

        // ---- client side ----------------------------------------------------------------

        private static bool Ready(Terminal.ConsoleEventArgs args)
        {
            _lastContext = args.Context;
            if (ZRoutedRpc.instance == null || Player.m_localPlayer == null)
            {
                args.Context.AddString("Not in a world.");
                return false;
            }
            return true;
        }

        private static void RunList(Terminal.ConsoleEventArgs args)
        {
            if (!Ready(args)) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(RpcList, new ZPackage());
        }

        private static void RunSet(Terminal.ConsoleEventArgs args)
        {
            if (!Ready(args)) return;

            if (args.Length < 4)
            {
                args.Context.AddString("usage: proven_set [player] [skill] [points]");
                args.Context.AddString("skills: " + SkillList());
                return;
            }

            if (!ParseSkill(args[2], args.Context, out Skills.SkillType skill)) return;
            if (!int.TryParse(args[3], out int points) || points < 0)
            {
                args.Context.AddString("points must be zero or a positive whole number.");
                return;
            }

            var pkg = new ZPackage();
            pkg.Write(ResolveSelf(args[1]));
            pkg.Write((int)skill);
            pkg.Write(points);
            ZRoutedRpc.instance.InvokeRoutedRPC(RpcSet, pkg);
        }

        private static void RunReset(Terminal.ConsoleEventArgs args)
        {
            if (!Ready(args)) return;

            if (args.Length < 2)
            {
                args.Context.AddString("usage: proven_reset [player] [skill?]");
                args.Context.AddString("Without a skill this clears every track AND the player's progression tier.");
                return;
            }

            int skillId = -1;
            if (args.Length >= 3)
            {
                if (!ParseSkill(args[2], args.Context, out Skills.SkillType skill)) return;
                skillId = (int)skill;
            }

            var pkg = new ZPackage();
            pkg.Write(ResolveSelf(args[1]));
            pkg.Write(skillId);
            ZRoutedRpc.instance.InvokeRoutedRPC(RpcReset, pkg);
        }

        /// <summary>`me` is resolved client-side to the caller's own id, since only the client knows it.</summary>
        private static string ResolveSelf(string target)
        {
            if (!string.Equals(target, "me", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(target, "self", StringComparison.OrdinalIgnoreCase))
            {
                return target;
            }
            return Player.m_localPlayer.GetPlayerID().ToString();
        }

        private static bool ParseSkill(string raw, Terminal context, out Skills.SkillType skill)
        {
            if (Enum.TryParse(raw, ignoreCase: true, result: out skill) && ProvenSkills.IsTracked(skill))
                return true;

            context.AddString($"'{raw}' is not a Proven skill. Try: {SkillList()}");
            return false;
        }

        private static string SkillList()
        {
            var names = new List<string>();
            foreach (Skills.SkillType s in ProvenSkills.Tracked) names.Add(s.ToString());
            return string.Join(", ", names.ToArray());
        }

        // ---- server side ----------------------------------------------------------------

        private static bool ServerReady(long sender, string what)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return false;
            if (!AdminAuth.IsSenderAdmin(sender))
            {
                AdminAuth.Refuse(sender, what);
                Reply(sender, "Refused: you are not a server admin.");
                return false;
            }
            return true;
        }

        private static void RPC_List(long sender, ZPackage pkg)
        {
            if (!ServerReady(sender, "proven_players")) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== Proven records on this server ===");

            int count = 0;
            foreach (KeyValuePair<long, ProvenRecord> kv in ProvenStore.AllRecords())
            {
                ProvenRecord r = kv.Value;
                var tracks = new List<string>();
                foreach (Skills.SkillType s in ProvenSkills.Tracked)
                {
                    int pts = r.GetPoints(s);
                    if (pts > 0) tracks.Add($"{s} {pts}");
                }

                sb.AppendLine($"{PlayerRegistry.NameFor(kv.Key)} ({kv.Key})  tier {r.Tier}  " +
                              $"presence {r.Presence()}  " +
                              (tracks.Count > 0 ? string.Join(", ", tracks.ToArray()) : "no Proven yet"));
                count++;
            }

            // Players known but with no record yet — still valid targets.
            foreach (KeyValuePair<long, string> kv in PlayerRegistry.Known())
            {
                bool hasRecord = false;
                foreach (KeyValuePair<long, ProvenRecord> r in ProvenStore.AllRecords())
                {
                    if (r.Key == kv.Key) { hasRecord = true; break; }
                }
                if (!hasRecord) sb.AppendLine($"{kv.Value} ({kv.Key})  no record yet");
            }

            if (count == 0) sb.AppendLine("(none)");
            Reply(sender, sb.ToString());
        }

        private static void RPC_Set(long sender, ZPackage pkg)
        {
            if (!ServerReady(sender, "proven_set")) return;

            string target = pkg.ReadString();
            var skill = (Skills.SkillType)pkg.ReadInt();
            int points = pkg.ReadInt();

            if (!PlayerRegistry.Resolve(target, out long playerId, out string problem))
            {
                Reply(sender, problem);
                return;
            }
            if (!ProvenSkills.IsTracked(skill)) return;

            int before = ProvenStore.SetPoints(playerId, skill, points);
            string name = PlayerRegistry.NameFor(playerId);

            Plugin.Log.LogWarning($"[Proven] ADMIN SET: peer {sender} set {name} ({playerId}) " +
                                  $"{skill} from {before} to {points} PP.");
            Reply(sender, $"{name} ({playerId}): {skill} {before} -> {points} PP " +
                          $"(rank {ProvenRecord.RankForPoints(points)}).");

            PushToTarget(playerId);
        }

        private static void RPC_Reset(long sender, ZPackage pkg)
        {
            if (!ServerReady(sender, "proven_reset")) return;

            string target = pkg.ReadString();
            int skillId = pkg.ReadInt();

            if (!PlayerRegistry.Resolve(target, out long playerId, out string problem))
            {
                Reply(sender, problem);
                return;
            }

            Skills.SkillType? skill = skillId >= 0 ? (Skills.SkillType?)skillId : null;
            if (skill.HasValue && !ProvenSkills.IsTracked(skill.Value)) return;

            string summary = ProvenStore.ResetPlayer(playerId, skill);
            string name = PlayerRegistry.NameFor(playerId);

            Plugin.Log.LogWarning($"[Proven] ADMIN RESET: peer {sender} reset {name} ({playerId}): {summary}");
            Reply(sender, $"{name} ({playerId}): {summary}");

            PushToTarget(playerId);
        }

        /// <summary>
        /// Push the new value to the affected player if they are online, so their trial log does
        /// not keep showing a number the server no longer agrees with.
        /// </summary>
        private static void PushToTarget(long playerId)
        {
            if (PlayerRegistry.TryGetPeerFor(playerId, out long peerUid))
                ProvenRpc.SyncTo(peerUid, playerId);
        }

        private static void Reply(long peer, string message)
        {
            var pkg = new ZPackage();
            pkg.Write(message ?? "");
            ZRoutedRpc.instance?.InvokeRoutedRPC(peer, RpcReply, pkg);
        }

        private static void RPC_Reply(long sender, ZPackage pkg)
        {
            string message = pkg.ReadString();
            if (_lastContext != null) _lastContext.AddString(message);
            else Plugin.Log.LogInfo("[Proven] " + message);
        }
    }
}
