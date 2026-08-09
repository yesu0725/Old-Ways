namespace OldWays
{
    /// <summary>
    /// The security boundary for every admin command.
    ///
    /// Terminal's `onlyAdmin` flag is client-side and therefore only a convenience — a modified
    /// client can invoke the RPC directly. The server re-checks the sender here before any command
    /// touches the store, and this is the only place that decision is made, so it cannot drift
    /// between commands as more are added.
    /// </summary>
    internal static class AdminAuth
    {
        internal static bool IsSenderAdmin(long sender)
        {
            ZNet net = ZNet.instance;
            if (net == null)
            {
                Plugin.Log.LogWarning("[AdminAuth] no ZNet — refusing.");
                return false;
            }

            // Host or single player: the command came from this machine, which is also the server.
            // GetPeer() returns null for ourselves, so peer lookup can never authorise the host —
            // this branch has to catch it. Checked by "is there a peer for this id" rather than by
            // comparing session ids, which do not always match the routed-RPC sender.
            ZNetPeer peer = net.GetPeer(sender);
            if (peer == null)
            {
                bool localAdmin = Player.m_localPlayer != null && net.LocalPlayerIsAdminOrHost();
                Plugin.Log.LogInfo($"[AdminAuth] sender {sender} has no peer — treating as host. " +
                                   $"LocalPlayerIsAdminOrHost={localAdmin}.");
                return localAdmin;
            }

            if (peer.m_socket == null)
            {
                Plugin.Log.LogWarning($"[AdminAuth] peer {sender} has no socket — refusing.");
                return false;
            }

            string hostName = peer.m_socket.GetHostName();
            bool isAdmin = !string.IsNullOrEmpty(hostName) && net.IsAdmin(hostName);
            Plugin.Log.LogInfo($"[AdminAuth] peer {sender} host='{hostName}' isAdmin={isAdmin}.");
            return isAdmin;
        }

        /// <summary>Logs and refuses. Every refusal leaves a trace, so bypass attempts are visible.</summary>
        internal static bool Refuse(long sender, string what)
        {
            Plugin.Log.LogWarning($"[Proven] REFUSED {what} from non-admin peer {sender}. " +
                                  "The client-side admin flag was bypassed or the peer is not an admin.");
            return false;
        }
    }
}
