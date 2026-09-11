using System;
using System.Collections;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardView))]
    public class CardAnimator : MonoBehaviour
    {
        static readonly Quaternion FaceDownShellRotation = Quaternion.Euler(180f, 0f, 0f);
        static readonly Quaternion FaceUpShellRotation = Quaternion.identity;

        [SerializeField] CardView cardView;
        [SerializeField] float dealArcHeight = 0.35f;

        Coroutine activeRoutine;

        void Reset()
        {
            cardView = GetComponent<CardView>();
        }

        void Awake()
        {
            if (cardView == null)
                cardView = GetComponent<CardView>();
        }

        public bool IsAnimating => activeRoutine != null;

        public void Cancel()
        {
            if (activeRoutine == null)
                return;

            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        public void DealFaceDown(Vector3 start, Vector3 end, float duration, Action onComplete = null)
        {
            PlayRoutine(DealFaceDownRoutine(start, end, duration, onComplete));
        }

        public void FlipFaceUp(float duration, Action onComplete = null)
        {
            PlayRoutine(FlipFaceUpRoutine(duration, onComplete));
        }

        public IEnumerator DealFaceDownRoutine(Vector3 start, Vector3 end, float duration, Action onComplete = null)
        {
            cardView.SetFaceUpImmediate(false);
            transform.position = start;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var position = Vector3.Lerp(start, end, eased);
                position.y += dealArcHeight * Mathf.Sin(eased * Mathf.PI);
                transform.position = position;
                yield return null;
            }

            transform.position = end;
            onComplete?.Invoke();
        }

        public IEnumerator FlipFaceUpRoutine(float duration, Action onComplete = null)
        {
            var shell = cardView.Shell;
            if (shell == null)
                yield break;

            var elapsed = 0f;
            var from = FaceDownShellRotation;
            var to = FaceUpShellRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                shell.localRotation = Quaternion.Slerp(from, to, eased);
                yield return null;
            }

            cardView.SetFaceUpImmediate(true);
            onComplete?.Invoke();
        }

        void PlayRoutine(IEnumerator routine)
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(RunAndClear(routine));
        }

        IEnumerator RunAndClear(IEnumerator routine)
        {
            yield return routine;
            activeRoutine = null;
        }

        public IEnumerator AnimateToRoutine(Vector3 targetPosition, float targetScale, float duration, Quaternion? targetRotation = null)
        {
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var endRotation = targetRotation ?? startRotation;
            var startScale = cardView.CardScale;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                cardView.SetCardScale(Mathf.Lerp(startScale, targetScale, t));
                yield return null;
            }

            transform.position = targetPosition;
            transform.rotation = endRotation;
            cardView.SetCardScale(targetScale);
        }
    }
}
