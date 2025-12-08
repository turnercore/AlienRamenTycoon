using System;
using Core;
using UnityEngine;

namespace Project
{
    public class OptionsMenuView : IApplicationLifecycle
    {
        private readonly MenuApplicationStateData menuApplicationStateData;
        private GameObject optionsMenuPrefab;
        private OptionsMenuReference optionsMenuReference;
        private readonly ApplicationData applicationData;
        private OptionsData optionsData;
        public Action OptionsClosed;

        public OptionsMenuView(
            GameObject optionsMenuPrefab,
            MenuApplicationStateData menuApplicationStateData,
            ApplicationData applicationData,
            OptionsData optionsData
        )
        {
            this.optionsMenuPrefab = optionsMenuPrefab;
            this.menuApplicationStateData = menuApplicationStateData;
            this.applicationData = applicationData;
            this.optionsData = optionsData;
        }

        public void Initialize()
        {
            optionsMenuReference = GameObject
                .Instantiate(optionsMenuPrefab)
                .GetComponent<OptionsMenuReference>();

            if (optionsMenuReference == null)
            {
                optionsMenuReference = GameObject.Instantiate(optionsMenuReference);
            }
            AddListeners();
            LoadSettings();
        }

        private void LoadSettings()
        {
            optionsMenuReference.masterVolumeSlider.value = optionsData.MasterVolume;
            optionsMenuReference.musicVolumeSlider.value = optionsData.MusicVolume;
            optionsMenuReference.sfxVolumeSlider.value = optionsData.SfxVolume;
            optionsMenuReference.voiceVolumeSlider.value = optionsData.VoiceVolume;
            optionsMenuReference.offlineModeToggle.isOn = optionsData.OfflineMode;
            optionsMenuReference.displayModeDropdown.value = (int)optionsData.DisplayMode;
        }

        private void AddListeners()
        {
            optionsMenuReference.offlineModeToggle.onValueChanged.AddListener(OnOfflineModeToggled);
            optionsMenuReference.displayModeDropdown.onValueChanged.AddListener(
                OnDisplayModeChanged
            );
            optionsMenuReference.backButton.onClick.AddListener(OnBackClicked);

            optionsMenuReference.masterVolumeSlider.onValueChanged.AddListener(
                OnMasterVolumeChanged
            );
            optionsMenuReference.musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            optionsMenuReference.sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            optionsMenuReference.voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        private void OnMasterVolumeChanged(float value)
        {
            Debug.Log("Master volume changed to: " + value);
            optionsData.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            Debug.Log($"Music volume changed to: {value}");
            optionsData.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            Debug.Log($"SFX volume changed to: {value}");
            optionsData.SetSfxVolume(value);
        }

        private void OnVoiceVolumeChanged(float value)
        {
            Debug.Log($"Voice volume changed to: {value}");
            optionsData.SetVoiceVolume(value);
        }

        private void OnOfflineModeToggled(bool isOn)
        {
            Debug.Log($"Offline mode toggled to: {isOn}");
            optionsData.SetOfflineMode(isOn);
        }

        private void OnDisplayModeChanged(int modeIndex)
        {
            Debug.Log($"Display mode changed to index: {modeIndex}");
            optionsData.SetDisplayMode((FullScreenMode)modeIndex);
        }

        private void OnBackClicked()
        {
            Debug.Log("Back button clicked - returning to main menu...");
            OptionsClosed?.Invoke();
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
            // Save options to player perfs if not already done
            optionsData.Save();
        }
    }
}
