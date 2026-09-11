using System.Collections;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Plays Battletheme Main once, then loops BattleTheme 1 for the match.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public class BattleMusicController : MonoBehaviour
    {
        AudioSource musicSource;

        void Awake()
        {
            Menu.GameSettings.ApplyAll();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            UpdateVolume();
            StartCoroutine(PlayBattleMusicRoutine());
        }

        IEnumerator PlayBattleMusicRoutine()
        {
            var intro = GameMusic.Load(GameMusic.BattleIntroTheme);
            var loop = GameMusic.Load(GameMusic.BattleLoopTheme);

            if (intro == null && loop == null)
            {
                Debug.LogWarning("BattleMusicController: No battle music clips found.");
                yield break;
            }

            if (intro != null)
            {
                musicSource.clip = intro;
                musicSource.loop = false;
                musicSource.Play();
                yield return new WaitWhile(() => musicSource.isPlaying);
            }

            if (loop == null)
                yield break;

            musicSource.clip = loop;
            musicSource.loop = true;
            musicSource.Play();
        }

        void UpdateVolume()
        {
            if (musicSource != null)
                musicSource.volume = Menu.GameSettings.EffectiveMusicVolume;
        }
    }
}
