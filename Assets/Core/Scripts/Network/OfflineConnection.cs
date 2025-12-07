using System;
using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Server
{
    public class OfflineNetworkConnection : INetworkConnection
    {
        public NetworkConnectionStatus Status { get; private set; } =
            NetworkConnectionStatus.Disconnected;

        public IEnumerator Initialize()
        {
            // Should this be connected or disconnected or something?
            Status = NetworkConnectionStatus.Connected;
            yield break;
        }

        public void Dispose() { }
    }
}
