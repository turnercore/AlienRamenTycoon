using System;
using System.Collections;
using System.Threading.Tasks;
using Core;
using UnityEditor.PackageManager;

namespace Server
{
    public interface INetworkService : IDisposable
    {
        public Action<NetworkConnectionStatus> OnNetworkStatusChanged { get; set; }

        INetworkConnection Connection { get; }

        /// <summary>
        /// Initialize the service (and its connection) during boot.
        /// </summary>
        IEnumerator Initialize();

        /// <summary>
        /// Convenience status – usually just proxies Connection.Status.
        /// </summary>
        NetworkConnectionStatus Status { get; }

        /// <summary>
        /// Fire an RPC and get raw JSON back.
        /// </summary>
        Task<string> CallRpcAsync(string id, string payloadJson);

        /// <summary>
        /// Fire an RPC with a typed payload and get a typed result.
        /// </summary>
        Task<TResponse> CallRpcAsync<TResponse, TRequest>(string id, TRequest payload);
    }
}
