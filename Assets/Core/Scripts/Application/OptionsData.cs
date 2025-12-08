using UnityEngine;

namespace Core
{
    /// <summary>
    /// Centralized storage for user-configurable options (volume, display mode, offline toggle, etc.).
    /// Persists values via PlayerPrefs for now so other systems can query/apply them easily.
    /// </summary>
    public sealed class OptionsData
    {
        private const string MasterKey = "options.audio.master";
        private const string MusicKey = "options.audio.music";
        private const string SfxKey = "options.audio.sfx";
        private const string VoiceKey = "options.audio.voice";
        private const string OfflineKey = "options.offline";
        private const string DisplayModeKey = "options.displayMode";

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public float VoiceVolume { get; private set; } = 1f;
        public bool OfflineMode { get; private set; }
        public FullScreenMode DisplayMode { get; private set; } = FullScreenMode.Windowed;

        public void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            VoiceVolume = PlayerPrefs.GetFloat(VoiceKey, 1f);
            OfflineMode = PlayerPrefs.GetInt(OfflineKey, 0) == 1;
            DisplayMode = (FullScreenMode)
                PlayerPrefs.GetInt(DisplayModeKey, (int)FullScreenMode.Windowed);
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        }

        public void SetVoiceVolume(float value)
        {
            VoiceVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VoiceKey, VoiceVolume);
        }

        public void SetOfflineMode(bool isOffline)
        {
            OfflineMode = isOffline;
            PlayerPrefs.SetInt(OfflineKey, isOffline ? 1 : 0);
        }

        public void SetDisplayMode(FullScreenMode mode)
        {
            DisplayMode = mode;
            PlayerPrefs.SetInt(DisplayModeKey, (int)mode);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
