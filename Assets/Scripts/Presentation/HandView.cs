using System.Collections;
using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using Willbound.Engine;
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

        public CardView CardPrefab => cardPrefab;

        void OnEnable() => SyncTableHeights();

        void SyncTableHeights()
        {
            dealHandCenter.y = PlaymatZones.HandY;
            keptHandCenter.y = PlaymatZones.HandY;
        }

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

        [Header("Drag To Play")]
        [SerializeField] TableMatchBridge matchBridge;
        [SerializeField] float playDropZThreshold = -2f;

        readonly List<CardView> handCards = new();
        readonly Dictionary<CardView, Coroutine> hoverAnimations = new();
        CardView selectedCard;
        CardView hoveredCard;
        CardView draggingCard;
        CardView pointerCard;
        Vector3 pointerDownScreen;
        bool pointerDragStarted;
        bool handKept;
        bool handTucked;
        Coroutine handLayoutRoutine;
        StoreActionController storeActions;
        StoreView storeView;
        bool discardMode;
        int discardTargetHandSize;
        System.Action discardCompleteCallback;

        public bool HandKept => handKept;
        public int HandCount => handCards.Count;
        public bool IsDiscardMode => discardMode;
        public bool IsOpeningReview => !handKept && !IsDealing && handCards.Count > 0;
        public bool IsDealing { get; private set; }
        public IReadOnlyList<CardView> OpeningHandCards => handCards;

        public int GetOpeningHandIndex(CardView card) => handCards.IndexOf(card);

        public CardView GetOpeningHandCardAt(int index) =>
            index >= 0 && index < handCards.Count ? handCards[index] : null;
        public bool ContainsHandCard(CardView card) => card != null && handCards.Contains(card);

        /// <summary>Pulls a card out of hand bookkeeping without destroying it (e.g. for a pending Sell).
        /// Pair with <see cref="ReattachHandCard"/> to put it back if the action turns out illegal.</summary>
        public bool DetachHandCard(CardView card) => card != null && handCards.Remove(card);

        public void ReattachHandCard(CardView card)
        {
            if (card != null && !handCards.Contains(card))
                handCards.Add(card);
        }

        public void RelayoutHand() => StartCoroutine(LayoutKeptHandRoutine());

        public bool IsDragging => draggingCard != null;
        public bool CanClickCards => handKept && !IsDealing;
        public bool CanReorderCards =>
            handKept
            && !IsDealing
            && !discardMode
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

        public void BindMatchBridge(TableMatchBridge bridge)
        {
            matchBridge = bridge;
        }

        /// <summary>Replaces the current hand display with one that mirrors the engine's real starting hand,
        /// so each visible card carries a real CardInstanceId and can be dragged onto the field to play it.</summary>
        public void SyncHandFromEngine(IReadOnlyList<CardInstance> engineHand)
        {
            ClearHand();
            handKept = true;
            handTucked = false;
            selectedCard = null;

            if (cardPrefab == null || engineHand == null)
                return;

            for (var i = 0; i < engineHand.Count; i++)
            {
                var instance = engineHand[i];
                if (instance == null)
                    continue;

                var slot = GetSpreadSlot(i, engineHand.Count, keptHandCenter, keptSpreadSpacing, i * 0.002f);
                var card = Instantiate(cardPrefab, slot.Position, slot.Rotation, transform);
                card.SetFrontTexture(EngineCatalog.GetCardArt(instance.Printing));
                card.SetFaceUpImmediate(true);
                card.SetCardScale(keptScale);
                card.SetClickable(true);
                card.SetEngineCardInstanceId(instance.InstanceId);

                var clickHandler = card.GetComponent<CardClickHandler>();
                if (clickHandler == null)
                    clickHandler = card.gameObject.AddComponent<CardClickHandler>();
                clickHandler.Init(this);

                handCards.Add(card);
            }
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
            handTucked = false;
            selectedCard = null;
            hoveredCard = null;
            draggingCard = null;
            pointerCard = null;
            pointerDragStarted = false;
            discardMode = false;
            discardCompleteCallback = null;
            IsDealing = false;
            HideFieldSlotHighlight();
            ClearHand();
        }

        public IEnumerator DealOpeningHandRoutine(Vector3 deckPosition)
        {
            IsDealing = true;
            handKept = false;
            handTucked = false;
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

        /// <summary>Discards the whole opening hand back to supply and deals a fresh one (a full mulligan,
        /// as opposed to <see cref="RedrawOpeningCardRoutine"/>'s one-card-at-a-time redraw).</summary>
        public IEnumerator MulliganOpeningHandRoutine(Vector3 deckPosition)
        {
            if (handKept || IsDealing)
                yield break;

            var fronts = new List<Texture2D>(handCards.Count);
            for (var i = 0; i < handCards.Count; i++)
            {
                if (handCards[i]?.FrontTexture != null)
                    fronts.Add(handCards[i].FrontTexture);
            }

            if (fronts.Count > 0)
                CardDeck.ReturnManyToSupplyAndShuffle(fronts);

            yield return DealOpeningHandRoutine(deckPosition);
        }

        public IEnumerator RedrawOpeningCardRoutine(
            CardView card,
            int slotIndex,
            Vector3 deckPosition)
        {
            if (!IsOpeningReview || card == null)
                yield break;

            var index = handCards.IndexOf(card);
            if (index < 0 || index != slotIndex)
                yield break;

            var fronts = CardDeck.Draw(1);
            if (fronts.Count == 0)
            {
                Debug.LogWarning("HandView: No cards left to redraw.");
                yield break;
            }

            CardDeck.ReturnToSupply(card.FrontTexture);
            Destroy(card.gameObject);
            handCards[slotIndex] = null;

            var spacing = dealSpreadSpacing > 0f
                ? dealSpreadSpacing
                : CardLayout.SpreadSpacing(dealScale);
            var handSlot = GetSpreadSlot(slotIndex, handCards.Count, dealHandCenter, spacing, slotIndex * 0.002f);

            var replacement = SpawnHandCard(fronts[0], deckPosition);
            handCards[slotIndex] = replacement;

            CardHoverAudio.PlayCardMove();

            var animator = replacement.GetComponent<CardAnimator>();
            if (animator != null)
            {
                yield return animator.DealFaceDownRoutine(deckPosition, handSlot.Position, dealDuration);
                yield return animator.FlipFaceUpRoutine(flipDuration);
            }
            else
            {
                replacement.SetFaceUpImmediate(true);
                ApplySlot(replacement, handSlot, dealScale);
            }

            replacement.SetClickable(true);
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

        public void BeginDiscardUntilHandSize(int targetHandSize, System.Action onComplete)
        {
            discardMode = true;
            discardTargetHandSize = Mathf.Max(0, targetHandSize);
            discardCompleteCallback = onComplete;
            ClearInspectSelection();
            storeActions?.SetMode(StoreActionMode.None);
        }

        public void HandleTableClick(CardView card)
        {
            if (!handCards.Contains(card))
                return;

            if (IsOpeningReview || CanClickCards)
            {
                HandleCardPointerDown(card);
                HandleCardPointerUp(card);
            }
        }

        public void HandleCardClicked(CardView card)
        {
            if (!handCards.Contains(card))
                return;

            if (discardMode)
            {
                if (handCards.Count <= discardTargetHandSize)
                    return;

                StartCoroutine(DiscardFromHandRoutine(card));
                return;
            }

            if (!CanClickCards && !IsOpeningReview)
                return;

            if (handKept && storeActions != null && storeActions.CurrentMode != StoreActionMode.None)
            {
                storeActions.HandleHandCardClicked(card);
                return;
            }

            HandleInspectCard(card);
        }

        IEnumerator DiscardFromHandRoutine(CardView card)
        {
            if (card == null || !handCards.Contains(card))
                yield break;

            var front = card.FrontTexture;
            yield return DestroyCardRoutine(card, 0.25f);

            if (front != null)
                CardDeck.ReturnToSupply(front);

            if (handCards.Count <= discardTargetHandSize)
            {
                discardMode = false;
                var callback = discardCompleteCallback;
                discardCompleteCallback = null;
                callback?.Invoke();
            }
        }

        public void HandleCardPointerDown(CardView card)
        {
            if ((!CanClickCards && !IsOpeningReview) || !handCards.Contains(card))
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

            UpdatePlayDropHighlight(card, worldPosition);
        }

        void EndDrag(CardView card)
        {
            if (TryHandlePlayDrop(card))
                return;

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

        /// <summary>If a real engine card was dragged forward past the hand row (toward the field), attempt to
        /// play it. Returns true once the drop is handled — either the card left the hand, or an illegal play
        /// was rejected and the card returns to its slot.</summary>
        bool TryHandlePlayDrop(CardView card)
        {
            HideFieldSlotHighlight();

            if (matchBridge == null || card.EngineCardInstanceId == null)
                return false;

            if (card.transform.position.z < playDropZThreshold)
                return false;

            draggingCard = null;

            if (matchBridge.TryPlayCard(card.EngineCardInstanceId.Value, out _))
            {
                handCards.Remove(card);
                StartCoroutine(AnimateCardPlayedAwayRoutine(card));
            }

            StartCoroutine(LayoutKeptHandRoutine(dragReorderDuration));
            return true;
        }

        static readonly Color FieldHighlightLegalColor = new(0.35f, 0.9f, 0.5f, 0.55f);
        static readonly Color FieldHighlightFullColor = new(0.9f, 0.35f, 0.35f, 0.5f);
        Transform fieldSlotHighlight;

        void UpdatePlayDropHighlight(CardView card, Vector3 worldPosition)
        {
            if (matchBridge == null || card.EngineCardInstanceId == null || !matchBridge.IsActive)
            {
                HideFieldSlotHighlight();
                return;
            }

            if (worldPosition.z < playDropZThreshold)
            {
                HideFieldSlotHighlight();
                return;
            }

            var canPlay = matchBridge.CanPlayCard(card.EngineCardInstanceId.Value);
            var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
            if (IsWillSite(card))
            {
                var wellCount = player.Willwell.Count;
                ShowFieldSlotHighlight(PlaymatZones.GetWillwellSlot(wellCount), canPlay && wellCount < PlaymatZones.WillwellSlotCount);
                return;
            }

            var fieldCount = player.Field.Count;
            if (fieldCount >= PlaymatZones.FieldSlotCount)
                ShowFieldSlotHighlight(PlaymatZones.GetFieldSlot(PlaymatZones.FieldSlotCount - 1), false);
            else
                ShowFieldSlotHighlight(PlaymatZones.GetFieldSlot(fieldCount), canPlay);
        }

        static bool IsWillSite(CardView card)
        {
            if (card.BoundPrinting != null)
                return card.BoundPrinting.Type == CardType.WillSite;
            return false;
        }

        void ShowFieldSlotHighlight(Vector3 worldPosition, bool legal)
        {
            EnsureFieldSlotHighlight();
            fieldSlotHighlight.position = worldPosition + new Vector3(0f, 0.02f, 0f);
            fieldSlotHighlight.gameObject.SetActive(true);

            var renderer = fieldSlotHighlight.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial.color = legal ? FieldHighlightLegalColor : FieldHighlightFullColor;
        }

        void HideFieldSlotHighlight()
        {
            if (fieldSlotHighlight != null)
                fieldSlotHighlight.gameObject.SetActive(false);
        }

        void EnsureFieldSlotHighlight()
        {
            if (fieldSlotHighlight != null)
                return;

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = "FieldSlotHighlight";

            var collider = markerObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            markerObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            markerObject.transform.localScale = new Vector3(
                CardLayout.Width(PlaymatZones.CardScale) * 1.05f,
                CardLayout.Depth(PlaymatZones.CardScale) * 1.05f,
                1f);

            var renderer = markerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                material.color = FieldHighlightLegalColor;
                renderer.sharedMaterial = material;
            }

            markerObject.SetActive(false);
            fieldSlotHighlight = markerObject.transform;
        }

        static IEnumerator AnimateCardPlayedAwayRoutine(CardView card)
        {
            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(card.transform.position, card.CardScale * 0.05f, 0.2f, card.transform.rotation);

            Destroy(card.gameObject);
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
            if ((!CanClickCards && !IsOpeningReview) || !handCards.Contains(card))
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

        public IEnumerator DestroyCardRoutine(CardView card, float duration)
        {
            if (!handCards.Remove(card))
                yield break;

            selectedCard = null;
            hoveredCard = null;

            var vanishScale = keptScale * 0.06f;
            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(card.transform.position, vanishScale, duration, CardView.TableRotation);
            else
                card.SetCardScale(vanishScale);

            Destroy(card.gameObject);

            var animating = 0;
            for (var i = 0; i < handCards.Count; i++)
            {
                var handCard = handCards[i];
                if (handCard == null)
                    continue;

                var slot = GetLayoutSlot(i);
                animating++;
                StartCoroutine(AnimateCardToSlotAndNotify(handCard, slot, keptScale, duration, () => animating--));
            }

            while (animating > 0)
                yield return null;
        }

        public void HandleCardHoverEnter(CardView card)
        {
            if (!handCards.Contains(card) || selectedCard != null || draggingCard != null)
                return;

            if (hoveredCard != null && hoveredCard != card)
                RestoreCardHover(hoveredCard);

            hoveredCard = card;
            card.transform.SetAsLastSibling();
            CardHoverAudio.PlayHoverFlip();
            StartCardHoverAnimation(card, true);
        }

        public void HandleCardHoverExit(CardView card)
        {
            if (selectedCard == card)
                return;

            if (hoveredCard == card)
                hoveredCard = null;

            RestoreCardHover(card);
        }

        void StartCardHoverAnimation(CardView card, bool entering)
        {
            StopCardHoverAnimation(card);
            hoverAnimations[card] = StartCoroutine(RunCardHoverAnimation(card, entering));
        }

        void StopCardHoverAnimation(CardView card, bool snapToRest = false)
        {
            if (card == null || !hoverAnimations.TryGetValue(card, out var routine))
            {
                if (snapToRest)
                    SnapCardToLayoutRest(card);
                return;
            }

            if (routine != null)
                StopCoroutine(routine);

            hoverAnimations.Remove(card);

            if (snapToRest)
                SnapCardToLayoutRest(card);
        }

        void RestoreCardHover(CardView card)
        {
            if (card == null || selectedCard == card)
                return;

            StartCardHoverAnimation(card, false);
        }

        void ClearAllHoverAnimations()
        {
            hoveredCard = null;

            var cards = new List<CardView>(hoverAnimations.Keys);
            for (var i = 0; i < cards.Count; i++)
                StopCardHoverAnimation(cards[i], snapToRest: true);
        }

        void LateUpdate()
        {
            ValidateHoverState();
        }

        void ValidateHoverState()
        {
            if (IsDealing || selectedCard != null || draggingCard != null)
                return;

            if (hoveredCard != null && !IsPointerOverCard(hoveredCard))
            {
                var staleHover = hoveredCard;
                hoveredCard = null;
                StopCardHoverAnimation(staleHover, snapToRest: true);
            }

            var baseScale = GetCurrentBaseScale();
            var enlargedThreshold = baseScale * 1.05f;
            var shrunkThreshold = handKept && !handTucked ? keptScale * 0.92f : 0f;
            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (card == null || card == selectedCard || card == hoveredCard)
                    continue;

                if (card.CardScale > enlargedThreshold)
                    SnapCardToLayoutRest(card);
                else if (shrunkThreshold > 0f && card.CardScale < shrunkThreshold)
                    SnapCardToLayoutRest(card);
            }
        }

        void SnapCardToLayoutRest(CardView card)
        {
            if (card == null || selectedCard == card)
                return;

            var index = handCards.IndexOf(card);
            if (index < 0)
                return;

            card.GetComponent<CardAnimator>()?.Cancel();
            var slot = GetLayoutSlot(index);
            card.transform.position = slot.Position;
            card.transform.rotation = slot.Rotation;
            card.SetCardScale(GetCurrentBaseScale());
        }

        static bool IsPointerOverCard(CardView card)
        {
            var camera = Camera.main;
            if (camera == null || card == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            var screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)Input.mousePosition;
#else
            var screenPosition = (Vector2)Input.mousePosition;
#endif

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 100f))
                return false;

            return hit.collider.GetComponentInParent<CardView>() == card;
        }

        IEnumerator RunCardHoverAnimation(CardView card, bool entering)
        {
            yield return AnimateCardHover(card, entering);
            hoverAnimations.Remove(card);
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

        float GetCurrentBaseScale()
        {
            if (!handKept)
                return dealScale;

            return handTucked ? keptScale * 0.72f : keptScale;
        }

        public void TuckForOverlayPick()
        {
            if (!handKept || selectedCard != null)
                return;

            StartExclusiveHandLayout(TuckAllHandRoutine(true));
        }

        public IEnumerator RestoreHandUnlessInspecting()
        {
            if (selectedCard != null)
                yield break;

            if (handLayoutRoutine != null)
            {
                StopCoroutine(handLayoutRoutine);
                handLayoutRoutine = null;
            }

            yield return TuckAllHandRoutine(false);
        }

        public void EnsureHandAtRestLayout()
        {
            if (!handKept)
                return;

            handTucked = false;
            ClearAllHoverAnimations();

            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (card == null)
                    continue;

                card.GetComponent<CardAnimator>()?.Cancel();
                var slot = GetLayoutSlot(i);
                card.transform.position = slot.Position;
                card.transform.rotation = slot.Rotation;
                card.SetCardScale(keptScale);
            }
        }

        void StartExclusiveHandLayout(IEnumerator routine)
        {
            if (handLayoutRoutine != null)
                StopCoroutine(handLayoutRoutine);

            handLayoutRoutine = StartCoroutine(RunExclusiveHandLayout(routine));
        }

        IEnumerator RunExclusiveHandLayout(IEnumerator routine)
        {
            yield return routine;
            handLayoutRoutine = null;
        }

        IEnumerator TuckAllHandRoutine(bool tuck)
        {
            handTucked = tuck;
            ClearAllHoverAnimations();
            var scale = tuck ? keptScale * 0.72f : keptScale;
            var zOffset = tuck ? -0.85f : 0f;
            var animating = 0;

            for (var i = 0; i < handCards.Count; i++)
            {
                var card = handCards[i];
                if (card == null)
                    continue;

                var slot = GetSpreadSlot(i, handCards.Count, keptHandCenter, keptSpreadSpacing, i * 0.002f);
                var target = slot.Position + new Vector3(0f, -0.01f, zOffset);
                animating++;
                StartCoroutine(AnimateCardToSlotAndNotify(card, target, slot.Rotation, scale, inspectAnimDuration, () => animating--));
            }

            while (animating > 0)
                yield return null;

            if (!tuck)
                EnsureHandAtRestLayout();
        }

        static IEnumerator AnimateCardToSlotAndNotify(
            CardView card,
            Vector3 position,
            Quaternion rotation,
            float scale,
            float duration,
            System.Action onComplete)
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

            onComplete?.Invoke();
        }

        IEnumerator KeepHandRoutine()
        {
            handKept = true;
            handTucked = false;
            selectedCard = null;
            ClearAllHoverAnimations();

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

            ClearAllHoverAnimations();

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
