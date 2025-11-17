using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    [CreateAssetMenu(
        fileName = "DesktopInputSettings",
        menuName = "Core/Settings/Input/DesktopInputSettings"
    )]
    public class DesktopInputSettings : InputSettings
    {
        public InputAction movementAction;
        public InputAction selectAction;
        public InputAction cancelAction;
    }
}
