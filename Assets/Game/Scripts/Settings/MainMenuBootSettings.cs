using Core;
using UnityEngine;

namespace Project
{
    [CreateAssetMenu(
        menuName = "Project/Settings/BootSettings/MainMenuBootSettings",
        fileName = "MainMenuBootSettings"
    )]
    public class MainMenuBootSettings : BootSettings<MainMenuReference> { }
}
