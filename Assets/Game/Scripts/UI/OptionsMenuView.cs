using System;
using Core;
using UnityEngine;

namespace Project
{
    /// <summary>
    /// Options Menu Overlay View.
    /// </summary>
    public class OptionsMenuView : IApplicationLifecycle
    {
        private readonly MenuApplicationStateData menuApplicationStateData;
        private GameObject optionsMenuPrefab;
        private OptionsMenuReference optionsMenuReference;
        private readonly ApplicationData applicationData;

        public OptionsMenuView(
            GameObject optionsMenuPrefab,
            MenuApplicationStateData menuApplicationStateData,
            ApplicationData applicationData
        )
        {
            this.optionsMenuPrefab = optionsMenuPrefab;
            this.menuApplicationStateData = menuApplicationStateData;
            this.applicationData = applicationData;
        }

        public void Initialize()
        {
            // Create the options menu reference from the prefab
            optionsMenuReference = GameObject
                .Instantiate(optionsMenuPrefab)
                .GetComponent<OptionsMenuReference>();

            if (optionsMenuReference == null)
            {
                optionsMenuReference = GameObject.Instantiate(optionsMenuReference);
            }

            // Set up listeners
            optionsMenuReference.offlineModeToggle.onValueChanged.AddListener(OnOfflineModeToggled);
            optionsMenuReference.displayModeDropdown.onValueChanged.AddListener(
                OnDisplayModeChanged
            );
            optionsMenuReference.backButton.onClick.AddListener(OnBackClicked);

            // vol sliders
            optionsMenuReference.masterVolumeSlider.onValueChanged.AddListener(
                OnMasterVolumeChanged
            );
            optionsMenuReference.musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            optionsMenuReference.sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            optionsMenuReference.voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        private void OnMasterVolumeChanged(float value)
        {
            Debug.Log($"Master volume changed to: {value}");
        }

        private void OnMusicVolumeChanged(float value)
        {
            Debug.Log($"Music volume changed to: {value}");
        }

        private void OnSfxVolumeChanged(float value)
        {
            Debug.Log($"SFX volume changed to: {value}");
        }

        private void OnVoiceVolumeChanged(float value)
        {
            Debug.Log($"Voice volume changed to: {value}");
        }

        private void OnOfflineModeToggled(bool isOn)
        {
            Debug.Log($"Offline mode toggled: {isOn}");
        }

        private void OnDisplayModeChanged(int modeIndex)
        {
            Debug.Log($"Display mode changed to index: {modeIndex}");
        }

        private void OnBackClicked()
        {
            Debug.Log("Back button clicked - returning to main menu...");
            Dispose();
        }

        public void Tick() { }

        public void Dispose()
        {
            if (optionsMenuReference == null)
            {
                return;
            }

            optionsMenuReference.offlineModeToggle.onValueChanged.RemoveListener(
                OnOfflineModeToggled
            );
            optionsMenuReference.displayModeDropdown.onValueChanged.RemoveListener(
                OnDisplayModeChanged
            );
            optionsMenuReference.backButton.onClick.RemoveListener(OnBackClicked);
            optionsMenuReference.masterVolumeSlider.onValueChanged.RemoveListener(
                OnMasterVolumeChanged
            );
            optionsMenuReference.musicVolumeSlider.onValueChanged.RemoveListener(
                OnMusicVolumeChanged
            );
            optionsMenuReference.sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            optionsMenuReference.voiceVolumeSlider.onValueChanged.RemoveListener(
                OnVoiceVolumeChanged
            );

            GameObject.Destroy(optionsMenuReference.gameObject);
            optionsMenuReference = null;
        }
    }
}
