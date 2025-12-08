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

        public MainMenuView(
            MainMenuReference mainMenuReference, // Use the specific type
            MenuApplicationStateData menuApplicationStateData
        )
        {
            this.mainMenuReference = mainMenuReference;
            this.menuApplicationStateData = menuApplicationStateData;
        }

        public void Initialize()
        {
            if (mainMenuReference == null)
            {
                mainMenuReference = GameObject.Instantiate(mainMenuReference);
            }
            mainMenuReference.exitButton.onClick.AddListener(Application.Quit);
            mainMenuReference.optionsButton.onClick.AddListener(() =>
                throw new NotImplementedException()
            );
            mainMenuReference.playButton.onClick.AddListener(() =>
                menuApplicationStateData.startGameRequests.Invoke(GameMode.Spaceship)
            );
        }

        public void Tick() { }

        public void Dispose() { }
    }
}
