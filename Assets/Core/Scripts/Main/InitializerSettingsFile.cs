using Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project
{
    [CreateAssetMenu(
        menuName = "Project/Settings/General/InitializerSettingsFile",
        fileName = "InitializerSettingsFile"
    )]
    public class InitializerSettingsFile : ScriptableObject
    {
        public AssetReferenceT<DesktopPlatformSettings> desktopPlatformSettings;
        public AssetReferenceT<WebPlatformSettings> webPlatformSettings;
        public AssetReferenceT<BootstrapSettings> bootstrapAssetReference;
    }
}
