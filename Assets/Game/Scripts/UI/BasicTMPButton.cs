using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    [RequireComponent(typeof(Button))]
    public class BasicTMPButton : MonoBehaviour
    {
        public TextMeshProUGUI TMPText;
        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
        }
    }
}
