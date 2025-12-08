using Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project
{
    public class SplashView : IApplicationLifecycle
    {
        private readonly SplashMenuReference splashMenuReference;
        private readonly ApplicationData applicationData;

        public SplashView(SplashMenuReference splashMenuReference, ApplicationData applicationData)
        {
            this.splashMenuReference = splashMenuReference;
            this.applicationData = applicationData;
        }

        public void Initialize()
        {
            if (splashMenuReference == null)
            {
                Debug.LogError("SplashMenuReference is null in SplashView");
                return;
            }

            splashMenuReference.acceptButton.onClick.AddListener(OnAcceptClicked);
            splashMenuReference.declineButton.onClick.AddListener(OnDeclineClicked);
        }

        private void OnAcceptClicked()
        {
            applicationData.ChangeApplicationState(ApplicationState.MainMenu);
        }

        private void OnDeclineClicked()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Tick() { }

        public void Dispose()
        {
            if (splashMenuReference == null)
            {
                return;
            }

            splashMenuReference.acceptButton.onClick.RemoveListener(OnAcceptClicked);
            splashMenuReference.declineButton.onClick.RemoveListener(OnDeclineClicked);
        }
    }
}
