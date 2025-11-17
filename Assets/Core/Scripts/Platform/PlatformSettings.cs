using UnityEngine;

namespace Core
{
    public abstract class PlatformSettings : ScriptableObject
    {
        public DevicePlatform devicePlatform;
        public InputSettings inputSettings;
    }
}
