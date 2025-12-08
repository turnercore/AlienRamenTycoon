using Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class OptionsMenuReference : MenuReference
    {
        // vol sliders
        //main
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Slider voiceVolumeSlider;

        // offline mode (no network calls)
        public Toggle offlineModeToggle;

        // fullscreen/windowed/borderless dropdown
        public TMP_Dropdown displayModeDropdown;

        public Button backButton;
    }
}
