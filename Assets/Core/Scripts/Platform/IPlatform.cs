using System.Collections;

namespace Core
{
    /// <summary>
    /// Add here your platforms as needed
    /// </summary>
    public enum DevicePlatform
    {
        Desktop,
        Web,
    }

    /// <summary>
    /// Factory interface for platform selection and initialization
    /// </summary>
    public interface IPlatform
    {
        IEnumerator Initialize(object applicationData);
        IApplicationLifecycle InputHandler();
        void Tick();
        void Dispose();
        void OnApplicationQuit();
    }
}
