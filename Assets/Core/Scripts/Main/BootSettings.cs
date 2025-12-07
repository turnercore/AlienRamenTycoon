using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    public abstract class BootSettings : ScriptableObject
    {
        public MenuPrefabsContainer menuPrefabsContainer;
        public GameModeSettings gameModeSettings;
    }
}
