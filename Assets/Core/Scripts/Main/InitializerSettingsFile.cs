using Project;
using Server;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    [CreateAssetMenu(
        menuName = "Project/Settings/General/InitializerSettingsFile",
        fileName = "InitializerSettingsFile"
    )]
    public class InitializerSettingsFile : ScriptableObject
    {
        public AssetReferenceT<DesktopPlatformSettings> desktopPlatformSettings;
        public AssetReferenceT<WebPlatformSettings> webPlatformSettings;
        public AssetReferenceT<SplashBootSettings> splashBootSettings;
        public AssetReferenceT<MainMenuBootSettings> mainMenuBootSettings;
        public AssetReferenceT<GameModeBootSettings> gameModeBootSettings;
        public AssetReferenceT<NetworkSettings> networkSettings;
    }
}
