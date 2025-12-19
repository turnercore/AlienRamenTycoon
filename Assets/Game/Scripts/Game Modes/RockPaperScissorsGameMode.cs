using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Server;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Project
{
    public class RockPaperScissorsGameMode : IGameMode
    {
        private readonly ApplicationData applicationData;
        private readonly MenuPrefabsContainer menuPrefabsContainer;
        private readonly AddressablesHandleHelper addressableHandles = new();

        private RockPaperScissorsView view;
        private INetworkService network => applicationData.Network;
        private IRpsMatchService rpsMatchService;
        private bool initialized;
        private bool awaitingServerResult;
        private bool matchmakingInProgress;
        private bool isMatched;
        private string matchId;
        private string localUserId;
        private string opponentUserId;

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
                rpsMatchService = network as IRpsMatchService;
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
                AttachMatchHandlers();
                StartMatchmakingIfNeeded();
            }
            else
            {
                DetachMatchHandlers();
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
                if (rpsMatchService == null)
                {
                    view.DisplayResult("Not connected.");
                    view.SetActionButtonsVisible(true);
                    awaitingServerResult = false;
                    return;
                }

                await rpsMatchService.SendPickAsync(action.ToString().ToLowerInvariant());
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
            if (rpsMatchService == null)
            {
                view.DisplayResult("Matchmaking unavailable.");
                return;
            }

            matchmakingInProgress = true;
            view.DisplayResult("Looking for opponent...");

            try
            {
                await rpsMatchService.MatchmakeAsync();
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
            DetachMatchHandlers();

            initialized = false;
            awaitingServerResult = false;
            matchmakingInProgress = false;
            isMatched = false;
            matchId = null;
            localUserId = null;
            opponentUserId = null;
            addressableHandles.ReleaseAll();
        }

        public void Dispose()
        {
            ExitGameMode();
            addressableHandles.Dispose();
        }

        private void AttachMatchHandlers()
        {
            if (rpsMatchService == null)
            {
                return;
            }

            DetachMatchHandlers();
            rpsMatchService.MatchReady += OnMatchReady;
            rpsMatchService.RoundResolved += OnRoundResolved;
        }

        private void DetachMatchHandlers()
        {
            if (rpsMatchService == null)
            {
                return;
            }

            rpsMatchService.MatchReady -= OnMatchReady;
            rpsMatchService.RoundResolved -= OnRoundResolved;
        }

        private void OnMatchReady(RpsMatchReadyPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            matchId = payload.matchId;
            localUserId = !string.IsNullOrEmpty(payload.localUserId)
                ? payload.localUserId
                : GetLocalUserId();
            opponentUserId = payload.opponentUserId;
            isMatched = !string.IsNullOrEmpty(matchId);
            matchmakingInProgress = false;
            view.DisplayResult("Match found!");
            view.SetActionButtonsVisible(true);
            view.UpdateScores(GetScore(payload.scores, localUserId), GetScore(payload.scores, opponentUserId));
            view.UpdateRound(payload.round);

            if (!string.IsNullOrEmpty(payload.opponentUsername))
            {
                view.UpdateOpponentName(payload.opponentUsername);
            }
        }

        private void OnRoundResolved(RpsRoundResultPayload payload)
        {
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
            view.UpdateRound(payload.round);
            ApplyResolvedRound(lastRound, payload.scores);
            view.SetActionButtonsVisible(true);
            awaitingServerResult = false;
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
            if (!string.IsNullOrEmpty(localUserId))
            {
                return localUserId;
            }

            return rpsMatchService?.LocalUserId;
        }

        private static int GetScore(Dictionary<string, int> scores, string userId)
        {
            if (scores == null || string.IsNullOrEmpty(userId))
            {
                return 0;
            }

            return scores.TryGetValue(userId, out int value) ? value : 0;
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
