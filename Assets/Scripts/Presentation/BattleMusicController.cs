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
            AudioListenerBootstrap.EnsureExists();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.loop = false;
            UpdateVolume();
            StartCoroutine(PlayBattleMusicRoutine());
        }

        IEnumerator PlayBattleMusicRoutine()
        {
            yield return null;
            AudioListenerBootstrap.EnsureExists();
            UpdateVolume();

            var intro = GameMusic.Load(GameMusic.BattleIntroTheme);
            var loop = GameMusic.Load(GameMusic.BattleLoopTheme);

            if (intro == null && loop == null)
            {
                Debug.LogWarning("BattleMusicController: No battle music clips found.");
                yield break;
            }

            if (intro != null)
            {
                yield return GameMusic.WaitUntilReady(intro);
                if (intro.loadState == AudioDataLoadState.Loaded)
                {
                    musicSource.clip = intro;
                    musicSource.loop = false;
                    UpdateVolume();
                    musicSource.Play();
                    while (musicSource != null && musicSource.isPlaying)
                        yield return null;
                }
            }

            if (loop == null)
                yield break;

            yield return GameMusic.WaitUntilReady(loop);
            if (loop.loadState != AudioDataLoadState.Loaded)
                yield break;

            musicSource.clip = loop;
            musicSource.loop = true;
            UpdateVolume();
            musicSource.Play();
        }

        void LateUpdate()
        {
            UpdateVolume();
        }

        void UpdateVolume()
        {
            if (musicSource != null)
                musicSource.volume = Menu.GameSettings.EffectiveMusicVolume;
        }
    }
}
