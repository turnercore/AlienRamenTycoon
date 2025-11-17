using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public abstract class InputSettings : ScriptableObject
    {
        public InputMode inputMode;
        public InputAction debugMenu;
    }
}
