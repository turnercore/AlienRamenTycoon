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
        private NetworkConnectionStatus status = NetworkConnectionStatus.Disconnected;

        public Action<NetworkConnectionStatus> OnStatusChanged { get; set; }

        public NetworkConnectionStatus Status
        {
            get => status;
            private set
            {
                if (status == value)
                {
                    return;
                }

                status = value;
                OnStatusChanged?.Invoke(status);
            }
        }

        public IEnumerator Initialize()
        {
            // Should this be connected or disconnected or something?
            Status = NetworkConnectionStatus.Connected;
            yield break;
        }

        public void Dispose()
        {
            Status = NetworkConnectionStatus.Disconnected;
            OnStatusChanged = null;
        }
    }
}
