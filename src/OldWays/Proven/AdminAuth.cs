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
            if (net == null) return false;

            // Listen server: the host's own routed RPC comes from their own session.
            if (sender == 0L || (Player.m_localPlayer != null && net.LocalPlayerIsAdminOrHost() &&
                                 sender == ZDOMan.GetSessionID()))
            {
                return true;
            }

            ZNetPeer peer = net.GetPeer(sender);
            if (peer?.m_socket == null) return false;

            string hostName = peer.m_socket.GetHostName();
            return !string.IsNullOrEmpty(hostName) && net.IsAdmin(hostName);
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
