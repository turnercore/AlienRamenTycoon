using System;
using Core;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

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
        private readonly BootstrapSettings bootstrapSettings;
        private MainMenuView mainMenuView;
        private AsyncOperationHandle<SceneInstance> loadSceneAsync;
        public bool IsApplicationStateInitialized { get; set; } = true;

        public MainMenuApplicationState(
            ApplicationData applicationData,
            MenuApplicationStateData menuApplicationStateData,
            BootstrapSettings bootstrapSettings
        )
        {
            this.applicationData = applicationData;
            this.menuApplicationStateData = menuApplicationStateData;
            this.bootstrapSettings = bootstrapSettings;
        }

        public void EnterApplicationState()
        {
            Addressables
                .LoadSceneAsync(bootstrapSettings.menuScene, LoadSceneMode.Single)
                .Completed += handle =>
            {
                OnSceneLoaded();
            };
            menuApplicationStateData.startGameRequests += mode =>
            {
                applicationData.ChangeApplicationState(ApplicationState.GameMode);
                applicationData.ChangeGameModeState(mode);
            };
        }

        private void OnSceneLoaded()
        {
            Addressables
                .LoadAssetAsync<MenuPrefabsContainer>(bootstrapSettings.menuPrefabsContainer)
                .Completed += OnContainerLoaded;
        }

        private void OnContainerLoaded(AsyncOperationHandle<MenuPrefabsContainer> handle)
        {
            mainMenuView = new MainMenuView(handle.Result, menuApplicationStateData);
            mainMenuView.Initialize();
        }

        public ApplicationState Tick()
        {
            mainMenuView?.Tick();
            return applicationData.ActiveApplicationState;
        }

        public void LateTick() { }

        public void Dispose() { }

        public void ExitApplicationState() { }
    }
}
