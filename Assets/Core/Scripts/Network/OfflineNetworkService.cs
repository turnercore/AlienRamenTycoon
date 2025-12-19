using System;
using System.Collections;
using System.Threading.Tasks;
using Core;

namespace Server
{
    public class OfflineNetworkService : INetworkService
    {
        private readonly INetworkConnection _connection = new OfflineNetworkConnection(); // your existing "do nothing" connection
        private Action<NetworkConnectionStatus> onNetworkStatusChanged;

        public INetworkConnection Connection => _connection;
        public NetworkConnectionStatus Status => _connection.Status;
        public Action<NetworkConnectionStatus> OnNetworkStatusChanged
        {
            get => onNetworkStatusChanged;
            set
            {
                if (onNetworkStatusChanged != null)
                {
                    _connection.OnStatusChanged -= onNetworkStatusChanged;
                }

                onNetworkStatusChanged = value;

                if (onNetworkStatusChanged != null)
                {
                    _connection.OnStatusChanged += onNetworkStatusChanged;
                    onNetworkStatusChanged.Invoke(_connection.Status);
                }
            }
        }

        public IEnumerator Initialize()
        {
            yield return _connection.Initialize();
        }

        public Task<string> CallRpcAsync(string id, string payloadJson)
        {
            // Offline stub
            return Task.FromResult<string>(null);
        }

        public Task<TResponse> CallRpcAsync<TResponse, TRequest>(string id, TRequest payload)
        {
            return Task.FromResult<TResponse>(default);
        }

        public void Dispose()
        {
            if (onNetworkStatusChanged != null)
            {
                _connection.OnStatusChanged -= onNetworkStatusChanged;
                onNetworkStatusChanged = null;
            }
            _connection.Dispose();
        }
    }
}
