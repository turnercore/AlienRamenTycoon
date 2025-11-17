using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// The Game Mode Game State which is the base state in which all GameModes need to run.
    /// Game Modes are any modes that involve gameplay and gameplay related scenes. This state is where the game runs.
    /// </summary>
    public class GameModeApplicationState : IApplicationState
    {
        private readonly ApplicationData applicationData;
        private readonly GameModeSettings gameModeSettings;
        private IGameMode gameMode;

        public bool IsApplicationStateInitialized { get; private set; }

        public GameModeApplicationState(
            ApplicationData applicationData,
            GameModeSettings gameModeSettings
        )
        {
            this.applicationData = applicationData;
            this.gameModeSettings = gameModeSettings;
        }

        public void EnterApplicationState()
        {
            if (applicationData.ActiveGameMode == GameMode.Invalid)
            {
                SceneReference sceneReference = Object.FindFirstObjectByType<SceneReference>(); // if we would a scene manager, we could inject this reference
                if (gameModeSettings.gameModeData.Any(g => g.gameMode == sceneReference.gameMode))
                {
                    applicationData.ChangeGameModeState(sceneReference.gameMode);
                }
                InitializeGameMode();
                return;
            }
            GameModeData gameModeData = gameModeSettings.gameModeData.FirstOrDefault(g =>
                g.gameMode == applicationData.ActiveGameMode
            );
            if (gameModeData != null)
            {
                Addressables.LoadSceneAsync(gameModeData.scene, LoadSceneMode.Single).Completed +=
                    handle =>
                    {
                        InitializeGameMode();
                    };
            }
        }

        private void InitializeGameMode()
        {
            switch (applicationData.ActiveGameMode)
            {
                // case GameMode.Tileset:
                //     gameMode = new TileClashGameMode(applicationData);
                //     break;
                // can add more games/games modes this way
                default:
                    break;
            }
            gameMode?.EnterGameMode();
            IsApplicationStateInitialized = true;
        }

        public ApplicationState Tick()
        {
            if (gameMode.IsGameModeInitialized)
            {
                gameMode.Tick();
            }
            return ApplicationState.GameMode;
        }

        public void LateTick()
        {
            if (gameMode.IsGameModeInitialized)
            {
                gameMode.LateTick();
            }
        }

        public void Dispose() { }

        public void ExitApplicationState()
        {
            gameMode.ExitGameMode();
            gameMode.Dispose();
            applicationData.ChangeGameModeState(GameMode.Invalid);
        }
    }
}
