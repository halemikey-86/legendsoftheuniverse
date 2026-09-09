using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    public class HandView : MonoBehaviour
    {
        readonly struct HandSlot
        {
            public HandSlot(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }

        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        [Header("Opening Spread")]
        [SerializeField] int openingHandSize = 7;
        [SerializeField] float dealScale = PlaymatZones.CardScale;
        [SerializeField] Vector3 dealHandCenter = PlaymatZones.OpeningHandCenter;
        [SerializeField] float dealSpreadSpacing = 4.26f;
        [SerializeField] float dealDuration = 0.35f;
        [SerializeField] float dealStagger = 0.05f;
        [SerializeField] float flipDuration = 0.22f;
        [SerializeField] float flipStagger = 0.04f;

        [Header("Kept Hand (bottom of table)")]
        [SerializeField] float keptScale = PlaymatZones.CardScale;
        [SerializeField] Vector3 keptHandCenter = PlaymatZones.HandCenter;
        [SerializeField] float keptSpreadSpacing = 4.26f;
        [SerializeField] float keepAnimDuration = 0.45f;

        [Header("Hover")]
        [SerializeField] float hoverScaleMultiplier = 1.14f;
        [SerializeField] float hoverLift = 0.06f;
        [SerializeField] float hoverForward = 0.18f;
        [SerializeField] float hoverAnimDuration = 0.12f;

        [Header("Inspect")]
        [SerializeField] float inspectScale = PlaymatZones.CardScale;
        [SerializeField] Vector3 inspectPosition = new(0f, 0.1f, -1.5f);
        [SerializeField] float inspectAnimDuration = 0.25f;

        [Header("Drag Reorder")]
        [SerializeField] float dragScreenThreshold = 12f;
        [SerializeField] float dragLift = 0.1f;
        [SerializeField] float dragReorderDuration = 0.2f;

        readonly List<CardView> handCards = new();
        CardView selectedCard;
        CardView hoveredCard;
        CardView draggingCard;
        CardView pointerCard;
        Vector3 pointerDownScreen;
        bool pointerDragStarted;
        bool handKept;
        StoreActionController storeActions;
        StoreView storeView;

        public bool HandKept => handKept;
        public bool IsDealing { get; private set; }
        public bool IsDragging => draggingCard != null;
        public bool CanClickCards => handKept && !IsDealing;
        public bool CanReorderCards =>
            handKept
            && !IsDealing
            && selectedCard == null
            && draggingCard == null
            && (storeActions == null || storeActions.CurrentMode == StoreActionMode.None);
        public bool HasInspectSelection => selectedCard != null;

        public void BindStoreActions(StoreActionController controller)
        {
            storeActions = controller;
        }

        public void BindStoreView(StoreView store)
        {
            storeView = store;
        }

        public void ClearInspectSelection()
        {
            if (selectedCard == null)
                return;

            selectedCard = null;
            StartCoroutine(LayoutKeptHandRoutine());
            storeView?.RestoreLayoutUnlessInspecting();
        }

        public void ResetSession()
        {
            handKept = false;
            selectedCard = null;
            hoveredCard = null;
            draggingCard = null;
            pointerCard = null;
            pointerDragStarted = false;
            IsDealing = false;
            ClearHand();
        }

        public IEnumerator DealOpeningHandRoutine(Vector3 deckPosition)
        {
            IsDealing = true;
            handKept = false;
            selectedCard = null;
            ClearHand();

            if (cardPrefab == null)
            {
                Debug.LogWarning("HandView: cardPrefab is not assigned.");
                IsDealing = false;
                yield break;
            }

            var handFronts = CardDeck.Draw(openingHandSize);
            if (handFronts.Count == 0)
            {
                Debug.LogWarning("HandView: No cards available to deal.");
                IsDealing = false;
                yield break;
            }

            var spacing = dealSpreadSpacing > 0f
                ? dealSpreadSpacing
                : CardLayout.SpreadSpacing(dealScale);
            var slots = new List<HandSlot>(handFronts.Count);
            for (var i = 0; i < handFronts.Count; i++)
            {
                var slot = GetSpreadSlot(i, handFronts.Count, dealHandCenter, spacing, i * 0.002f);
                var card = SpawnHandCard(handFronts[i], deckPosition);
                handCards.Add(card);
                slots.Add(slot);
            }

            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                var slot = slots[i];
                var animator = card.GetComponent<CardAnimator>();

                if (animator != null)
                    StartCoroutine(animator.DealFaceDownRoutine(deckPosition, slot.Position, dealDuration));
                else
                {
                    card.SetFaceUpImmediate(false);
                    ApplySlot(card, slot, dealScale);
                }

                if (dealStagger > 0f && i < handCards.Count - 1)
                    yield return new WaitForSeconds(dealStagger);
            }

            yield return new WaitForSeconds(dealDuration);

            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (card == null)
                    continue;

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    StartCoroutine(FlipWithDelay(animator, i * flipStagger));
                else
                    card.SetFaceUpImmediate(true);
            }

            yield return new WaitForSeconds(flipDuration + ((handCards.Count - 1) * flipStagger));

            foreach (var card in handCards)
                card?.SetClickable(true);

            IsDealing = false;
        }

        IEnumerator FlipWithDelay(CardAnimator animator, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            yield return animator.FlipFaceUpRoutine(flipDuration);
        }

        public void KeepHand()
        {
            if (handKept || IsDealing)
                return;

            StartCoroutine(KeepHandRoutine());
        }

        public IEnumerator RunKeepHandRoutine()
        {
            if (handKept || IsDealing)
                yield break;

            yield return KeepHandRoutine();
        }

        public void HandleCardClicked(CardView card)
        {
            if (!CanClickCards || !handCards.Contains(card))
                return;

            if (storeActions != null && storeActions.CurrentMode != StoreActionMode.None)
            {
                storeActions.HandleHandCardClicked(card);
                return;
            }

            HandleInspectCard(card);
        }

        public void HandleCardPointerDown(CardView card)
        {
            if (!CanClickCards || !handCards.Contains(card))
                return;

            pointerCard = card;
            pointerDragStarted = false;
            pointerDownScreen = GetMouseScreenPosition();
        }

        public void HandleCardDrag(CardView card)
        {
            if (pointerCard != card || !handCards.Contains(card))
                return;

            if (!CanReorderCards)
                return;

            if (!pointerDragStarted)
            {
                if (Vector3.Distance(GetMouseScreenPosition(), pointerDownScreen) < dragScreenThreshold)
                    return;

                BeginDrag(card);
            }

            UpdateDragPosition(card);
        }

        public void HandleCardPointerUp(CardView card)
        {
            if (pointerCard != card)
                return;

            if (pointerDragStarted && draggingCard == card)
                EndDrag(card);
            else
                HandleCardClicked(card);

            pointerCard = null;
            pointerDragStarted = false;
        }

        void BeginDrag(CardView card)
        {
            pointerDragStarted = true;
            draggingCard = card;
            hoveredCard = null;
            card.transform.SetAsLastSibling();
            card.SetCardScale(keptScale * hoverScaleMultiplier);
        }

        void UpdateDragPosition(CardView card)
        {
            if (!TryGetPointerWorldPositionOnTable(out var worldPosition))
                return;

            worldPosition.y += dragLift;
            card.transform.position = worldPosition;
            card.transform.rotation = CardView.TableRotation;
        }

        void EndDrag(CardView card)
        {
            var oldIndex = handCards.IndexOf(card);
            if (oldIndex < 0)
            {
                draggingCard = null;
                return;
            }

            var newIndex = GetInsertIndexFromPosition(card.transform.position);
            if (newIndex != oldIndex)
            {
                handCards.RemoveAt(oldIndex);
                if (newIndex > oldIndex)
                    newIndex--;

                newIndex = Mathf.Clamp(newIndex, 0, handCards.Count);
                handCards.Insert(newIndex, card);
            }

            draggingCard = null;
            StartCoroutine(LayoutKeptHandRoutine(dragReorderDuration));
        }

        int GetInsertIndexFromPosition(Vector3 worldPosition)
        {
            if (handCards.Count <= 1)
                return 0;

            for (var i = 0; i < handCards.Count; i++)
            {
                var slot = GetLayoutSlot(i);
                if (worldPosition.x < slot.Position.x)
                    return i;
            }

            return handCards.Count - 1;
        }

        bool TryGetPointerWorldPositionOnTable(out Vector3 worldPosition)
        {
            worldPosition = default;
            var camera = Camera.main;
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(GetMouseScreenPosition());
            var plane = new Plane(Vector3.up, new Vector3(0f, keptHandCenter.y, 0f));
            if (!plane.Raycast(ray, out var distance))
                return false;

            worldPosition = ray.GetPoint(distance);
            return true;
        }

        static Vector3 GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null
                ? (Vector3)Mouse.current.position.ReadValue()
                : Input.mousePosition;
#else
            return Input.mousePosition;
#endif
        }

        public void HandleInspectCard(CardView card)
        {
            if (!CanClickCards || !handCards.Contains(card))
                return;

            storeActions?.ClearStoreInspectSelection();

            if (selectedCard == card)
            {
                selectedCard = null;
                StartCoroutine(LayoutKeptHandRoutine());
                storeView?.RestoreLayoutUnlessInspecting();
                return;
            }

            selectedCard = card;
            hoveredCard = null;
            BringCardToFront(card);
            storeView?.TuckForOverlayInspect();
            StartCoroutine(InspectCardRoutine(card));
        }

        public IEnumerator AdoptCardRoutine(CardView card, float duration)
        {
            card.transform.SetParent(transform);
            handCards.Add(card);

            var clickHandler = card.GetComponent<CardClickHandler>();
            if (clickHandler == null)
                clickHandler = card.gameObject.AddComponent<CardClickHandler>();
            clickHandler.Init(this);

            card.SetClickable(true);
            card.SetFaceUpImmediate(true);

            for (var i = 0; i < handCards.Count; i++)
            {
                var handCard = handCards[i];
                if (handCard == null)
                    continue;

                var slot = GetLayoutSlot(i);
                StartCoroutine(AnimateCardToSlot(handCard, slot, keptScale, duration));
            }

            yield return new WaitForSeconds(duration);
        }

        public IEnumerator DiscardCardRoutine(CardView card, Vector3 outPosition, float duration)
        {
            if (!handCards.Remove(card))
                yield break;

            selectedCard = null;
            hoveredCard = null;

            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(outPosition, keptScale * 0.8f, duration, CardView.TableRotation);
            else
            {
                card.transform.position = outPosition;
                card.SetCardScale(keptScale * 0.8f);
            }

            Destroy(card.gameObject);

            for (var i = 0; i < handCards.Count; i++)
            {
                var handCard = handCards[i];
                if (handCard == null)
                    continue;

                var slot = GetLayoutSlot(i);
                StartCoroutine(AnimateCardToSlot(handCard, slot, keptScale, duration));
            }

            yield return new WaitForSeconds(duration);
        }

        public void HandleCardHoverEnter(CardView card)
        {
            if (!handCards.Contains(card) || selectedCard != null || draggingCard != null)
                return;

            hoveredCard = card;
            card.transform.SetAsLastSibling();
            CardHoverAudio.PlayHoverFlip();
            StartCoroutine(AnimateCardHover(card, true));
        }

        public void HandleCardHoverExit(CardView card)
        {
            if (hoveredCard != card)
                return;

            hoveredCard = null;
            if (selectedCard == card)
                return;

            StartCoroutine(AnimateCardHover(card, false));
        }

        IEnumerator AnimateCardHover(CardView card, bool entering)
        {
            var index = handCards.IndexOf(card);
            if (index < 0)
                yield break;

            var slot = GetLayoutSlot(index);
            var baseScale = GetCurrentBaseScale();

            if (entering)
            {
                var hoverPosition = slot.Position + new Vector3(0f, hoverLift, hoverForward);
                yield return AnimateCardToSlot(card, hoverPosition, baseScale * hoverScaleMultiplier, hoverAnimDuration, slot.Rotation);
            }
            else
            {
                yield return AnimateCardToSlot(card, slot, baseScale, hoverAnimDuration);
            }
        }

        HandSlot GetLayoutSlot(int index)
        {
            if (handKept)
                return GetSpreadSlot(index, handCards.Count, keptHandCenter, keptSpreadSpacing, index * 0.002f);

            return GetSpreadSlot(index, handCards.Count, dealHandCenter, dealSpreadSpacing, index * 0.002f);
        }

        float GetCurrentBaseScale() => handKept ? keptScale : dealScale;

        IEnumerator KeepHandRoutine()
        {
            handKept = true;
            selectedCard = null;
            hoveredCard = null;

            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (card == null)
                    continue;

                card.SetClickable(true);
                var slot = GetSpreadSlot(i, handCards.Count, keptHandCenter, keptSpreadSpacing, i * 0.002f);
                StartCoroutine(AnimateCardToSlot(card, slot, keptScale, keepAnimDuration));
            }

            yield return new WaitForSeconds(keepAnimDuration);
        }

        IEnumerator LayoutKeptHandRoutine(float duration = -1f)
        {
            if (duration < 0f)
                duration = inspectAnimDuration;

            hoveredCard = null;

            var animating = 0;
            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (card == null)
                    continue;

                var slot = GetLayoutSlot(i);
                var scale = GetCurrentBaseScale();
                animating++;
                StartCoroutine(AnimateCardToSlotAndNotify(card, slot, scale, duration, () => animating--));
            }

            selectedCard = null;

            while (animating > 0)
                yield return null;
        }

        IEnumerator InspectCardRoutine(CardView card)
        {
            BringCardToFront(card);

            var animating = 0;
            for (var i = 0; i < handCards.Count; i++)
            {
                var handCard = handCards[i];
                if (handCard == null)
                    continue;

                animating++;
                if (handCard == card)
                {
                    StartCoroutine(AnimateCardToSlotAndNotify(
                        handCard,
                        inspectPosition,
                        inspectScale,
                        inspectAnimDuration,
                        CardView.TableRotation,
                        () => animating--));
                }
                else
                {
                    var slot = GetLayoutSlot(i);
                    var scale = GetCurrentBaseScale();
                    StartCoroutine(AnimateCardToSlotAndNotify(handCard, slot, scale, inspectAnimDuration, () => animating--));
                }
            }

            while (animating > 0)
                yield return null;
        }

        void BringCardToFront(CardView card)
        {
            if (card == null)
                return;

            card.transform.SetAsLastSibling();
        }

        static IEnumerator AnimateCardToSlotAndNotify(
            CardView card,
            Vector3 position,
            float scale,
            float duration,
            Quaternion rotation,
            System.Action onComplete)
        {
            yield return AnimateCardToSlot(card, position, scale, duration, rotation);
            onComplete?.Invoke();
        }

        static IEnumerator AnimateCardToSlotAndNotify(
            CardView card,
            HandSlot slot,
            float scale,
            float duration,
            System.Action onComplete)
        {
            yield return AnimateCardToSlot(card, slot, scale, duration);
            onComplete?.Invoke();
        }

        static IEnumerator AnimateCardToSlot(CardView card, HandSlot slot, float scale, float duration)
        {
            yield return AnimateCardToSlot(card, slot.Position, scale, duration, slot.Rotation);
        }

        static IEnumerator AnimateCardToSlot(CardView card, Vector3 position, float scale, float duration, Quaternion rotation)
        {
            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(position, scale, duration, rotation);
            else
            {
                card.transform.position = position;
                card.transform.rotation = rotation;
                card.SetCardScale(scale);
            }
        }

        CardView SpawnHandCard(Texture2D front, Vector3 deckPosition)
        {
            var card = Instantiate(cardPrefab, deckPosition, CardView.TableRotation, transform);
            card.SetFrontTexture(front);
            card.SetFaceUpImmediate(false);
            card.SetCardScale(dealScale);
            card.SetClickable(false);

            var clickHandler = card.GetComponent<CardClickHandler>();
            if (clickHandler == null)
                clickHandler = card.gameObject.AddComponent<CardClickHandler>();
            clickHandler.Init(this);

            return card;
        }

        static HandSlot GetSpreadSlot(int index, int count, Vector3 center, float spacing, float yLift)
        {
            var startX = center.x - ((count - 1) * spacing * 0.5f);
            var position = new Vector3(startX + (index * spacing), center.y + yLift, center.z);
            return new HandSlot(position, CardView.TableRotation);
        }

        static void ApplySlot(CardView card, HandSlot slot, float scale)
        {
            card.transform.position = slot.Position;
            card.transform.rotation = slot.Rotation;
            card.SetCardScale(scale);
        }

        void ClearHand()
        {
            for (var i = handCards.Count - 1; i >= 0; i--)
            {
                if (handCards[i] != null)
                    Destroy(handCards[i].gameObject);
            }

            handCards.Clear();
        }
    }
}
