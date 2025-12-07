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
        private GdprUIReference gdprReferencePrefab;

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
            Debug.Log("SplashApplicationState.EnterApplicationState() called");

            if (bootInitializer == null)
            {
                Debug.LogError("bootInitializer is null!");
                return;
            }

            if (bootInitializer.menuPrefabsContainer == null)
            {
                Debug.LogError(
                    "menuPrefabsContainer is null! Did you assign it in SplashBootSettings?"
                );
                return;
            }

            Debug.Log(
                $"Loading GDPR UI Reference from: {bootInitializer.menuPrefabsContainer.gdprUIReference}"
            );

            if (gdprReferencePrefab == null)
            {
                var prefab = Addressables
                    .LoadAssetAsync<GameObject>(
                        bootInitializer.menuPrefabsContainer.gdprUIReference
                    )
                    .WaitForCompletion();

                if (prefab == null)
                {
                    Debug.LogError("Failed to load GDPR prefab!");
                    return;
                }

                Debug.Log($"Loaded prefab: {prefab.name}");
                gdprReferencePrefab = GameObject
                    .Instantiate(prefab)
                    .GetComponent<GdprUIReference>();

                if (gdprReferencePrefab == null)
                {
                    Debug.LogError("Prefab does not have GdprUIReference component!");
                    return;
                }
            }

            GdprUIReference gdprReference = GameObject
                .Instantiate(gdprReferencePrefab.gameObject)
                .GetComponent<GdprUIReference>();

            Debug.Log($"Instantiated GDPR UI: {gdprReference.gameObject.name}");

            gdprReference.continueButton.onClick.AddListener(() =>
            {
                applicationData.ChangeApplicationState(ApplicationState.MainMenu);
                Object.Destroy(gdprReference.gameObject);
            });
        }

        public ApplicationState Tick()
        {
            return ApplicationState.Splash;
        }

        public void LateTick() { }

        public void Dispose()
        {
            // Clean up addressable resources if needed
            Addressables.Release(gdprReferencePrefab);
        }

        public void ExitApplicationState() { }

        public void DisposeApplicationState() { }
    }
}
