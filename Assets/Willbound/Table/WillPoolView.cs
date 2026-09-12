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

            StartCoroutine(PaymentRoutine(amount, worth: false));
        }

        public void PlayWorthPayment(int amount, Vector3 cardWorldPosition)
        {
            if (amount <= 0)
                return;

            StartCoroutine(PaymentRoutine(amount, worth: true));
        }

        public void PlayWillRefund(int amount, Vector3 fromCardWorldPosition)
        {
            if (amount <= 0)
                return;

            StartCoroutine(RefundRoutine(amount));
        }

        IEnumerator PaymentRoutine(int amount, bool worth)
        {
            // v1: pool dial ticks after a beat; full coin-flight cinema (toward the screen-anchored
            // Will/Worth HUD) can be added per set theme.
            yield return new WaitForSeconds(paymentDuration);

            if (!worth)
                SetWill(Mathf.Max(0, displayedWill - amount));
        }

        IEnumerator RefundRoutine(int amount)
        {
            yield return new WaitForSeconds(paymentDuration);

            SetWill(displayedWill + amount);
        }
    }
}
