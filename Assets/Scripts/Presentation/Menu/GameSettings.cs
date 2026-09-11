using UnityEngine;

namespace LegendsOfTheUniverse.Presentation.Menu
{
    public static class GameSettings
    {
        const string MasterVolumeKey = "settings.masterVolume";
        const string SfxVolumeKey = "settings.sfxVolume";
        const string MusicVolumeKey = "settings.musicVolume";
        const string FullscreenKey = "settings.fullscreen";
        const string QualityKey = "settings.quality";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
            set => PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f);
            set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
            set => PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            set => PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        }

        public static int QualityLevel
        {
            get => PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            set => PlayerPrefs.SetInt(QualityKey, Mathf.Clamp(value, 0, QualitySettings.names.Length - 1));
        }

        public static float EffectiveSfxVolume => MasterVolume * SfxVolume;
        public static float EffectiveMusicVolume => MasterVolume * MusicVolume;

        public static void ApplyAll()
        {
            Screen.fullScreen = Fullscreen;
            QualitySettings.SetQualityLevel(QualityLevel, true);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.Save();
        }
    }
}
