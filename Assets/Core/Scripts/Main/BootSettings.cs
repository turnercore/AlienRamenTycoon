using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core
{
    public abstract class BootSettings<TMenu> : ScriptableObject
        where TMenu : MenuReference
    {
        public AssetReferenceT<TMenu> menuReference;
        public GameModeSettings gameModeSettings;
    }
}
