namespace Core
{
    /// <summary>
    /// Base class used by the platform factory to select the current platform and the input
    /// </summary>
    public class PlatformSelector
    {
        public readonly DevicePlatform devicePlatform;
        private InputMode inputMode;

        public PlatformSelector(DevicePlatform devicePlatform, InputMode inputMode)
        {
            this.devicePlatform = devicePlatform;
            this.inputMode = inputMode;
        }

        public void SetInputMode(InputMode newInputMode)
        {
            inputMode = newInputMode;
        }

        public static InputMode GetPlatformDefaultInputMode()
        {
#if UNITY_EDITOR
            return InputMode.Desktop;
#else
            return InputMode.Desktop;
#endif
        }

        public static DevicePlatform GetDevicePlatform()
        {
#if UNITY_EDITOR
            return DevicePlatform.Desktop;
#else
            return DevicePlatform.Desktop;
#endif
        }
    }
}
