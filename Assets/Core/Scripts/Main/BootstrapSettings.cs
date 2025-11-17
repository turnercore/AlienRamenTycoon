using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project
{
    [CreateAssetMenu(
        menuName = "Project/Settings/General/BootstrapSettings",
        fileName = "BootstrapSettings"
    )]
    public class BootstrapSettings : ScriptableObject
    {
        public AssetReference menuScene;
        public GdprUIReference gdprUIReference;
        public GameModeSettings gameModeSettings;
        public AssetReferenceT<MenuPrefabsContainer> menuPrefabsContainer;
    }
}
