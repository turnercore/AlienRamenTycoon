using System;
using Core;
using UnityEngine;
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
        private readonly MainMenuBootSettings mainMenuBootSettings;
        private MainMenuView mainMenuView;
        private MainMenuReference mainMenuReference;
        private AsyncOperationHandle<SceneInstance> loadSceneAsync;
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
            Addressables
                .LoadAssetAsync<MainMenuReference>(
                    mainMenuBootSettings.menuPrefabsContainer.mainMenuReference
                )
                .Completed += handle =>
            {
                OnMenuLoaded(handle);
            };

            menuApplicationStateData.startGameRequests += mode =>
            {
                applicationData.ChangeApplicationState(ApplicationState.GameMode);
                applicationData.ChangeGameModeState(mode);
            };
        }

        private void OnMenuLoaded(AsyncOperationHandle<MainMenuReference> handle)
        {
            mainMenuReference = GameObject
                .Instantiate<MainMenuReference>(handle.Result)
                .GetComponent<MainMenuReference>();
        }

        public ApplicationState Tick()
        {
            mainMenuView?.Tick();
            return applicationData.ActiveApplicationState;
        }

        public void LateTick() { }

        public void Dispose()
        {
            mainMenuView?.Dispose();
            if (mainMenuReference != null)
            {
                GameObject.Destroy(mainMenuReference.gameObject);
            }

            Addressables.Release(mainMenuReference);
            //Addressables.Release(mainMenuView);
        }

        public void ExitApplicationState() { }
    }
}
