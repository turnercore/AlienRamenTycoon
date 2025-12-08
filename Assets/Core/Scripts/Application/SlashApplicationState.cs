using Core;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        private SplashMenuReference splashMenuReference;
        private SplashView splashView;
        private readonly AddressablesHandleHelper addressableHandles = new();

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

        private void LoadMenuFromAddressables()
        {
            addressableHandles.LoadAssetAsync<GameObject>(
                bootInitializer.menuPrefabsContainer.splashMenuReference,
                handle => OnSplashPrefabLoaded(handle)
            );
        }

        private void OnSplashPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError("Failed to load splash UI reference.");
                FailOutOfApplicationState();
                return;
            }

            splashMenuReference = GameObject
                .Instantiate(handle.Result)
                .GetComponent<SplashMenuReference>();

            if (splashMenuReference == null)
            {
                Debug.LogError(
                    "Instantiated splash UI does not have SplashMenuReference component!"
                );
                FailOutOfApplicationState();
                return;
            }

            splashView = new SplashView(splashMenuReference, applicationData);
            splashView.Initialize();
        }

        public ApplicationState Tick()
        {
            return ApplicationState.Splash;
        }

        public void LateTick() { }

        public void Dispose()
        {
            splashView?.Dispose();
            if (splashMenuReference != null)
            {
                Object.Destroy(splashMenuReference.gameObject);
                splashMenuReference = null;
            }

            addressableHandles.Dispose();
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
