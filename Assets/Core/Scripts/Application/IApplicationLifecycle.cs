namespace Core
{
    /// <summary>
    /// Prototype for every behaviour that needs to run tick/frame updates
    /// </summary>
    public interface IApplicationLifecycle
    {
        public void Initialize();
        public void Tick();
        public void Dispose();
    }
}
