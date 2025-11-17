using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project
{
    [CreateAssetMenu(
        menuName = "Project/Settings/General/DelayLoadSettingsFile",
        fileName = "DelayLoadSettingsFile"
    )]
    public class BootstrapSettings : ScriptableObject
    {
        public AssetReference menuScene;
        public GdprUIReference gdprUIReference;
        public GameModeSettings gameModeSettings;
        public AssetReferenceT<MenuPrefabsContainer> menuPrefabsContainer;
    }
}
