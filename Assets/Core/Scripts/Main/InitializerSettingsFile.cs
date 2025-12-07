using Core;
using Server;
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
        public AssetReferenceT<BootSettings<MenuReference>> bootAssetReference;
        public AssetReferenceT<NakamaSettings> nakamaServerSettings;
    }
}
