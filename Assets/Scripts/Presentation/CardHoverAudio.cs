using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    public static class CardHoverAudio
    {
        static readonly string[] HoverClipNames =
        {
            "Flipping Poker Card v1",
            "Flipping Poker Card v2",
            "Flipping Poker Card v3",
            "Flipping Poker Card v4",
        };

        const string CardMoveClipName = "card move sound";

        static AudioSource source;
        static AudioClip[] hoverClips;
        static AudioClip cardMoveClip;
        static bool initialized;

        public static void PlayHoverFlip()
        {
            EnsureInitialized();
            if (source == null || hoverClips == null || hoverClips.Length == 0)
                return;

            source.volume = Menu.GameSettings.EffectiveSfxVolume;
            source.PlayOneShot(hoverClips[Random.Range(0, hoverClips.Length)]);
        }

        public static void PlayCardMove()
        {
            EnsureInitialized();
            if (source == null || cardMoveClip == null)
                return;

            source.volume = Menu.GameSettings.EffectiveSfxVolume;
            source.PlayOneShot(cardMoveClip);
        }

        static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            var host = new GameObject("CardHoverAudio");
            Object.DontDestroyOnLoad(host);
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = Menu.GameSettings.EffectiveSfxVolume;

            var loaded = new List<AudioClip>();
            foreach (var clipName in HoverClipNames)
            {
                var clip = LoadClip(clipName);
                if (clip != null)
                    loaded.Add(clip);
            }

            hoverClips = loaded.ToArray();
            cardMoveClip = LoadClip(CardMoveClipName);

            if (hoverClips.Length == 0)
                Debug.LogWarning("CardHoverAudio: No flip sounds found in Resources/Sounds or Assets/Sounds.");

            if (cardMoveClip == null)
                Debug.LogWarning("CardHoverAudio: card move sound not found in Resources/Sounds or Assets/Sounds.");
        }

        static AudioClip LoadClip(string clipName)
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

            var resourcesPath = $"Assets/Resources/Sounds/{clipName}.mp3";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(resourcesPath);
#else
            return null;
#endif
        }
    }
}
