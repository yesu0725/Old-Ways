using System;
using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// `proven_grant &lt;skill&gt; &lt;points&gt;` — a **testing** command to skip the ~15 starred kills
    /// it otherwise takes to reach rank 1, so a power can be verified without a grinding session.
    ///
    /// This is a deliberate hole in an otherwise closed system, so it is fenced on every side:
    ///   - flagged as a cheat, so it is disabled on servers that disallow cheats
    ///   - client-side `onlyAdmin` so it does not appear for ordinary players
    ///   - **re-verified server-side**: the server checks the sender is on the admin list before
    ///     granting anything. The client-side flag is a convenience, not the security boundary —
    ///     a modified client could call the RPC directly, so the server never trusts it.
    ///   - the grant is applied by the server through the same store as a real award, so it cannot
    ///     desync client and server views of Proven
    ///   - every use is logged at warning level with the granting player's id
    ///
    /// It grants Proven Points, not ranks, so the rank ladder and thresholds stay the single source
    /// of truth.
    /// </summary>
    internal static class ProvenGrantCommand
    {
        private const string RpcGrant = "OldWays_GrantProven";

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpc
        {
            private static void Postfix()
            {
                ZRoutedRpc.instance?.Register<ZPackage>(RpcGrant, RPC_Grant);
            }
        }

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static class Register
        {
            private static void Postfix()
            {
                new Terminal.ConsoleCommand("proven_grant",
                    "[skill] [points] - grant Proven Points for testing (admin only)",
                    Run,
                    isCheat: true, isNetwork: false, onlyServer: false, isSecret: false,
                    allowInDevBuild: false, optionsFetcher: SkillOptions,
                    alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
            }
        }

        private static System.Collections.Generic.List<string> SkillOptions()
        {
            var options = new System.Collections.Generic.List<string>();
            foreach (Skills.SkillType skill in ProvenSkills.Tracked) options.Add(skill.ToString());
            return options;
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 3)
            {
                args.Context.AddString("usage: proven_grant [skill] [points]");
                args.Context.AddString("skills: " + string.Join(", ", SkillOptions().ToArray()));
                return;
            }

            if (!Enum.TryParse(args[1], ignoreCase: true, result: out Skills.SkillType skill) ||
                !ProvenSkills.IsTracked(skill))
            {
                args.Context.AddString($"'{args[1]}' is not a Proven skill. Try: " +
                                       string.Join(", ", SkillOptions().ToArray()));
                return;
            }

            if (!int.TryParse(args[2], out int points) || points <= 0)
            {
                args.Context.AddString("points must be a positive whole number.");
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("No local player.");
                return;
            }

            var pkg = new ZPackage();
            pkg.Write(player.GetPlayerID());
            pkg.Write(player.GetPlayerName() ?? "");
            pkg.Write((int)skill);
            pkg.Write(points);
            ZRoutedRpc.instance?.InvokeRoutedRPC(RpcGrant, pkg);

            args.Context.AddString($"Requested {points} Proven Points in {skill}. " +
                                   "The server decides; run 'proven' to see the result.");
        }

        private static void RPC_Grant(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;

            long playerId = pkg.ReadLong();
            string playerName = pkg.ReadString();
            var skill = (Skills.SkillType)pkg.ReadInt();
            int points = pkg.ReadInt();

            if (!AdminAuth.IsSenderAdmin(sender))
            {
                AdminAuth.Refuse(sender, $"proven_grant ({points} points in {skill} for player {playerId})");
                return;
            }

            if (!ProvenSkills.IsTracked(skill) || points <= 0) return;

            ProvenStore.RememberName(playerId, playerName);
            int total = ProvenStore.GrantPoints(playerId, skill, points);

            Plugin.Log.LogWarning($"[Proven] ADMIN GRANT: peer {sender} granted player {playerId} " +
                                  $"'{playerName}' {points} points in {skill} (now {total}).");

            ProvenRpc.SyncTo(sender, playerId);
        }
    }
}
