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
                return fromResources;

#if UNITY_EDITOR
            foreach (var extension in new[] { ".mp3", ".wav" })
            {
                var path = $"Assets/Sounds/{clipName}{extension}";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    return clip;
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/Sounds/{clipName}.mp3");
#else
            return null;
#endif
        }
    }
}
