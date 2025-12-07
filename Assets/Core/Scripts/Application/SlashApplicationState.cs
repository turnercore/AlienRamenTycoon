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
            if (gdprReferencePrefab == null)
            {
                gdprReferencePrefab = Addressables
                    .LoadAssetAsync<GdprUIReference>(bootInitializer.menuReference)
                    .WaitForCompletion();
            }

            GdprUIReference gdprReference = GameObject.Instantiate<GdprUIReference>(
                gdprReferencePrefab
            );
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

        public void Dispose() { }

        public void ExitApplicationState() { }

        public void DisposeApplicationState() { }
    }
}
