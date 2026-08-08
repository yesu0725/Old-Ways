using System.Collections.Generic;

namespace OldWays
{
    /// <summary>
    /// Server-side map of who is who.
    ///
    /// Proven is keyed by <see cref="Player.GetPlayerID"/>, but the server only ever sees that id
    /// when a client sends it — peers carry a name and a peer uid, not a player id. Without this,
    /// an admin could not name a target. Clients announce themselves on spawn; the server also
    /// remembers ids it has seen through awards, so offline players stay addressable.
    /// </summary>
    internal static class PlayerRegistry
    {
        private static readonly Dictionary<long, string> NamesByPlayerId = new();
        private static readonly Dictionary<long, long> PlayerIdByPeer = new();

        internal static void Register(long peerUid, long playerId, string name)
        {
            if (!string.IsNullOrEmpty(name)) NamesByPlayerId[playerId] = name;
            if (peerUid != 0L) PlayerIdByPeer[peerUid] = playerId;
        }

        internal static void RememberName(long playerId, string name)
        {
            if (!string.IsNullOrEmpty(name)) NamesByPlayerId[playerId] = name;
        }

        internal static string NameFor(long playerId) =>
            NamesByPlayerId.TryGetValue(playerId, out string n) ? n : "?";

        internal static bool TryGetPeerFor(long playerId, out long peerUid)
        {
            foreach (KeyValuePair<long, long> kv in PlayerIdByPeer)
            {
                if (kv.Value == playerId) { peerUid = kv.Key; return true; }
            }
            peerUid = 0L;
            return false;
        }

        internal static IEnumerable<KeyValuePair<long, string>> Known() => NamesByPlayerId;

        /// <summary>
        /// Resolves an admin's target string to a player id. Accepts a raw player id, an exact
        /// name, or an unambiguous partial name. Returns false with a reason the admin can act on
        /// rather than a bare failure.
        /// </summary>
        internal static bool Resolve(string query, out long playerId, out string problem)
        {
            playerId = 0L;
            problem = null;

            if (string.IsNullOrEmpty(query))
            {
                problem = "no player given";
                return false;
            }

            if (long.TryParse(query, out long asId) && NamesByPlayerId.ContainsKey(asId))
            {
                playerId = asId;
                return true;
            }

            var exact = new List<long>();
            var partial = new List<long>();
            foreach (KeyValuePair<long, string> kv in NamesByPlayerId)
            {
                if (string.Equals(kv.Value, query, System.StringComparison.OrdinalIgnoreCase)) exact.Add(kv.Key);
                else if (kv.Value.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0) partial.Add(kv.Key);
            }

            List<long> hits = exact.Count > 0 ? exact : partial;

            if (hits.Count == 0)
            {
                // A raw id the server has never seen is still usable — the player may simply have
                // no record yet. Only refuse things that are not ids at all.
                if (long.TryParse(query, out long unknownId))
                {
                    playerId = unknownId;
                    return true;
                }
                problem = $"no player matching '{query}'. Use 'proven_players' to list known players.";
                return false;
            }

            if (hits.Count > 1)
            {
                var names = new List<string>();
                foreach (long id in hits) names.Add($"{NameFor(id)} ({id})");
                problem = $"'{query}' matches several players: {string.Join(", ", names.ToArray())}. Be more specific or use the id.";
                return false;
            }

            playerId = hits[0];
            return true;
        }

        internal static void Clear()
        {
            NamesByPlayerId.Clear();
            PlayerIdByPeer.Clear();
        }
    }
}
