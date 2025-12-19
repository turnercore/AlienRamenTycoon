using System;
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
        private bool initialized;

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
        }

        private void HandleQuitRequested()
        {
            applicationData.ChangeApplicationState(ApplicationState.Exit);
        }

        private void HandleActionSubmitted(RockPaperScissorsView.RockPaperScissorsAction action)
        {
            Debug.Log($"RockPaperScissors action submitted: {action}");
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

            initialized = false;
            addressableHandles.ReleaseAll();
        }

        public void Dispose()
        {
            ExitGameMode();
            addressableHandles.Dispose();
        }
    }
}
