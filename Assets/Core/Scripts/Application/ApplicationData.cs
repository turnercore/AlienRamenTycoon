using Server;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// This class is meant to hold application essential data for different game states
    /// </summary>
    public class ApplicationData
    {
        public PlatformSelector platformSelector;

        // Global network service for all game states
        public INetworkService Network { get; set; }

        public ApplicationState ActiveApplicationState { get; private set; }
        public GameMode ActiveGameMode { get; private set; }

        public OptionsData optionsData { get; private set; }

        public void ChangeApplicationState(ApplicationState applicationState)
        {
            // Cleanup old application state
            this.ActiveApplicationState = applicationState;
        }

        public void ChangeGameModeState(GameMode gameMode)
        {
            if (ActiveApplicationState != ApplicationState.GameMode && gameMode != GameMode.Invalid)
            {
                Debug.LogError(
                    "Cannot change Game Mode state in any other Application State than ApplicationState.GameMode"
                );
                return;
            }
            this.ActiveGameMode = gameMode;
        }
    }
}
