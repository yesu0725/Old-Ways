using System;
using System.Collections.Generic;
using HarmonyLib;

namespace OldWays
{
    /// <summary>
    /// Client -> server kill reports, server -> client Proven sync.
    ///
    /// WHY A REPORT AND NOT PURE SERVER DETECTION
    /// In Valheim a creature's ZDO is owned by whichever client is engaged with it, so its death
    /// runs on that client, not on the dedicated server. There is no server-side hook that sees
    /// every kill. The owning client therefore reports the kill and the server decides what it is
    /// worth.
    ///
    /// WHAT THIS DOES AND DOES NOT BUY
    /// The server owns every rule that matters: weights, tier gating, diminishing returns,
    /// thresholds, storage. A player cannot grant themselves Proven with `raiseskill`, by editing
    /// a local file, or by tampering with config — which is what docs/03 set out to stop. It does
    /// NOT make forged reports impossible: a modified client could claim kills it did not make.
    /// Closing that fully is not achievable within Valheim's ownership model. Rate limiting below
    /// blunts the crude version of it. This limitation is recorded in docs/07 rather than papered
    /// over.
    /// </summary>
    internal static class ProvenRpc
    {
        private const string RpcReportKill = "OldWays_ReportKill";
        private const string RpcSyncProven = "OldWays_SyncProven";
        private const string RpcAnnounce = "OldWays_Announce";

        /// <summary>The local player's Proven, pushed by the server. Display only — never authoritative.</summary>
        internal static ProvenRecord LocalRecord { get; private set; } = new();

        internal static bool LocalRecordReceived { get; private set; }

        // Crude rate limit: a report burst far beyond plausible combat is dropped.
        private const int MaxReportsPerWindow = 30;
        private static readonly TimeSpan RateWindow = TimeSpan.FromSeconds(10);
        private static readonly Dictionary<long, Queue<DateTime>> ReportTimes = new();

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class RegisterRpcs
        {
            private static void Postfix(ZNet __instance)
            {
                if (ZRoutedRpc.instance == null) return;

                ZRoutedRpc.instance.Register<ZPackage>(RpcReportKill, RPC_ReportKill);
                ZRoutedRpc.instance.Register<ZPackage>(RpcSyncProven, RPC_SyncProven);
                ZRoutedRpc.instance.Register<ZPackage>(RpcAnnounce, RPC_Announce);

                if (__instance.IsServer())
                {
                    ProvenStore.Load();
                    Plugin.Log.LogInfo("[Proven] server authority online; store loaded.");
                }
            }
        }

        /// <summary>Persist on world save so Proven survives a crash as well as a clean shutdown.</summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldAndPlayerProfiles))]
        private static class SaveWithWorld
        {
            private static void Postfix(ZNet __instance)
            {
                if (__instance.IsServer()) ProvenStore.Save();
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Shutdown))]
        private static class SaveOnShutdown
        {
            private static void Prefix(ZNet __instance)
            {
                if (__instance.IsServer()) ProvenStore.Save(force: true);
                LocalRecord = new ProvenRecord();
                LocalRecordReceived = false;
                Effects.Clear();     // prefab lookups belong to the world we are leaving
            }
        }

        // ---- identity handshake ---------------------------------------------------------

        /// <summary>
        /// Clients announce themselves on spawn so the server can map peer -> player id -> name.
        /// Without this an admin has no way to name a target: peers carry a name and a peer uid,
        /// but Proven is keyed by player id, which only the client knows.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class AnnounceOnSpawn
        {
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer) return;
                if (ZRoutedRpc.instance == null) return;

                var pkg = new ZPackage();
                pkg.Write(__instance.GetPlayerID());
                pkg.Write(__instance.GetPlayerName() ?? "");
                ZRoutedRpc.instance.InvokeRoutedRPC(RpcAnnounce, pkg);

                // Ask for our own record so the trial log is populated before earning anything.
                LocalRecordReceived = false;
            }
        }

        private static void RPC_Announce(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;

            try
            {
                long playerId = pkg.ReadLong();
                string name = pkg.ReadString();
                PlayerRegistry.Register(sender, playerId, name);
                Plugin.Log.LogInfo($"[Proven] peer {sender} is player {playerId} '{name}'.");

                // Push their current standing so the skills screen is right from the moment they
                // spawn, rather than blank until their first kill.
                SyncTo(sender, playerId);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Proven] bad announce from peer {sender}: {e.Message}");
            }
        }

        // ---- client -> server -----------------------------------------------------------

        internal static void ReportKill(long playerId, string playerName, string victimPrefab,
                                        int victimLevel, Skills.SkillType skill,
                                        bool duringBossFight, bool sneakKill)
        {
            if (ZRoutedRpc.instance == null) return;

            var pkg = new ZPackage();
            pkg.Write(playerId);
            pkg.Write(playerName ?? "");
            pkg.Write(victimPrefab ?? "");
            pkg.Write(victimLevel);
            pkg.Write((int)skill);
            pkg.Write(duringBossFight);
            pkg.Write(sneakKill);

            // On a listen server (host playing) this resolves locally rather than over the wire.
            ZRoutedRpc.instance.InvokeRoutedRPC(RpcReportKill, pkg);
        }

        private static void RPC_ReportKill(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
            if (!OldWaysConfig.ModEnabled.Value) return;

            try
            {
                long playerId = pkg.ReadLong();
                string playerName = pkg.ReadString();
                string victimPrefab = pkg.ReadString();
                int victimLevel = pkg.ReadInt();
                var skill = (Skills.SkillType)pkg.ReadInt();
                bool duringBossFight = pkg.ReadBool();
                bool sneakKill = pkg.ReadBool();

                if (!RateLimitOk(sender))
                {
                    Plugin.Log.LogWarning($"[Proven] rate-limited kill reports from peer {sender} (player {playerId}).");
                    return;
                }

                ProvenStore.RememberName(playerId, playerName);

                int awarded = ProvenStore.AwardKill(playerId, victimPrefab, victimLevel, skill,
                                                    duringBossFight, sneakKill);
                if (awarded > 0) SyncTo(sender, playerId);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Proven] malformed kill report from peer {sender}: {e.Message}");
            }
        }

        private static bool RateLimitOk(long peer)
        {
            DateTime now = DateTime.UtcNow;
            if (!ReportTimes.TryGetValue(peer, out Queue<DateTime> times))
            {
                times = new Queue<DateTime>();
                ReportTimes[peer] = times;
            }

            while (times.Count > 0 && now - times.Peek() > RateWindow) times.Dequeue();
            if (times.Count >= MaxReportsPerWindow) return false;

            times.Enqueue(now);
            return true;
        }

        // ---- server -> client -----------------------------------------------------------

        internal static void SyncTo(long peer, long playerId)
        {
            if (ZRoutedRpc.instance == null) return;

            ProvenRecord record = ProvenStore.Get(playerId);
            var pkg = new ZPackage();
            pkg.Write(record.Serialize());
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, RpcSyncProven, pkg);
        }

        private static void RPC_SyncProven(long sender, ZPackage pkg)
        {
            try
            {
                ProvenRecord previous = LocalRecord;
                bool hadRecord = LocalRecordReceived;

                LocalRecord = ProvenRecord.Deserialize(pkg.ReadString());
                LocalRecordReceived = true;

                // The server is the only thing that knows a rank was earned, and it learns it in
                // the same breath as awarding the points. Announce it here, where the new values
                // land, rather than sending a second message that could arrive out of order.
                //
                // Skipped on the first sync of a session: that one carries everything you already
                // had, and celebrating it would fire on every login.
                if (hadRecord) AnnounceRankUps(previous, LocalRecord);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Proven] bad sync package: {e.Message}");
            }
        }

        private static void AnnounceRankUps(ProvenRecord before, ProvenRecord after)
        {
            Player local = Player.m_localPlayer;
            if (local == null) return;

            foreach (Skills.SkillType skill in ProvenSkills.Tracked)
            {
                int oldRank = before.GetRank(skill);
                int newRank = after.GetRank(skill);
                if (newRank <= oldRank) continue;

                local.Message(MessageHud.MessageType.TopLeft,
                    $"Your hands remember. {skill} — {ProvenSkills.RankName(newRank)}.", 0, null);

                Effects.SpawnOn(OldWaysConfig.FxRankUp.Value, local);

                Plugin.Log.LogInfo($"[Proven] rank up: {skill} {oldRank} -> {newRank} " +
                                   $"({ProvenSkills.RankName(newRank)}).");
            }
        }

        /// <summary>
        /// On a host/listen server the local player is also the authority, so read straight from
        /// the store instead of waiting for a round trip that never happens.
        /// </summary>
        internal static ProvenRecord RecordForLocalPlayer()
        {
            if (ZNet.instance != null && ZNet.instance.IsServer() && Player.m_localPlayer != null)
                return ProvenStore.Get(Player.m_localPlayer.GetPlayerID());

            return LocalRecord;
        }
    }
}
