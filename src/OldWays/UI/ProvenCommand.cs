using System.Text;
using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// `proven` console command — prints the local player's full trial log.
    ///
    /// This is the verification tool for the Phase 1 acceptance test in docs/09: run `proven`,
    /// then `raiseskill swords 100`, then `proven` again. The vanilla skill moves; Proven does not.
    ///
    /// Read-only by design. There is deliberately no command to grant Proven — that would be a
    /// back door into the exact thing the system exists to prevent.
    /// </summary>
    internal static class ProvenCommand
    {
        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        private static class Register
        {
            private static void Postfix()
            {
                new Terminal.ConsoleCommand("proven", "show your Old Ways trial log", Run,
                    isCheat: false, isNetwork: false, onlyServer: false, isSecret: false,
                    allowInDevBuild: false, optionsFetcher: null, alwaysRefreshTabOptions: false,
                    remoteCommand: false, onlyAdmin: false);
            }
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("No local player.");
                return;
            }

            ProvenRecord record = ProvenRpc.RecordForLocalPlayer();
            bool authoritative = ZNet.instance != null && ZNet.instance.IsServer();

            var sb = new StringBuilder();
            sb.AppendLine("=== The Old Ways — trial log ===");
            sb.AppendLine($"Old Ways Presence: {record.Presence()} ({ProvenSkills.RankName(record.Presence())})");
            if (!authoritative && !ProvenRpc.LocalRecordReceived)
                sb.AppendLine("(no sync from the server yet — earn Proven once to populate this)");
            sb.AppendLine();

            Skills skills = player.GetSkills();

            foreach (Skills.SkillType type in ProvenSkills.Tracked)
            {
                int points = record.GetPoints(type);
                int rank = record.GetRank(type);
                int next = ProvenRecord.NextThreshold(rank);
                float vanillaLevel = skills.GetSkillLevel(type);

                string progress = next > 0 ? $"{points}/{next}" : $"{points} (max)";
                sb.AppendLine($"{type,-16} {ProvenSkills.RankName(rank),-9} {progress,-12} vanilla skill {vanillaLevel:0}");

                foreach (var power in ProvenSkills.PowersFor(type))
                {
                    string state = rank >= power.Key ? "UNLOCKED" : $"rank {power.Key}";
                    sb.AppendLine($"    [{state,-8}] {power.Value}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Trials: {ProvenSkills.TrialsFor(Skills.SkillType.Swords)}");
            sb.AppendLine($"Requires vanilla skill {OldWaysConfig.SoftSkillPrerequisite.Value}+ alongside rank 1.");

            args.Context.AddString(sb.ToString());
        }
    }
}
