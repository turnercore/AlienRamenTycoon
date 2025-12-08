using System;
using Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Project
{
    /// <summary>
    /// Application state for exiting the application.
    /// </summary>
    public class QuitApplicationState : IApplicationState
    {
        public bool IsApplicationStateInitialized { get; set; } = true;

        public void EnterApplicationState()
        {
            Debug.Log("Quitting application...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public ApplicationState Tick()
        {
            return ApplicationState.Exit;
        }

        public void LateTick() { }

        public void ExitApplicationState() { }

        public void Dispose() { }
    }
}
