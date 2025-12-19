namespace Core
{
    /// <summary>
    /// Prototype for every behaviour that needs to run tick/frame updates
    /// </summary>
    public interface IApplicationLifecycle
    {
        //Update Initialize so it can be async and return IEnumerator if needed
        public void Initialize();
        public void Tick();
        public void Dispose();
    }
}
