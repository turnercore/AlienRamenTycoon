using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

namespace Server
{
    public class NakamaNetworkService : INetworkService, IDisposable, IRpsMatchService
    {
        private const long OpPick = 1;
        private const long OpRoundResult = 2;
        private const long OpMatchReady = 3;

        public INetworkConnection Connection => _connection;
        public NetworkConnectionStatus Status => _connection.Status;

        private readonly NakamaConnection _connection;
        private Action<NetworkConnectionStatus> onNetworkStatusChanged;
        private ISocket socket;
        private bool handlersAttached;
        private bool matchmakingInProgress;
        private string matchId;
        private Action<IMatchmakerMatched> matchmakerMatchedHandler;
        private Action<IMatchState> matchStateHandler;

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

        public event Action<RpsMatchReadyPayload> MatchReady;
        public event Action<RpsRoundResultPayload> RoundResolved;

        public string MatchId => matchId;
        public string LocalUserId => _connection?.Session?.UserId;

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
            DetachRealtimeHandlers();
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

        public async Task MatchmakeAsync()
        {
            if (!EnsureConnected())
            {
                throw new InvalidOperationException("Network not connected.");
            }

            EnsureSocket();
            if (socket == null)
            {
                throw new InvalidOperationException("Socket not available.");
            }

            if (matchmakingInProgress || !string.IsNullOrEmpty(matchId))
            {
                return;
            }

            matchmakingInProgress = true;
            await socket.AddMatchmakerAsync("*", 2, 2);
        }

        public async Task SendPickAsync(string action)
        {
            if (!EnsureConnected())
            {
                throw new InvalidOperationException("Network not connected.");
            }

            if (string.IsNullOrEmpty(matchId))
            {
                throw new InvalidOperationException("Match not ready.");
            }

            EnsureSocket();
            if (socket == null)
            {
                throw new InvalidOperationException("Socket not available.");
            }

            var payload = new RpsActionRequest { action = action };
            string json = JsonConvert.SerializeObject(payload);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await socket.SendMatchStateAsync(matchId, OpPick, data);
        }

        private void EnsureSocket()
        {
            if (socket != null)
            {
                return;
            }

            if (_connection == null || _connection.Socket == null)
            {
                return;
            }

            socket = _connection.Socket;
            AttachRealtimeHandlers();
        }

        private void AttachRealtimeHandlers()
        {
            if (socket == null || handlersAttached)
            {
                return;
            }

            matchmakerMatchedHandler = OnMatchmakerMatched;
            matchStateHandler = OnMatchStateReceived;
            socket.ReceivedMatchmakerMatched += matchmakerMatchedHandler;
            socket.ReceivedMatchState += matchStateHandler;
            handlersAttached = true;
        }

        private void DetachRealtimeHandlers()
        {
            if (socket == null || !handlersAttached)
            {
                return;
            }

            if (matchmakerMatchedHandler != null)
            {
                socket.ReceivedMatchmakerMatched -= matchmakerMatchedHandler;
            }

            if (matchStateHandler != null)
            {
                socket.ReceivedMatchState -= matchStateHandler;
            }

            handlersAttached = false;
            matchmakerMatchedHandler = null;
            matchStateHandler = null;
        }

        private async void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            if (socket == null || matched == null)
            {
                matchmakingInProgress = false;
                return;
            }

            try
            {
                var joined = await socket.JoinMatchAsync(matched.MatchId);
                matchId = joined.Id;
                matchmakingInProgress = false;

                string localUserId = _connection?.Session?.UserId;
                string opponentUserId = null;
                string opponentUsername = null;

                foreach (var presence in matched.Users)
                {
                    if (presence.Presence.UserId == localUserId)
                    {
                        continue;
                    }

                    opponentUserId = presence.Presence.UserId;
                    opponentUsername = presence.Presence.Username;
                    break;
                }

                var payload = new RpsMatchReadyPayload
                {
                    matchId = matchId,
                    localUserId = localUserId,
                    opponentUserId = opponentUserId,
                    opponentUsername = opponentUsername,
                    round = 1,
                    scores = new Dictionary<string, int>(),
                };

                if (!string.IsNullOrEmpty(localUserId))
                {
                    payload.scores[localUserId] = 0;
                }

                if (!string.IsNullOrEmpty(opponentUserId))
                {
                    payload.scores[opponentUserId] = 0;
                }

                MatchReady?.Invoke(payload);
            }
            catch (Exception ex)
            {
                Debug.LogError($"NakamaNetworkService: join match failed: {ex}");
                matchmakingInProgress = false;
            }
        }

        private void OnMatchStateReceived(IMatchState state)
        {
            if (state == null)
            {
                return;
            }

            try
            {
                string json = Encoding.UTF8.GetString(state.State);

                if (state.OpCode == OpRoundResult)
                {
                    var payload = JsonConvert.DeserializeObject<RpsMatchResult>(json);
                    if (payload == null)
                    {
                        return;
                    }

                    var result = new RpsRoundResultPayload
                    {
                        round = payload.round,
                        picks = payload.picks,
                        winnerUserId = payload.winnerUserId,
                        scores = payload.scores,
                        nextRound = payload.nextRound,
                    };
                    RoundResolved?.Invoke(result);
                }
                else if (state.OpCode == OpMatchReady)
                {
                    var payload = JsonConvert.DeserializeObject<RpsMatchReadyServerPayload>(json);
                    if (payload == null || payload.players == null)
                    {
                        return;
                    }

                    string localUserId = _connection?.Session?.UserId;
                    string opponentUserId = null;
                    string opponentUsername = null;

                    foreach (var entry in payload.players)
                    {
                        if (entry.Key != localUserId)
                        {
                            opponentUserId = entry.Key;
                            opponentUsername = entry.Value?.username;
                            break;
                        }
                    }

                    var ready = new RpsMatchReadyPayload
                    {
                        matchId = payload.matchId,
                        localUserId = localUserId,
                        opponentUserId = opponentUserId,
                        opponentUsername = opponentUsername,
                        round = payload.round,
                        scores = payload.scores,
                    };
                    MatchReady?.Invoke(ready);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"NakamaNetworkService: match state parse error: {ex}");
            }
        }

        [Serializable]
        private class RpsActionRequest
        {
            public string action;
        }

        [Serializable]
        private class RpsMatchResult
        {
            public int round;
            public Dictionary<string, int> scores;
            public int nextRound;
            public Dictionary<string, string> picks;
            public string winnerUserId;
        }
    }
}
