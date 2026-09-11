using System.Collections;
using LegendsOfTheUniverse.Presentation;
using UnityEngine;

namespace Willbound.Table
{
    /// <summary>
    /// Engine A card body — binds engine instanceId to the presentation CardView shell.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LegendsOfTheUniverse.Presentation.CardView))]
    public sealed class CardView : MonoBehaviour
    {
        const float ExhaustTurnDegrees = 90f;
        const float ExhaustDuration = 0.18f;
        const float DragLift = 0.01f;
        const float GhostAlpha = 0.6f;

        [SerializeField] LegendsOfTheUniverse.Presentation.CardView presentation;
        [SerializeField] float hoverLift = 0.003f;

        int instanceId = -1;
        bool exhausted;
        Coroutine exhaustRoutine;
        Vector3 homePosition;
        Quaternion homeRotation;

        public int InstanceId => instanceId;
        public LegendsOfTheUniverse.Presentation.CardView Presentation => presentation;
        public bool IsDragging { get; private set; }

        void Awake()
        {
            if (presentation == null)
                presentation = GetComponent<LegendsOfTheUniverse.Presentation.CardView>();
        }

        public void BindInstance(int id)
        {
            instanceId = id;
        }

        public void RememberHome()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        public void BeginDrag()
        {
            IsDragging = true;
            RememberHome();
            SetGhost(true);
            transform.position += new Vector3(0f, DragLift, 0f);
        }

        public void UpdateDrag(Vector3 worldPosition)
        {
            if (!IsDragging)
                return;

            transform.position = worldPosition + new Vector3(0f, DragLift, 0f);
        }

        public void EndDrag(bool accepted)
        {
            IsDragging = false;
            SetGhost(false);

            if (!accepted)
                SnapHome();
        }

        public void SnapHome()
        {
            StartCoroutine(SnapHomeRoutine());
        }

        IEnumerator SnapHomeRoutine()
        {
            var start = transform.position;
            var end = homePosition;
            var duration = 0.2f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            transform.position = homePosition;
            transform.rotation = homeRotation;
        }

        public void SetExhausted(bool value)
        {
            if (exhausted == value)
                return;

            exhausted = value;
            if (exhaustRoutine != null)
                StopCoroutine(exhaustRoutine);

            exhaustRoutine = StartCoroutine(AnimateExhaust(value));
        }

        IEnumerator AnimateExhaust(bool turnExhausted)
        {
            var shell = presentation != null ? presentation.Shell : transform;
            var start = shell.localEulerAngles;
            var end = turnExhausted
                ? start + new Vector3(0f, ExhaustTurnDegrees, 0f)
                : new Vector3(start.x, 0f, start.z);

            var elapsed = 0f;
            while (elapsed < ExhaustDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / ExhaustDuration);
                shell.localEulerAngles = Vector3.Lerp(start, end, t);
                yield return null;
            }

            shell.localEulerAngles = end;
            exhaustRoutine = null;
        }

        public void SetHealth(int current, int max)
        {
            // Health chip UI hook — v1 logs; chip mesh can be added per set theme.
            if (current != max)
                gameObject.name = $"{gameObject.name} [{current}/{max}]";
        }

        void SetGhost(bool ghost)
        {
            if (presentation == null)
                return;

            // Opacity via emission scale on table materials.
            var scale = ghost ? GhostAlpha : 1f;
            transform.localScale = Vector3.one * (presentation.CardScale * scale);
        }
    }
}
