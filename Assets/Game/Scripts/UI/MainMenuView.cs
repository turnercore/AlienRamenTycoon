using System;
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

        public MainMenuView(
            MainMenuReference mainMenuReference, // Use the specific type
            MenuApplicationStateData menuApplicationStateData,
            ApplicationData applicationData
        )
        {
            this.mainMenuReference = mainMenuReference;
            this.menuApplicationStateData = menuApplicationStateData;
            this.applicationData = applicationData;
        }

        public void Initialize()
        {
            if (mainMenuReference == null)
            {
                mainMenuReference = GameObject.Instantiate(mainMenuReference);
            }
            mainMenuReference.exitButton.onClick.AddListener(OnQuitClicked);
            mainMenuReference.optionsButton.onClick.AddListener(() =>
                throw new NotImplementedException()
            );
            mainMenuReference.playButton.onClick.AddListener(() =>
                menuApplicationStateData.startGameRequests.Invoke(GameMode.Spaceship)
            );
        }

        private void OnQuitClicked()
        {
            // Change to a Quit Application State
            applicationData.ChangeApplicationState(ApplicationState.Exit);
        }

        public void Tick() { }

        public void Dispose() { }
    }
}
