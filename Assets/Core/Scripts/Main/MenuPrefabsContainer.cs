using Project;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MenuPrefabsContainer",
    menuName = "Project/Settings/Prefab Container/MenuPrefabContainer"
)]
public class MenuPrefabsContainer : ScriptableObject
{
    public MainMenuReference mainMenuReference;
}
