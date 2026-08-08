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
                LocalRecord = ProvenRecord.Deserialize(pkg.ReadString());
                LocalRecordReceived = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[Proven] bad sync package: {e.Message}");
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
