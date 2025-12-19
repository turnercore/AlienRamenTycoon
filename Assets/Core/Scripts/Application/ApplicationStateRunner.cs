using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// This is the class that will execute the update cycles for each game state. This should be the only place using Unity's Monobehaviour specific classes
    /// </summary>
    public class ApplicationStateRunner : MonoBehaviour
    {
        private IApplicationState currentState;
        private ApplicationState currentStateType;
        private Dictionary<ApplicationState, IApplicationState> applicationStates;
        private ApplicationData applicationData;
        private bool isRunnerUpdated;
        private bool isUnloading;
        private IPlatform platform;

        private ProfilerMarker changeApplicationStateMarker = new ProfilerMarker(
            "ChangeApplicationState"
        );
        private ProfilerMarker tickApplicationStateMarker = new ProfilerMarker(
            "ApplicationState_Tick"
        );

        public void Initialize(
            Dictionary<ApplicationState, IApplicationState> stateLookUp,
            ApplicationData applicationData,
            IPlatform selectedPlatform
        )
        {
            this.platform = selectedPlatform;
            applicationStates = stateLookUp;
            this.applicationData = applicationData;
            ChangeApplicationState(applicationData.ActiveApplicationState);
        }

        public void FixedUpdate()
        {
            if (currentState == null || !currentState.IsApplicationStateInitialized)
            {
                return;
            }
        }

        public void Update()
        {
            tickApplicationStateMarker.Begin();

            // Pre-ticks area
            platform.Tick();

            if (!isRunnerUpdated && currentState.IsApplicationStateInitialized)
            {
                isRunnerUpdated = true;
            }

            var nextState = currentStateType;
            if (isRunnerUpdated || isUnloading)
            {
                nextState = currentState.Tick();
                if (applicationData.ActiveApplicationState != currentStateType)
                {
                    nextState = applicationData.ActiveApplicationState;
                }
            }

            if (nextState != currentStateType)
            {
                ChangeApplicationState(nextState);
            }

            // Ticks area

            // Post-ticks area

            tickApplicationStateMarker.End();
        }

        public void LateUpdate()
        {
            if (currentState == null || !currentState.IsApplicationStateInitialized)
            {
                return;
            }
            currentState.LateTick();
        }

        public void OnDestroy()
        {
            currentState.Dispose();
        }

        private void ChangeApplicationState(ApplicationState newApplicationState)
        {
            changeApplicationStateMarker.Begin();
            if (currentState != null)
            {
                currentState.ExitApplicationState();
            }

            currentState = applicationStates[newApplicationState];
            currentState.EnterApplicationState();
            currentStateType = newApplicationState;
            applicationData.ChangeApplicationState(newApplicationState);
            isRunnerUpdated = false;
            Debug.Log($"Entered {newApplicationState} ApplicationState");
            changeApplicationStateMarker.End();
        }
    }
}
