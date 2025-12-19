using System;
using System.Collections;
using System.Threading.Tasks;
using Core;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

namespace Server
{
    public class NakamaNetworkService : INetworkService
    {
        public INetworkConnection Connection => _connection;
        public NetworkConnectionStatus Status => _connection.Status;

        private readonly NakamaConnection _connection;
        private Action<NetworkConnectionStatus> onNetworkStatusChanged;
        private float pollSocketInterval = 0.1f;

        public NakamaNetworkService(NakamaConnection connection)
        {
            _connection = connection;
        }

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
            Debug.Log("NakamaNetworkService.Initialize() started");
            // delegate boot sequence to the connection
            yield return _connection.Initialize();
            Debug.Log($"NakamaConnection.Initialize() completed. Status: {_connection.Status}");

            // Optionally wait until connection is no longer "Connecting"
            int timeout = 0;
            while (_connection.Status == NetworkConnectionStatus.Connecting && timeout < 300)
            {
                yield return null;
                timeout++;
            }

            Debug.Log(
                $"NakamaNetworkService.Initialize() finished. Final status: {_connection.Status}, Timeout reached: {timeout >= 300}"
            );
            // You could do extra setup here if needed (e.g. join default presence channel)
        }

        public async Task<string> CallRpcAsync(string id, string payloadJson)
        {
            if (!EnsureConnected())
            {
                Debug.LogError($"NakamaNetworkService: CallRpcAsync('{id}') when not connected.");
                return null;
            }

            var client = _connection.Client; // expose these as internal/properties on NakamaConnection
            var session = _connection.Session;

            try
            {
                var rpc = await client.RpcAsync(session, id, payloadJson);
                return rpc.Payload;
            }
            catch (ApiResponseException apiEx)
            {
                Debug.LogError(
                    $"NakamaNetworkService RPC error [{id}]: {apiEx.StatusCode} {apiEx.Message}"
                );
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"NakamaNetworkService RPC exception [{id}]: {ex}");
                return null;
            }
        }

        public async Task<TResponse> CallRpcAsync<TResponse, TRequest>(string id, TRequest payload)
        {
            var json = payload != null ? JsonConvert.SerializeObject(payload) : "{}";

            var resultJson = await CallRpcAsync(id, json);
            if (string.IsNullOrEmpty(resultJson))
            {
                return default;
            }

            try
            {
                return JsonConvert.DeserializeObject<TResponse>(resultJson);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"NakamaNetworkService: Failed to deserialize RPC response for {id}: {ex}"
                );
                return default;
            }
        }

        public void Dispose()
        {
            if (onNetworkStatusChanged != null)
            {
                _connection.OnStatusChanged -= onNetworkStatusChanged;
                onNetworkStatusChanged = null;
            }
            _connection?.Dispose();
        }

        private bool EnsureConnected()
        {
            return _connection != null && _connection.Status == NetworkConnectionStatus.Connected;
        }
    }
}
