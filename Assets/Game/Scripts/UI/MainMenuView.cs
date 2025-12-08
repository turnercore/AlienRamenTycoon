using System;
using System.Threading.Tasks;
using Core;
using UnityEngine;

namespace Project
{
    /// <summary>
    /// Main menu interface class which is responsible for creating the main menu view and handling the main menu events
    /// </summary>
    public class MainMenuView : IApplicationLifecycle
    {
        private readonly MenuApplicationStateData menuApplicationStateData;
        private MainMenuReference mainMenuReference;
        private readonly ApplicationData applicationData;
        private OptionsMenuView optionsMenuView;
        private readonly UnityEngine.AddressableAssets.AssetReferenceT<GameObject> optionsMenuPrefabReference;
        private GameObject optionsMenuPrefab;
        private readonly AddressablesHandleHelper handles = new();
        private bool isOptionsMenuOpen = false;
        private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> optionsHandle;

        public MainMenuView(
            MainMenuReference mainMenuReference, // Use the specific type
            MenuApplicationStateData menuApplicationStateData,
            UnityEngine.AddressableAssets.AssetReferenceT<GameObject> optionsMenuPrefabReference,
            ApplicationData applicationData
        )
        {
            this.mainMenuReference = mainMenuReference;
            this.menuApplicationStateData = menuApplicationStateData;
            this.optionsMenuPrefabReference = optionsMenuPrefabReference;
            this.applicationData = applicationData;

            // go ahead and async load the options menu prefab from addressables
            optionsHandle = handles.LoadAssetAsync<GameObject>(
                optionsMenuPrefabReference,
                handle =>
                {
                    if (
                        handle.Status
                        == UnityEngine
                            .ResourceManagement
                            .AsyncOperations
                            .AsyncOperationStatus
                            .Succeeded
                    )
                    {
                        optionsMenuPrefab = handle.Result;
                    }
                    else
                    {
                        Debug.LogError("Failed to load OptionsMenu prefab.");
                    }
                }
            );
        }

        public void Initialize()
        {
            if (mainMenuReference == null)
            {
                mainMenuReference = GameObject.Instantiate(mainMenuReference);
            }

            mainMenuReference.exitButton.onClick.AddListener(OnQuitClicked);
            mainMenuReference.optionsButton.onClick.AddListener(OnOptionsClicked);
            mainMenuReference.playButton.onClick.AddListener(OnStartClicked);
        }

        private void OnQuitClicked()
        {
            // Change to a Quit Application State
            applicationData.ChangeApplicationState(ApplicationState.Exit);
        }

        private void OnStartClicked()
        {
            Debug.Log("Start button clicked - starting game...");
        }

        private void OnOptionsClicked()
        {
            //See if options menu is already open
            if (isOptionsMenuOpen)
            {
                return;
            }
            // See if option menu has loaded from addresables
            if (optionsMenuPrefab == null)
            {
                Debug.Log("Options menu has not finished loading yet.");
                return;
            }

            isOptionsMenuOpen = true;

            // Create the optinons menu view
            optionsMenuView = new OptionsMenuView(
                optionsMenuPrefab,
                menuApplicationStateData,
                applicationData
            );
            optionsMenuView.Initialize();
        }

        public void Tick() { }

        public void Dispose()
        {
            // Clean up main menu listeners
            mainMenuReference.exitButton.onClick.RemoveListener(OnQuitClicked);
            mainMenuReference.optionsButton.onClick.RemoveListener(OnOptionsClicked);
            mainMenuReference.playButton.onClick.RemoveListener(OnStartClicked);

            GameObject.Destroy(mainMenuReference.gameObject);
            mainMenuReference = null;

            optionsMenuView?.Dispose();
            optionsMenuView = null;

            optionsHandle.Release();
            handles.Dispose();
        }
    }
}
