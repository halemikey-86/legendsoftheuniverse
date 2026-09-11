using System.Collections;
using LegendsOfTheUniverse.Presentation;
using UnityEngine;

namespace Willbound.Table
{
    /// <summary>
    /// Will pool display + payment cinema (coins/pips soak into card Will badge).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WillPoolView : MonoBehaviour
    {
        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] float paymentDuration = 0.35f;

        int displayedWill = -1;

        void Awake()
        {
            if (playmatZones == null)
                playmatZones = GetComponent<PlaymatZonesView>();
        }

        public void SetWill(int amount)
        {
            if (displayedWill == amount)
                return;

            displayedWill = amount;
            playmatZones?.SetWillPool(amount);
        }

        public void PlayWillPayment(int amount, Vector3 cardWorldPosition)
        {
            if (amount <= 0)
                return;

            StartCoroutine(PaymentRoutine(amount, cardWorldPosition, PlaymatZones.WillRoundTrack, false));
        }

        public void PlayWorthPayment(int amount, Vector3 cardWorldPosition)
        {
            if (amount <= 0)
                return;

            StartCoroutine(PaymentRoutine(amount, cardWorldPosition, PlaymatZones.Worth, true));
        }

        public void PlayWillRefund(int amount, Vector3 fromCardWorldPosition)
        {
            if (amount <= 0)
                return;

            StartCoroutine(RefundRoutine(amount, fromCardWorldPosition, PlaymatZones.WillRoundTrack));
        }

        IEnumerator PaymentRoutine(int amount, Vector3 target, Vector3 poolAnchor, bool worth)
        {
            var start = poolAnchor;
            var end = target + Vector3.up * 0.05f;
            var elapsed = 0f;

            while (elapsed < paymentDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / paymentDuration);
                // v1: pool dial ticks; full coin mesh can be added per set theme.
                _ = Vector3.Lerp(start, end, t);
                yield return null;
            }

            if (!worth)
                SetWill(Mathf.Max(0, displayedWill - amount));
        }

        IEnumerator RefundRoutine(int amount, Vector3 fromCard, Vector3 poolAnchor)
        {
            var elapsed = 0f;
            while (elapsed < paymentDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetWill(displayedWill + amount);
        }
    }
}
