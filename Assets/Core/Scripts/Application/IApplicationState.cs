namespace Core
{
    /// <summary>
    /// Add or modify project specific game states
    /// </summary>
    public enum ApplicationState
    {
        Invalid,
        Splash,
        Loading,
        MainMenu,
        GameMode,
    }

    /// <summary>
    /// Prototype for creating application states
    /// </summary>
    public interface IApplicationState
    {
        public void EnterApplicationState();
        public ApplicationState Tick();
        public void LateTick();
        public void ExitApplicationState();
        void Dispose();
        bool IsApplicationStateInitialized { get; }
    }
}
