using System;
using Core;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Project
{
    public class MenuApplicationStateData
    {
        public Action<GameMode> startGameRequests;
    }

    /// <summary>
    /// If the game has a Main Menu, this would be the state where it runs.
    /// This game state is also exemplifying how the UI framework should be initialized
    /// </summary>
    public class MainMenuApplicationState : IApplicationState
    {
        private readonly ApplicationData applicationData;
        private readonly MenuApplicationStateData menuApplicationStateData;
        private readonly MainMenuBootSettings mainMenuBootSettings;
        private readonly AddressablesHandleHelper addressableHandles = new();
        private Action<GameMode> startGameHandler;
        private MainMenuView mainMenuView;
        private MainMenuReference mainMenuReference;
        public bool IsApplicationStateInitialized { get; set; } = true;

        public MainMenuApplicationState(
            ApplicationData applicationData,
            MenuApplicationStateData menuApplicationStateData,
            MainMenuBootSettings mainMenuBootSettings
        )
        {
            this.applicationData = applicationData;
            this.menuApplicationStateData = menuApplicationStateData;
            this.mainMenuBootSettings = mainMenuBootSettings;
        }

        public void EnterApplicationState()
        {
            addressableHandles.LoadAssetAsync<GameObject>(
                mainMenuBootSettings.menuPrefabsContainer.mainMenuReference,
                handle => OnMenuLoaded(handle)
            );

            startGameHandler = mode =>
            {
                applicationData.ChangeApplicationState(ApplicationState.GameMode);
                applicationData.ChangeGameModeState(mode);
            };

            menuApplicationStateData.startGameRequests += startGameHandler;
        }

        private void OnMenuLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError("Failed to load MainMenuReference.");
                return;
            }

            // Instantiate the main menu prefab
            GameObject mainMenuInstance = GameObject.Instantiate(handle.Result);

            mainMenuReference = mainMenuInstance.GetComponent<MainMenuReference>();

            if (mainMenuReference == null)
            {
                Debug.LogError("MainMenuReference prefab missing component");
                return;
            }

            mainMenuView = new MainMenuView(
                mainMenuReference,
                menuApplicationStateData,
                optionsMenuPrefabReference: mainMenuBootSettings
                    .menuPrefabsContainer
                    .optionsMenuReference,
                applicationData
            );
            mainMenuView.Initialize();
        }

        public ApplicationState Tick()
        {
            mainMenuView?.Tick();
            return applicationData.ActiveApplicationState;
        }

        public void LateTick() { }

        public void Dispose()
        {
            if (startGameHandler != null)
            {
                menuApplicationStateData.startGameRequests -= startGameHandler;
                startGameHandler = null;
            }

            mainMenuView?.Dispose();
            if (mainMenuReference != null)
            {
                GameObject.Destroy(mainMenuReference.gameObject);
                mainMenuReference = null;
            }

            addressableHandles.Dispose();
        }

        public void ExitApplicationState() { }
    }
}
