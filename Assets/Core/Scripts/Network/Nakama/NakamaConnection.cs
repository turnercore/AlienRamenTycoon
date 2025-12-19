using System;
using System.Collections;
using System.Net.WebSockets;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Nakama;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Server
{
    public class NakamaConnection : INetworkConnection, IDisposable
    {
        private static readonly int[] reconnectDelaysMs = new[]
        {
            200,
            500,
            1000,
            1500,
            2000,
            5000,
            10000,
            30000,
            60000,
            120000,
        };

        private NetworkConnectionStatus status;
        private SynchronizationContext mainThreadContext;
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
                Debug.Log($"Nakama: status changed -> {status}");
                DispatchStatusChanged(status);
            }
        }

        private NakamaSettings settings;
        private readonly AddressablesHandleHelper handles = new();
        private CancellationTokenSource heartbeatCts;
        private CancellationTokenSource reconnectCts;

        public IClient Client { get; private set; }
        public ISession Session { get; private set; }
        public ISocket Socket { get; private set; }

        public string[] errorCodes;

        private Action socketConnectedHandler;
        private Action<string> socketClosedHandler;
        private Action<Exception> socketErrorHandler;

        private const string HealthcheckRpcId = "healthcheck";
        private readonly string _settingsAddress = "Assets/Game/Settings/NakamaSettings.asset";

        public AssetReferenceT<NakamaSettings> SettingsAddress;

        public void Dispose()
        {
            handles.Dispose();
            StopHeartbeat();
            StopReconnectLoop();
            CloseConnectionAsync()
                .Forget(ex => Debug.LogError($"Nakama: error during Dispose: {ex}"));
            errorCodes = null;
            OnStatusChanged = null;
        }

        private async Task CloseConnectionAsync()
        {
            StopHeartbeat();
            StopReconnectLoop();
            if (Socket != null)
            {
                if (socketConnectedHandler != null)
                {
                    Socket.Connected -= socketConnectedHandler;
                }

                if (socketClosedHandler != null)
                {
                    Socket.Closed -= socketClosedHandler;
                }

                if (socketErrorHandler != null)
                {
                    Socket.ReceivedError -= socketErrorHandler;
                }
            }

            if (Socket != null && Socket.IsConnected)
            {
                try
                {
                    await Socket.CloseAsync();
                }
                catch (WebSocketException)
                {
                    // Ignore forced close during shutdown.
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Nakama: error while closing socket: {ex}");
                }
            }
            Socket = null;
            Session = null;
            Client = null;
            Status = NetworkConnectionStatus.Disconnected;
        }

        // ---------------------------------------------------------------------
        // Initialize: load settings via Addressables, then run full connect Task
        // ---------------------------------------------------------------------
        public IEnumerator Initialize()
        {
            mainThreadContext = SynchronizationContext.Current;
            Debug.Log("NakamaConnection.Initialize() started");

            // Load settings via Addressables
            AsyncOperationHandle<NakamaSettings> settingsHandle;
            if (SettingsAddress == null)
            {
                Debug.Log("SettingsAddress is null, using default: " + _settingsAddress);
                settingsHandle = handles.LoadAssetAsync<NakamaSettings>(_settingsAddress);
            }
            else
            {
                Debug.Log("Loading NakamaSettings from SettingsAddress");
                settingsHandle = handles.LoadAssetAsync<NakamaSettings>(SettingsAddress);
            }

            yield return new WaitUntil(() => settingsHandle.IsDone);
            Debug.Log($"NakamaSettings load complete. Status: {settingsHandle.Status}");

            if (settingsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { "nakama_settings_load_failed" };
                Debug.LogError("Nakama: Failed to load NakamaSettings");
                yield break;
            }

            settings = settingsHandle.Result;
            Debug.Log("NakamaSettings loaded successfully. Starting connect...");

            // Run the connect task and wait for it here so we don't rely on async void.
            var connectTask = TryToConnectAsync();
            while (!connectTask.IsCompleted)
            {
                yield return null;
            }

            if (connectTask.IsFaulted)
            {
                Debug.LogError($"Nakama: TryToConnectAsync faulted: {connectTask.Exception}");
                Status = NetworkConnectionStatus.Error;
            }

            Debug.Log($"NakamaConnection.Initialize() completed. Final status: {Status}");
        }

        private void DispatchStatusChanged(NetworkConnectionStatus currentStatus)
        {
            if (mainThreadContext == null)
            {
                OnStatusChanged?.Invoke(currentStatus);
                return;
            }

            mainThreadContext.Post(_ => OnStatusChanged?.Invoke(currentStatus), null);
        }

        // ---------------------------------------------------------------------
        // Full connect flow: client -> auth -> healthcheck -> socket
        // ---------------------------------------------------------------------
        private async Task TryToConnectAsync()
        {
            Debug.Log("NakamaConnection.TryToConnectAsync() called");

            if (settings == null)
            {
                Debug.LogError("Nakama: TryToConnectAsync called before settings were loaded.");
                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { "nakama_settings_null" };
                return;
            }

            Status = NetworkConnectionStatus.Connecting;
            Debug.Log("Status set to Connecting. Starting async connection...");

            // --- Create client ------------------------------------------------
            try
            {
                var scheme = string.IsNullOrEmpty(settings.Scheme) ? "http" : settings.Scheme;
                var host = settings.Host;
                var port = settings.Port > 0 ? settings.Port : (scheme == "https" ? 443 : 7350);

                // Trim in case there is any stray whitespace in the asset.
                var serverKeyRaw = string.IsNullOrEmpty(settings.ServerKey)
                    ? "defaultkey"
                    : settings.ServerKey;
                var serverKey = serverKeyRaw.Trim();

                Debug.Log(
                    $"Nakama: preparing client\n"
                        + $"  scheme='{scheme}'\n"
                        + $"  host='{host}'\n"
                        + $"  port={port}\n"
                        + $"  serverKeyRaw='{serverKeyRaw}' (len={serverKeyRaw.Length})\n"
                        + $"  serverKeyTrimmed='{serverKey}' (len={serverKey.Length})"
                );

                // Optional: ping endpoint to verify we're hitting the right place
                await DebugPingEndpointAsync(scheme, host, port);

                Client = new Client(
                    scheme,
                    host,
                    port,
                    serverKey,
                    UnityWebRequestAdapter.Instance,
                    autoRefreshSession: true
                );

                // Optional: have Nakama log HTTP errors internally as well.
                Client.Logger = new UnityLogger();

                if (settings.TimeoutSeconds > 0)
                {
                    Client.Timeout = settings.TimeoutSeconds;
                }

                Debug.Log($"Nakama: Client created. {scheme}://{host}:{port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Nakama: failed to create Client: {ex}");
                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { "nakama_client_create_failed" };
                return;
            }

            // --- Authenticate device -----------------------------------------
            try
            {
                var deviceId = PlayerPrefs.GetString(
                    "nakama.deviceId",
                    SystemInfo.deviceUniqueIdentifier
                );

                if (
                    deviceId == SystemInfo.unsupportedIdentifier
                    || string.IsNullOrWhiteSpace(deviceId)
                )
                {
                    deviceId = Guid.NewGuid().ToString();
                }

                PlayerPrefs.SetString("nakama.deviceId", deviceId);
                PlayerPrefs.Save();

                Debug.Log(
                    $"Nakama: AuthenticateDeviceAsync to {Client.Scheme}://{Client.Host}:{Client.Port} with deviceId='{deviceId}'"
                );
                Session = await Client.AuthenticateDeviceAsync(deviceId);
                Debug.Log("Nakama: AuthenticateDeviceAsync succeeded.");
            }
            catch (ApiResponseException apiEx)
            {
                Debug.LogError(
                    "Nakama: API error during AuthenticateDeviceAsync\n"
                        + $"  StatusCode: {apiEx.StatusCode}\n"
                        + $"  GrpcStatusCode: {apiEx.GrpcStatusCode}\n"
                        + $"  Message: {apiEx.Message}"
                );

                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { $"auth_api_{apiEx.StatusCode}" };
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Nakama: unexpected error during AuthenticateDeviceAsync: {ex}");
                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { "auth_exception" };
                return;
            }

            // --- Healthcheck RPC ---------------------------------------------
            var healthy = await RunHealthcheckAsync();
            if (!healthy)
            {
                Status = NetworkConnectionStatus.Disconnected;
                errorCodes = new[] { "healthcheck_failed" };
                Debug.LogError("Nakama: healthcheck RPC failed.");
                return;
            }

            // --- Open socket --------------------------------------------------
            try
            {
                bool useMainThread = settings.UseMainThreadSocket;
                Socket = Client.NewSocket(useMainThread: useMainThread);
                // wire socket lifecycle
                socketConnectedHandler = () =>
                {
                    Debug.Log("Nakama: socket connected event");
                    Status = NetworkConnectionStatus.Connected;
                    StopReconnectLoop();
                    StartHeartbeat();
                };
                socketClosedHandler = reason =>
                {
                    Debug.Log($"Nakama: socket closed event ({reason})");
                    Status = NetworkConnectionStatus.Disconnected;
                    StopHeartbeat();
                    TriggerReconnect("socket closed");
                };
                socketErrorHandler = ex =>
                {
                    Debug.LogError($"Nakama: socket error event {ex}");
                    Status = NetworkConnectionStatus.Error;
                    StopHeartbeat();
                    TriggerReconnect("socket error");
                };

                Socket.Connected += socketConnectedHandler;
                Socket.Closed += socketClosedHandler;
                Socket.ReceivedError += socketErrorHandler;

                bool appearOnline = true;
                int connectTimeout =
                    settings.SocketConnectTimeoutSeconds > 0
                        ? settings.SocketConnectTimeoutSeconds
                        : 30;

                await Socket.ConnectAsync(Session, appearOnline, connectTimeout);

                Status = NetworkConnectionStatus.Connected;
                errorCodes = null;
                StartHeartbeat();
                StopReconnectLoop();

                Debug.Log("Nakama: Connected successfully (auth + healthcheck + socket OK).");
            }
            catch (ApiResponseException apiEx)
            {
                Debug.LogError(
                    $"Nakama: API error while connecting socket: {apiEx.StatusCode} {apiEx.Message}"
                );
                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { $"socket_api_{apiEx.StatusCode}" };
                StopHeartbeat();
                TriggerReconnect("socket api error");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Nakama: unexpected error while connecting socket: {ex}");
                Status = NetworkConnectionStatus.Error;
                errorCodes = new[] { "socket_exception" };
                StopHeartbeat();
                TriggerReconnect("socket exception");
            }
        }

        // ---------------------------------------------------------------------
        // Healthcheck RPC: expects { "status": "ok" }
        // ---------------------------------------------------------------------
        private async Task<bool> RunHealthcheckAsync()
        {
            if (Client == null || Session == null)
            {
                Debug.LogError(
                    "Nakama: RunHealthcheckAsync called before client/session are ready."
                );
                return false;
            }

            try
            {
                var rpc = await Client.RpcAsync(Session, HealthcheckRpcId);

                if (string.IsNullOrEmpty(rpc.Payload))
                {
                    Debug.LogWarning("Nakama: healthcheck RPC returned empty payload.");
                    return false;
                }

                var json = JObject.Parse(rpc.Payload);
                var statusToken = json["status"];
                if (statusToken != null)
                {
                    var status = statusToken.ToString();
                    bool ok = string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase);

                    Debug.Log($"Nakama: healthcheck status = {status}");
                    return ok;
                }

                Debug.LogWarning($"Nakama: healthcheck payload missing 'status': {rpc.Payload}");
                return false;
            }
            catch (ApiResponseException apiEx)
            {
                Debug.LogError(
                    $"Nakama: healthcheck RPC API error: {apiEx.StatusCode} {apiEx.Message}"
                );
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Nakama: healthcheck RPC exception: {ex}");
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // Debug: ping endpoint to verify we're hitting the right host/port
        // ---------------------------------------------------------------------
        private async Task DebugPingEndpointAsync(string scheme, string host, int port)
        {
            var url = $"{scheme}://{host}:{port}/";
            using (var req = UnityWebRequest.Get(url))
            {
                Debug.Log($"Nakama: endpoint ping -> {url}");
                await req.SendWebRequest();
                Debug.Log(
                    $"Nakama: endpoint ping result {url}\n"
                        + $"  responseCode: {req.responseCode}\n"
                        + $"  result: {req.result}\n"
                        + $"  text: {req.downloadHandler.text}"
                );
            }
        }

        private void StartHeartbeat()
        {
            StopHeartbeat();
            heartbeatCts = new CancellationTokenSource();
            var token = heartbeatCts.Token;
            _ = HeartbeatLoopAsync(token);
        }

        private void StopHeartbeat()
        {
            if (heartbeatCts == null)
            {
                return;
            }

            heartbeatCts.Cancel();
            heartbeatCts.Dispose();
            heartbeatCts = null;
        }

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (Client == null || Session == null)
                {
                    Status = NetworkConnectionStatus.Disconnected;
                    continue;
                }

                bool healthy = await RunHealthcheckAsync();
                Status = healthy
                    ? NetworkConnectionStatus.Connected
                    : NetworkConnectionStatus.Error;
                if (!healthy)
                {
                    StopHeartbeat();
                    TriggerReconnect("heartbeat failed");
                }
            }
        }

        private void TriggerReconnect(string reason)
        {
            if (reconnectCts != null || Status == NetworkConnectionStatus.Connecting)
            {
                return;
            }

            Debug.Log($"Nakama: starting reconnect loop ({reason})");
            reconnectCts = new CancellationTokenSource();
            var token = reconnectCts.Token;
            _ = ReconnectLoopAsync(token);
        }

        private async Task ReconnectLoopAsync(CancellationToken token)
        {
            for (int i = 0; i < reconnectDelaysMs.Length && !token.IsCancellationRequested; i++)
            {
                try
                {
                    await Task.Delay(reconnectDelaysMs[i], token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (Status == NetworkConnectionStatus.Connected)
                {
                    StopReconnectLoop();
                    return;
                }

                try
                {
                    await CloseConnectionAsync();
                    await TryToConnectAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Nakama: reconnect attempt failed: {ex}");
                }

                if (Status == NetworkConnectionStatus.Connected)
                {
                    StopReconnectLoop();
                    return;
                }
            }
        }

        private void StopReconnectLoop()
        {
            if (reconnectCts == null)
            {
                return;
            }

            reconnectCts.Cancel();
            reconnectCts.Dispose();
            reconnectCts = null;
        }
    }
}
