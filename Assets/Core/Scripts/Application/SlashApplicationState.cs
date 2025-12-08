using System.Runtime.InteropServices;
using Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project
{
    /// <summary>
    /// This is an example for the first game state which in our case would usually contain a gdpr
    /// This is an example to show how it could be initialized and also how it could boot into the next game state
    /// Ideally a SceneHandler/Manager of sorts should handle loading and unloading scenes.
    /// </summary>
    public class SplashApplicationState : IApplicationState
    {
        private readonly SplashBootSettings bootInitializer;
        private readonly ApplicationData applicationData;
        public bool IsApplicationStateInitialized { get; set; } = true;
        private GameObject gdprReferencePrefab;
        private GdprUIReference gdprMenuReference;
        private readonly AddressablesHandleHelper handles = new();

        public SplashApplicationState(
            SplashBootSettings bootInitializer,
            ApplicationData applicationData
        )
        {
            this.bootInitializer = bootInitializer;
            this.applicationData = applicationData;
        }

        public void EnterApplicationState()
        {
            // Guard statements
            if (!EnterApplicationStateGuard())
                return;

            LoadMenuFromAddressables();
        }

        private bool EnterApplicationStateGuard()
        {
            if (bootInitializer == null)
            {
                Debug.LogError("bootInitializer is null!");
                FailOutOfApplicationState();
                return false;
            }

            if (bootInitializer.menuPrefabsContainer == null)
            {
                Debug.LogError(
                    "menuPrefabsContainer is null! Did you assign it in SplashBootSettings?"
                );
                FailOutOfApplicationState();
                return false;
            }

            return true;
        }

        private async void LoadMenuFromAddressables()
        {
            if (gdprReferencePrefab == null)
            {
                // Loading the gdpr prefab from our prefab references
                handles
                    .LoadAssetAsync<GameObject>(
                        bootInitializer.menuPrefabsContainer.gdprUIReference
                    )
                    .Completed += op =>
                {
                    if (
                        op.Status
                        == UnityEngine
                            .ResourceManagement
                            .AsyncOperations
                            .AsyncOperationStatus
                            .Succeeded
                    )
                    {
                        gdprReferencePrefab = op.Result;
                        CreateMenu();
                    }
                    else
                    {
                        FailOutOfApplicationState();
                        Debug.LogError("Failed to load GDPR UI prefab from Addressables.");
                    }
                };
            }
        }

        private void CreateMenu()
        {
            // Instantiate the GDPR UI from the loaded prefab
            gdprMenuReference = GameObject
                .Instantiate(gdprReferencePrefab.gameObject)
                .GetComponent<GdprUIReference>();

            if (gdprMenuReference == null)
            {
                Debug.LogError("Instantiated GDPR UI does not have GdprUIReference component!");
                FailOutOfApplicationState();
                return;
            }

            WireButtons();
        }

        private void WireButtons()
        {
            // Adding listners from the menu
            gdprMenuReference.continueButton.onClick.AddListener(() =>
            {
                applicationData.ChangeApplicationState(ApplicationState.MainMenu);
                Object.Destroy(gdprMenuReference.gameObject);
            });
        }

        public ApplicationState Tick()
        {
            return ApplicationState.Splash;
        }

        public void LateTick() { }

        public void Dispose()
        {
            // Destroy instantiated GDPR UI if it still exists
            if (gdprMenuReference != null)
            {
                Object.Destroy(gdprMenuReference.gameObject);
                gdprMenuReference = null;
            }

            // Release all loaded addressable assets
            handles.ReleaseAll();
            handles.Dispose();
        }

        // what should happen if it fails out of creating something??

        private void FailOutOfApplicationState()
        {
            Debug.LogError("Failed out of Splash Application State.");
            //Dispose();
        }

        public void ExitApplicationState()
        {
            Dispose();
        }

        public void DisposeApplicationState()
        {
            Dispose();
        }
    }
}
