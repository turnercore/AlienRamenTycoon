using System;
using System.Collections;
using Core;

namespace Server
{
    public enum NetworkConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Other,
    }

    public interface INetworkConnection : IDisposable
    {
        Action<NetworkConnectionStatus> OnStatusChanged { get; set; }
        NetworkConnectionStatus Status { get; }

        /// <summary>
        /// Start up the connection (load settings, connect, healthcheck, etc).
        /// </summary>
        IEnumerator Initialize();
    }
}
