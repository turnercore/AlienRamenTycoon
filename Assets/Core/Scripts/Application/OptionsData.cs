using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Centralized storage for user-configurable options (volume, display mode, offline toggle, etc.).
    /// Persists values via PlayerPrefs for now so other systems can query/apply them easily.
    /// </summary>
    public sealed class OptionsData : IDisposable
    {
        private const string MasterKey = "options.audio.master";
        private const string MusicKey = "options.audio.music";
        private const string SfxKey = "options.audio.sfx";
        private const string VoiceKey = "options.audio.voice";
        private const string OfflineKey = "options.offline";
        private const string DisplayModeKey = "options.displayMode";
        private DisplayModeApplier displayModeApplier;

        // ---- Events other systems can subscribe to ----
        public event Action<float> MasterVolumeChanged;
        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<float> VoiceVolumeChanged;

        public event Action<bool> OfflineModeChanged;

        public event Action<FullScreenMode> DisplayModeChanged;

        // ---- Current values ----
        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float VoiceVolume { get; private set; } = 1f;
        public bool OfflineMode { get; private set; }
        public FullScreenMode DisplayMode { get; private set; } = FullScreenMode.Windowed;

        public void Load(bool raiseEvents = false)
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            VoiceVolume = PlayerPrefs.GetFloat(VoiceKey, 1f);
            OfflineMode = PlayerPrefs.GetInt(OfflineKey, 0) == 1;
            DisplayMode = (FullScreenMode)
                PlayerPrefs.GetInt(DisplayModeKey, (int)FullScreenMode.Windowed);

            if (!raiseEvents)
            {
                return;
            }

            MasterVolumeChanged?.Invoke(MasterVolume);
            MusicVolumeChanged?.Invoke(MusicVolume);
            SfxVolumeChanged?.Invoke(SfxVolume);
            VoiceVolumeChanged?.Invoke(VoiceVolume);
            OfflineModeChanged?.Invoke(OfflineMode);
            DisplayModeChanged?.Invoke(DisplayMode);

            // Create the DisplayModeApplier
            displayModeApplier = new DisplayModeApplier(this);
        }

        public void SetMasterVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(MasterVolume, value))
            {
                return;
            }

            MasterVolume = value;
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            MasterVolumeChanged?.Invoke(MasterVolume);
        }

        public void SetMusicVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(MusicVolume, value))
            {
                return;
            }

            MusicVolume = value;
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            MusicVolumeChanged?.Invoke(MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(SfxVolume, value))
            {
                return;
            }

            SfxVolume = value;
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            SfxVolumeChanged?.Invoke(SfxVolume);
        }

        public void SetVoiceVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(VoiceVolume, value))
            {
                return;
            }

            VoiceVolume = value;
            PlayerPrefs.SetFloat(VoiceKey, VoiceVolume);
            VoiceVolumeChanged?.Invoke(VoiceVolume);
        }

        public void SetOfflineMode(bool isOffline)
        {
            if (OfflineMode == isOffline)
            {
                return;
            }

            OfflineMode = isOffline;
            PlayerPrefs.SetInt(OfflineKey, isOffline ? 1 : 0);
            OfflineModeChanged?.Invoke(OfflineMode);
        }

        public void SetDisplayMode(FullScreenMode mode)
        {
            if (DisplayMode == mode)
            {
                return;
            }

            DisplayMode = mode;
            PlayerPrefs.SetInt(DisplayModeKey, (int)mode);
            DisplayModeChanged?.Invoke(DisplayMode);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        public void Dispose()
        {
            displayModeApplier?.Dispose();
            displayModeApplier = null;
        }
    }

    // Helper class to apply Display Modes
    public class DisplayModeApplier : IDisposable
    {
        private readonly OptionsData optionsData;

        public DisplayModeApplier(OptionsData optionsData)
        {
            this.optionsData = optionsData;

            // Apply on startup
            Apply(optionsData.DisplayMode);

            // Apply whenever it changes
            optionsData.DisplayModeChanged += Apply;
        }

        private void Apply(FullScreenMode mode)
        {
            // If you only care about mode (not resolution):
            Screen.fullScreenMode = mode;

            // If you want to control resolution too, something like:
            // var width = Screen.currentResolution.width;
            // var height = Screen.currentResolution.height;
            // Screen.SetResolution(width, height, mode);
        }

        public void Dispose()
        {
            optionsData.DisplayModeChanged -= Apply;
        }
    }
}
