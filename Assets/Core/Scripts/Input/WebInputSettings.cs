using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    [CreateAssetMenu(
        fileName = "WebInputSettings",
        menuName = "Core/Settings/Input/WebInputSettings"
    )]
    public class WebInputSettings : InputSettings
    {
        public InputAction movementAction;
        public InputAction selectAction;
        public InputAction cancelAction;
    }
}
