namespace Core
{
    public enum GameMode
    {
        Invalid,
        Tileset,
    }

    /// <summary>
    /// Prototype for crating new game modes
    /// </summary>
    public interface IGameMode
    {
        bool IsGameModeInitialized { get; }

        public void EnterGameMode();
        public void Tick();
        public void LateTick();
        void ExitGameMode();
        void Dispose();
    }
}
