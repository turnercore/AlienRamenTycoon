using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core;
using Nakama;
using Newtonsoft.Json;
using Server;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Project
{
    public class RockPaperScissorsGameMode : IGameMode
    {
        private const long OpPick = 1;
        private const long OpRoundResult = 2;

        private readonly ApplicationData applicationData;
        private readonly MenuPrefabsContainer menuPrefabsContainer;
        private readonly AddressablesHandleHelper addressableHandles = new();

        private RockPaperScissorsView view;
        private INetworkService network => applicationData.Network;
        private bool initialized;
        private bool awaitingServerResult;
        private bool matchmakingInProgress;
        private bool isMatched;
        private ISocket socket;
        private Action<IMatchmakerMatched> matchmakerMatchedHandler;
        private Action<IMatchState> matchStateHandler;
        private string matchId;

        public bool IsGameModeInitialized => initialized;

        public RockPaperScissorsGameMode(
            ApplicationData applicationData,
            MenuPrefabsContainer menuPrefabsContainer
        )
        {
            this.applicationData = applicationData;
            this.menuPrefabsContainer = menuPrefabsContainer;
        }

        public void EnterGameMode()
        {
            initialized = false;

            if (
                menuPrefabsContainer == null
                || menuPrefabsContainer.rockPaperScissorsMenuReference == null
            )
            {
                Debug.LogError(
                    "RockPaperScissorsGameMode: Missing prefab reference in MenuPrefabsContainer."
                );
                return;
            }

            addressableHandles.LoadAssetAsync<GameObject>(
                menuPrefabsContainer.rockPaperScissorsMenuReference,
                OnPrefabLoaded
            );

            Debug.Log("RockPaperScissorsGameMode: Entered Game Mode.");
        }

        private void OnPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError("RockPaperScissorsGameMode: Failed to load menu prefab.");
                return;
            }

            view = new RockPaperScissorsView(handle.Result);
            view.OnQuitRequested += HandleQuitRequested;
            view.OnActionSubmitted += HandleActionSubmitted;
            view.Initialize();
            view.SetActionButtonsVisible(false);
            view.DisplayResult("Looking for opponent...");

            // Networking setup
            if (network == null)
            {
                Debug.LogError("RockPaperScissorsGameMode: Network service is not available.");
                return;
            }
            else
            {
                network.OnNetworkStatusChanged += HandleNetworkStatusChanged;
                // Update for current
                HandleNetworkStatusChanged(network.Connection.Status);
            }
            initialized = true;
        }

        private void HandleNetworkStatusChanged(NetworkConnectionStatus status)
        {
            view.UpdateConnectionStatus(status.ToString());
            if (status == NetworkConnectionStatus.Connected)
            {
                AttachSocketHandlers();
                StartMatchmakingIfNeeded();
            }
            else
            {
                DetachSocketHandlers();
            }
        }

        private void HandleQuitRequested()
        {
            applicationData.ChangeApplicationState(ApplicationState.Exit);
        }

        private void HandleActionSubmitted(RockPaperScissorsView.RockPaperScissorsAction action)
        {
            if (awaitingServerResult)
            {
                return;
            }

            _ = SubmitActionAsync(action);
        }

        private async Task SubmitActionAsync(RockPaperScissorsView.RockPaperScissorsAction action)
        {
            if (network == null)
            {
                Debug.LogError("RockPaperScissorsGameMode: Network service is not available.");
                return;
            }

            if (string.IsNullOrEmpty(matchId))
            {
                view.DisplayResult("Not matched yet.");
                return;
            }

            awaitingServerResult = true;
            view.SetActionButtonsVisible(false);
            view.ShowWaitingForOpponent();

            try
            {
                ISocket activeSocket = GetSocket();
                if (activeSocket == null)
                {
                    view.DisplayResult("Not connected.");
                    view.SetActionButtonsVisible(true);
                    awaitingServerResult = false;
                    return;
                }

                var payload = new RpsActionRequest
                {
                    action = action.ToString().ToLowerInvariant(),
                };
                string json = JsonConvert.SerializeObject(payload);
                byte[] data = Encoding.UTF8.GetBytes(json);
                await activeSocket.SendMatchStateAsync(matchId, OpPick, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"RPS submit exception: {ex}");
                view.DisplayResult("Error: request_failed");
                view.SetActionButtonsVisible(true);
                awaitingServerResult = false;
            }
        }

        private void StartMatchmakingIfNeeded()
        {
            if (isMatched || matchmakingInProgress || string.IsNullOrEmpty(GetLocalUserId()))
            {
                return;
            }

            _ = StartMatchmakingAsync();
        }

        private async Task StartMatchmakingAsync()
        {
            ISocket activeSocket = GetSocket();
            if (activeSocket == null)
            {
                return;
            }

            matchmakingInProgress = true;
            view.DisplayResult("Looking for opponent...");

            try
            {
                await activeSocket.AddMatchmakerAsync("*", 2, 2);
            }
            catch (Exception ex)
            {
                Debug.LogError($"RPS matchmake exception: {ex}");
                view.DisplayResult("Matchmaking error.");
                matchmakingInProgress = false;
            }
        }

        public void Tick()
        {
            if (initialized)
            {
                view?.Tick();
            }
        }

        public void LateTick() { }

        public void ExitGameMode()
        {
            if (view != null)
            {
                view.OnQuitRequested -= HandleQuitRequested;
                view.OnActionSubmitted -= HandleActionSubmitted;
                view.Dispose();
                view = null;
            }

            if (network != null)
            {
                network.OnNetworkStatusChanged -= HandleNetworkStatusChanged;
            }
            DetachSocketHandlers();

            initialized = false;
            awaitingServerResult = false;
            matchmakingInProgress = false;
            isMatched = false;
            matchId = null;
            addressableHandles.ReleaseAll();
        }

        public void Dispose()
        {
            ExitGameMode();
            addressableHandles.Dispose();
        }

        private void AttachSocketHandlers()
        {
            ISocket socket = GetSocket();
            if (socket == null || socket == this.socket)
            {
                return;
            }

            DetachSocketHandlers();
            this.socket = socket;
            matchmakerMatchedHandler = OnMatchmakerMatched;
            matchStateHandler = OnMatchStateReceived;
            this.socket.ReceivedMatchmakerMatched += matchmakerMatchedHandler;
            this.socket.ReceivedMatchState += matchStateHandler;
        }

        private void DetachSocketHandlers()
        {
            if (socket == null)
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

            socket = null;
            matchmakerMatchedHandler = null;
            matchStateHandler = null;
        }

        private ISocket GetSocket()
        {
            if (
                network is NakamaNetworkService nakamaService
                && nakamaService.Connection is NakamaConnection nakamaConnection
            )
            {
                return nakamaConnection.Socket;
            }

            return null;
        }

        private async void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            if (matched == null || socket == null)
            {
                return;
            }

            try
            {
                var joined = await socket.JoinMatchAsync(matched.MatchId);
                matchId = joined.Id;
                isMatched = true;
                matchmakingInProgress = false;
                view.DisplayResult("Match found!");
                view.SetActionButtonsVisible(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"RPS join match exception: {ex}");
                view.DisplayResult("Match join failed.");
                matchmakingInProgress = false;
            }
        }

        private void OnMatchStateReceived(IMatchState state)
        {
            if (state == null || state.OpCode != OpRoundResult)
            {
                return;
            }

            try
            {
                string json = Encoding.UTF8.GetString(state.State);
                var payload = JsonConvert.DeserializeObject<RpsMatchResult>(json);
                if (payload == null)
                {
                    return;
                }

                var lastRound = new RpsRoundResult
                {
                    round = payload.round,
                    picks = payload.picks,
                    winnerUserId = payload.winnerUserId,
                };
                ApplyResolvedRound(lastRound, payload.scores);
                view.SetActionButtonsVisible(true);
                awaitingServerResult = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RPS match state parse error: {ex}");
            }
        }

        private void ApplyResolvedRound(RpsRoundResult lastRound, Dictionary<string, int> scores)
        {
            if (lastRound == null || lastRound.picks == null)
            {
                view.DisplayResult("Result pending");
                return;
            }

            string localUserId = GetLocalUserId();
            string opponentUserId = null;

            if (!string.IsNullOrEmpty(localUserId) && lastRound.picks.ContainsKey(localUserId))
            {
                foreach (var entry in lastRound.picks)
                {
                    if (entry.Key != localUserId)
                    {
                        opponentUserId = entry.Key;
                        break;
                    }
                }
            }
            else
            {
                foreach (var entry in lastRound.picks)
                {
                    localUserId = entry.Key;
                    break;
                }

                foreach (var entry in lastRound.picks)
                {
                    if (entry.Key != localUserId)
                    {
                        opponentUserId = entry.Key;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(localUserId) || string.IsNullOrEmpty(opponentUserId))
            {
                view.DisplayResult("Result pending");
                return;
            }

            string playerAction = lastRound.picks[localUserId];
            string opponentAction = lastRound.picks[opponentUserId];
            string outcomeText = BuildOutcomeText(
                playerAction,
                opponentAction,
                lastRound.winnerUserId,
                localUserId
            );
            view.DisplayResult(outcomeText);

            if (
                scores != null
                && scores.ContainsKey(localUserId)
                && scores.ContainsKey(opponentUserId)
            )
            {
                view.UpdateScores(scores[localUserId], scores[opponentUserId]);
            }
        }

        private string GetLocalUserId()
        {
            if (
                network is NakamaNetworkService nakamaService
                && nakamaService.Connection is NakamaConnection nakamaConnection
                && nakamaConnection.Session != null
            )
            {
                return nakamaConnection.Session.UserId;
            }

            return null;
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

        [Serializable]
        private class RpsRoundResult
        {
            public int round;
            public Dictionary<string, string> picks;
            public string winnerUserId;
        }

        private static string BuildOutcomeText(
            string playerAction,
            string opponentAction,
            string winnerUserId,
            string localUserId
        )
        {
            if (string.IsNullOrEmpty(playerAction) || string.IsNullOrEmpty(opponentAction))
            {
                return "Result pending";
            }

            if (string.IsNullOrEmpty(winnerUserId))
            {
                return $"{playerAction} ties {opponentAction}!";
            }

            if (string.Equals(winnerUserId, localUserId, StringComparison.Ordinal))
            {
                return $"{playerAction} beats {opponentAction}!";
            }

            return $"{playerAction} loses to {opponentAction}!";
        }
    }
}
