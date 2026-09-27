using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    public static class GameMusic
    {
        public const string MainMenuTheme = "MainMenutheme";
        public const string BattleIntroTheme = "Battletheme Main";
        public const string BattleLoopTheme = "BattleTheme 1";

        public static AudioClip Load(string clipName)
        {
            var fromResources = Resources.Load<AudioClip>($"Sounds/{clipName}");
            if (fromResources != null)
                return Prepare(fromResources);

#if UNITY_EDITOR
            foreach (var extension in new[] { ".mp3", ".wav" })
            {
                var path = $"Assets/Sounds/{clipName}{extension}";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    return Prepare(clip);
            }

            return Prepare(AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/Sounds/{clipName}.mp3"));
#else
            return null;
#endif
        }

        public static AudioClip Prepare(AudioClip clip)
        {
            if (clip == null)
                return null;

            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            return clip;
        }

        public static System.Collections.IEnumerator WaitUntilReady(AudioClip clip)
        {
            if (clip == null)
                yield break;

            Prepare(clip);
            while (clip.loadState == AudioDataLoadState.Loading)
                yield return null;
        }
    }
}
