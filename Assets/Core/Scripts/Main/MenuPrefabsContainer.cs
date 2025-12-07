using Project;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(
    fileName = "MenuPrefabsContainer",
    menuName = "Project/Settings/Prefab Container/MenuPrefabContainer"
)]
public class MenuPrefabsContainer : ScriptableObject
{
    public AssetReferenceT<MainMenuReference> mainMenuReference;
    public AssetReferenceT<GameObject> gdprUIReference;
}
