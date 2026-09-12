using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Store (river) — dealt face-down into a horizontal row, then flipped. Can fold away to clear the battle map.
    /// </summary>
    [DisallowMultipleComponent]
    public class StoreView : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        public CardView CardPrefab => cardPrefab;

        [Header("Horizontal Row")]
        [SerializeField] int storeSlotCount = PlaymatZones.StoreSlotCount;
        [SerializeField] Vector3 rowCenter = PlaymatZones.StoreRowCenter;
        [SerializeField] Vector3 listAnchor = new(-13.5f, 0.45f, 7.5f);
        [SerializeField] float rowSpacing = PlaymatZones.StoreSpacing;
        [SerializeField] float storeCardScale = PlaymatZones.StoreCardScale;
        [SerializeField] float cardLift = 0.03f;

        [Header("River Fold")]
        [SerializeField] float riverBundleDuration = 0.28f;
        [SerializeField] float riverVanishDuration = 0.42f;
        [SerializeField] float riverVanishStagger = 0.025f;
        [SerializeField] float riverBundleScaleMultiplier = 0.58f;
        [SerializeField] float riverVanishScaleMultiplier = 0.06f;
        [SerializeField] float riverVanishArcHeight = 0.45f;

        [Header("Animation")]
        [SerializeField] float dealDuration = 0.22f;
        [SerializeField] float dealStagger = 0.04f;
        [SerializeField] float flipDuration = 0.18f;
        [SerializeField] float flipStagger = 0.025f;
        [SerializeField] float layoutAnimDuration = 0.35f;

        [Header("Hover")]
        [SerializeField] float hoverScaleMultiplier = 1.14f;
        [SerializeField] float hoverLift = 0.06f;
        [SerializeField] float hoverForward = -0.18f;
        [SerializeField] float hoverAnimDuration = 0.12f;

        [Header("Inspect")]
        [SerializeField] float inspectScale = PlaymatZones.CardScale;
        [SerializeField] Vector3 inspectPosition = new(0f, 0.1f, 0.5f);
        [SerializeField] float inspectAnimDuration = 0.25f;
        [SerializeField] float inspectPeerScaleMultiplier = 0.72f;
        [SerializeField] float inspectPeerZOffset = 0.65f;

        readonly List<CardView> storeCards = new();
        readonly Dictionary<CardView, Coroutine> hoverAnimations = new();
        StoreActionController storeActions;
        HandView handView;
        CardView selectedCard;
        CardView hoveredCard;
        bool isRiverCollapsed;

        public bool IsDealing { get; private set; }
        public bool RoundStarted { get; private set; }
        public bool IsRiverCollapsed => isRiverCollapsed;
        bool turnStoreEnabled = true;

        void OnEnable() => SyncTableHeights();

        void SyncTableHeights()
        {
            rowCenter.y = PlaymatZones.CardY;
            listAnchor.y = PlaymatZones.CardY;
        }

        public bool CanInteractCards =>
            turnStoreEnabled
            && RoundStarted
            && !IsDealing
            && !isRiverCollapsed
            && handView != null
            && handView.HandKept;

        public void SetTurnStoreEnabled(bool enabled)
        {
            turnStoreEnabled = enabled;
        }
        public bool HasInspectSelection => selectedCard != null;

        public void ClearStoreRuntime()
        {
            HoverTooltipView.Hide();
            isRiverCollapsed = false;
            RoundStarted = false;
            ClearAllHoverAnimations();
            ClearStore();
        }

        public void EnsureRowEmpty()
        {
            if (storeCards.Count == 0)
                return;

            ClearStore();
        }

        public void BindStoreActions(StoreActionController controller)
        {
            storeActions = controller;
        }

        public void BindHandView(HandView hand)
        {
            handView = hand;
        }

        public void RestoreLayoutUnlessInspecting()
        {
            if (IsDealing || selectedCard != null)
                return;

            StartCoroutine(LayoutStoreRoutine());
        }

        public void TuckForOverlayInspect()
        {
            if (IsDealing || storeCards.Count == 0 || selectedCard != null)
                return;

            StartCoroutine(TuckAllCardsRoutine());
        }

        public void ClearInspectSelection()
        {
            if (selectedCard == null)
                return;

            selectedCard = null;
            StartCoroutine(LayoutStoreRoutine());
        }

        public void HandleStoreCardClicked(CardView card)
        {
            if (!CanInteractCards || !storeCards.Contains(card))
                return;

            if (storeActions != null && storeActions.CurrentMode != StoreActionMode.None)
            {
                storeActions.HandleStoreCardAction(card);
                return;
            }

            HandleInspectCard(card);
        }

        public void HandleInspectCard(CardView card)
        {
            if (!CanInteractCards || !storeCards.Contains(card))
                return;

            handView?.ClearInspectSelection();

            if (selectedCard == card)
            {
                selectedCard = null;
                StartCoroutine(LayoutStoreRoutine());
                return;
            }

            selectedCard = card;
            hoveredCard = null;
            BringCardToFront(card);
            StartCoroutine(InspectCardRoutine(card));
        }

        public void HandleCardHoverEnter(CardView card)
        {
            HoverTooltipView.ShowCard(card);

            if (!CanInteractCards || !storeCards.Contains(card) || selectedCard != null)
                return;

            if (handView != null && handView.HasInspectSelection)
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
            HoverTooltipView.Hide();

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
            if (IsDealing || isRiverCollapsed || selectedCard != null)
                return;

            if (hoveredCard != null && !IsPointerOverCard(hoveredCard))
            {
                var staleHover = hoveredCard;
                hoveredCard = null;
                StopCardHoverAnimation(staleHover, snapToRest: true);
            }

            var enlargedThreshold = storeCardScale * 1.05f;
            for (var i = 0; i < storeCards.Count; i++)
            {
                var card = storeCards[i];
                if (card == null || !card.gameObject.activeInHierarchy || card == selectedCard || card == hoveredCard)
                    continue;

                if (card.CardScale > enlargedThreshold)
                    SnapCardToLayoutRest(card);
            }
        }

        void SnapCardToLayoutRest(CardView card)
        {
            if (card == null || selectedCard == card || isRiverCollapsed)
                return;

            var index = storeCards.IndexOf(card);
            if (index < 0)
                return;

            card.GetComponent<CardAnimator>()?.Cancel();
            var slot = GetSlotPosition(index);
            card.transform.position = slot;
            card.transform.rotation = CardView.TableRotation;
            card.SetCardScale(storeCardScale);
        }

        static bool IsPointerOverCard(CardView card)
        {
            var camera = Camera.main;
            if (camera == null || card == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            var screenPosition = UnityEngine.InputSystem.Mouse.current != null
                ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
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

        public int IndexOf(CardView card)
        {
            return storeCards.IndexOf(card);
        }

        public void DetachCard(CardView card)
        {
            var index = storeCards.IndexOf(card);
            if (index >= 0)
                storeCards[index] = null;

            if (selectedCard == card)
                selectedCard = null;
            if (hoveredCard == card)
                hoveredCard = null;
        }

        public IEnumerator PlaceRiverLegendariesRoutine(IReadOnlyList<CardView> legendaries)
        {
            if (legendaries == null || legendaries.Count == 0)
                yield break;

            ClearStore();
            isRiverCollapsed = false;
            IsDealing = true;

            for (var i = 0; i < legendaries.Count && i < storeSlotCount; i++)
            {
                var card = legendaries[i];
                if (card == null)
                    continue;

                while (storeCards.Count <= i)
                    storeCards.Add(null);

                card.transform.SetParent(transform, true);
                card.SetFaceUpImmediate(true);
                card.SetCardScale(storeCardScale);
                card.SetClickable(false);
                storeCards[i] = card;

                var target = GetSlotPosition(i);
                CardHoverAudio.PlayCardMove();

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    yield return animator.AnimateToRoutine(target, storeCardScale, layoutAnimDuration, CardView.TableRotation);
                else
                {
                    card.transform.position = target;
                    card.transform.rotation = CardView.TableRotation;
                }
            }

            IsDealing = false;
        }

        public IEnumerator BeginRoundRoutine(Vector3 deckPosition)
        {
            isRiverCollapsed = false;
            IsDealing = true;

            if (cardPrefab == null)
            {
                IsDealing = false;
                yield break;
            }

            EnsureStoreSlotList();
            var newlyDealt = new List<CardView>();

            for (var i = 0; i < storeSlotCount; i++)
            {
                if (storeCards[i] != null)
                    continue;

                var fronts = CardDeck.Draw(1);
                if (fronts.Count == 0)
                    break;

                var slotPosition = GetSlotPosition(i);
                var card = SpawnStoreCard(fronts[0], deckPosition, slotPosition);
                storeCards[i] = card;
                newlyDealt.Add(card);

                CardHoverAudio.PlayCardMove();

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    yield return animator.DealFaceDownRoutine(deckPosition, slotPosition, dealDuration);
                else
                    card.transform.position = slotPosition;

                if (dealStagger > 0f)
                    yield return new WaitForSeconds(dealStagger);
            }

            for (var i = 0; i < newlyDealt.Count; i++)
            {
                var card = newlyDealt[i];
                if (card == null)
                    continue;

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    StartCoroutine(FlipWithDelay(animator, i * flipStagger));
                else
                    card.SetFaceUpImmediate(true);
            }

            if (newlyDealt.Count > 0)
                yield return new WaitForSeconds(flipDuration + ((newlyDealt.Count - 1) * flipStagger));

            RoundStarted = true;
            EnableStoreClicks();
            IsDealing = false;
        }

        public IEnumerator DealStoreRoutine(Vector3 deckPosition)
        {
            ClearStore();
            isRiverCollapsed = false;
            IsDealing = true;
            RoundStarted = false;

            if (cardPrefab == null)
            {
                IsDealing = false;
                yield break;
            }

            var fronts = CardDeck.Draw(storeSlotCount);
            if (fronts.Count == 0)
            {
                IsDealing = false;
                yield break;
            }

            for (var i = 0; i < fronts.Count; i++)
            {
                var slotPosition = GetSlotPosition(i);
                var card = SpawnStoreCard(fronts[i], deckPosition, slotPosition);
                storeCards.Add(card);

                CardHoverAudio.PlayCardMove();

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    yield return animator.DealFaceDownRoutine(deckPosition, slotPosition, dealDuration);
                else
                    card.transform.position = slotPosition;

                if (dealStagger > 0f && i < fronts.Count - 1)
                    yield return new WaitForSeconds(dealStagger);
            }

            for (var i = 0; i < storeCards.Count; i++)
            {
                var card = storeCards[i];
                if (card == null)
                    continue;

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    StartCoroutine(FlipWithDelay(animator, i * flipStagger));
                else
                    card.SetFaceUpImmediate(true);
            }

            yield return new WaitForSeconds(flipDuration + ((storeCards.Count - 1) * flipStagger));
            RoundStarted = true;
            EnableStoreClicks();
            IsDealing = false;
        }

        void EnsureStoreSlotList()
        {
            while (storeCards.Count < storeSlotCount)
                storeCards.Add(null);
        }

        public IEnumerator AdmitHandCardRoutine(CardView card, int slotIndex, float duration)
        {
            if (card == null || slotIndex < 0)
                yield break;

            while (storeCards.Count <= slotIndex)
                storeCards.Add(null);

            var displaced = storeCards[slotIndex];
            if (displaced != null && displaced != card)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(displaced.gameObject);
                else
#endif
                    Destroy(displaced.gameObject);
            }

            var handClick = card.GetComponent<CardClickHandler>();
            if (handClick != null)
                Destroy(handClick);

            card.transform.SetParent(transform, true);
            card.SetFaceUpImmediate(true);
            card.SetClickable(false);
            storeCards[slotIndex] = card;

            CardHoverAudio.PlayCardMove();

            var target = GetSlotPosition(slotIndex);
            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(target, storeCardScale, duration, CardView.TableRotation);
            else
            {
                card.transform.position = target;
                card.transform.rotation = CardView.TableRotation;
                card.SetCardScale(storeCardScale);
            }
        }

        public void EnableAllStoreClicks()
        {
            EnableStoreClicks();
        }

        public IEnumerator RefillSlotRoutine(int slotIndex, Vector3 deckPosition)
        {
            if (cardPrefab == null || slotIndex < 0)
                yield break;

            while (storeCards.Count <= slotIndex)
                storeCards.Add(null);

            var fronts = CardDeck.Draw(1);
            if (fronts.Count == 0)
                yield break;

            var slotPosition = GetSlotPosition(slotIndex);
            var card = SpawnStoreCard(fronts[0], deckPosition, slotPosition);
            storeCards[slotIndex] = card;

            CardHoverAudio.PlayCardMove();

            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
            {
                yield return animator.DealFaceDownRoutine(deckPosition, slotPosition, dealDuration);
                yield return animator.FlipFaceUpRoutine(flipDuration);
            }
            else
            {
                card.transform.position = slotPosition;
                card.SetFaceUpImmediate(true);
            }

            EnableCardClick(card);
        }

        public void ToggleRiverCollapse(Vector3 deckPosition, RectTransform hideButton = null)
        {
            if (!HasAnyStoreCards() || IsDealing)
                return;

            if (isRiverCollapsed)
                StartCoroutine(ExpandRiverRoutine(deckPosition));
            else
                StartCoroutine(CollapseRiverRoutine(hideButton));
        }

        bool HasAnyStoreCards()
        {
            for (var i = 0; i < storeCards.Count; i++)
            {
                if (storeCards[i] != null)
                    return true;
            }

            return false;
        }

        IEnumerator CollapseRiverRoutine(RectTransform hideButton)
        {
            IsDealing = true;
            selectedCard = null;
            ClearAllHoverAnimations();
            storeActions?.ClearStoreInspectSelection();
            DisableStoreClicks();

            var activeCards = new List<CardView>();
            for (var i = 0; i < storeCards.Count; i++)
            {
                if (storeCards[i] != null)
                    activeCards.Add(storeCards[i]);
            }

            if (activeCards.Count == 0)
            {
                IsDealing = false;
                yield break;
            }

            var animating = 0;
            var bundleScale = storeCardScale * riverBundleScaleMultiplier;
            for (var i = 0; i < activeCards.Count; i++)
            {
                var card = activeCards[i];
                card.transform.SetAsLastSibling();
                animating++;
                var bundlePosition = GetRiverBundlePosition(i, activeCards.Count);
                StartCoroutine(AnimateCardToSlotAndNotify(card, bundlePosition, bundleScale, riverBundleDuration, () => animating--));
            }

            while (animating > 0)
                yield return null;

            var vanishTarget = GetHideButtonTablePosition(hideButton);
            var vanishScale = storeCardScale * riverVanishScaleMultiplier;
            animating = 0;
            for (var i = 0; i < activeCards.Count; i++)
            {
                var card = activeCards[i];
                animating++;
                var delay = i * riverVanishStagger;
                StartCoroutine(AnimateCardVanishAndNotify(
                    card,
                    vanishTarget,
                    vanishScale,
                    riverVanishDuration,
                    riverVanishArcHeight,
                    delay,
                    () => animating--));
            }

            while (animating > 0)
                yield return null;

            for (var i = 0; i < storeCards.Count; i++)
            {
                if (storeCards[i] != null)
                    storeCards[i].gameObject.SetActive(false);
            }

            isRiverCollapsed = true;
            IsDealing = false;
        }

        IEnumerator ExpandRiverRoutine(Vector3 deckPosition)
        {
            IsDealing = true;
            isRiverCollapsed = false;

            for (var i = 0; i < storeCards.Count; i++)
            {
                var card = storeCards[i];
                if (card == null)
                    continue;

                card.gameObject.SetActive(true);
                card.SetClickable(false);
                card.SetCardScale(storeCardScale);
                card.SetFaceUpImmediate(false);
                card.transform.position = deckPosition;
                card.transform.rotation = CardView.TableRotation;

                var slotPosition = GetRowPosition(i, storeSlotCount);
                CardHoverAudio.PlayCardMove();

                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                {
                    animator.Cancel();
                    yield return animator.DealFaceDownRoutine(deckPosition, slotPosition, dealDuration);
                    yield return animator.FlipFaceUpRoutine(flipDuration);
                }
                else
                {
                    card.transform.position = slotPosition;
                    card.SetFaceUpImmediate(true);
                }

                card.SetCardScale(storeCardScale);

                if (dealStagger > 0f)
                    yield return new WaitForSeconds(dealStagger);
            }

            EnableStoreClicks();
            IsDealing = false;
        }

        void DisableStoreClicks()
        {
            foreach (var card in storeCards)
            {
                if (card != null)
                    card.SetClickable(false);
            }
        }

        void EnableStoreClicks()
        {
            foreach (var card in storeCards)
                EnableCardClick(card);
        }

        void EnableCardClick(CardView card)
        {
            if (card == null)
                return;

            card.SetClickable(true);
            var clickHandler = card.GetComponent<StoreCardClickHandler>();
            if (clickHandler == null)
                clickHandler = card.gameObject.AddComponent<StoreCardClickHandler>();
            clickHandler.Init(this);
        }

        CardView SpawnStoreCard(Texture2D front, Vector3 deckPosition, Vector3 slotPosition)
        {
            var card = Instantiate(cardPrefab, deckPosition, CardView.TableRotation, transform);
            card.SetFrontTexture(front);
            card.SetFaceUpImmediate(false);
            card.SetCardScale(storeCardScale);
            return card;
        }

        IEnumerator LayoutStoreRoutine()
        {
            ClearAllHoverAnimations();

            var animating = 0;
            for (var i = 0; i < storeCards.Count; i++)
            {
                var card = storeCards[i];
                if (card == null)
                    continue;

                var slot = GetSlotPosition(i);
                animating++;
                StartCoroutine(AnimateCardToSlotAndNotify(card, slot, storeCardScale, inspectAnimDuration, () => animating--));
            }

            while (animating > 0)
                yield return null;
        }

        IEnumerator TuckAllCardsRoutine()
        {
            var animating = 0;
            for (var i = 0; i < storeCards.Count; i++)
            {
                var card = storeCards[i];
                if (card == null)
                    continue;

                animating++;
                var slot = GetInspectPeerPosition(i);
                var scale = storeCardScale * inspectPeerScaleMultiplier;
                StartCoroutine(AnimateCardToSlotAndNotify(card, slot, scale, inspectAnimDuration, () => animating--));
            }

            while (animating > 0)
                yield return null;
        }

        IEnumerator InspectCardRoutine(CardView card)
        {
            BringCardToFront(card);

            var animating = 0;
            for (var i = 0; i < storeCards.Count; i++)
            {
                var storeCard = storeCards[i];
                if (storeCard == null)
                    continue;

                animating++;
                if (storeCard == card)
                {
                    StartCoroutine(AnimateCardToSlotAndNotify(
                        storeCard,
                        inspectPosition,
                        inspectScale,
                        inspectAnimDuration,
                        () => animating--));
                }
                else
                {
                    var slot = GetInspectPeerPosition(i);
                    var scale = storeCardScale * inspectPeerScaleMultiplier;
                    StartCoroutine(AnimateCardToSlotAndNotify(storeCard, slot, scale, inspectAnimDuration, () => animating--));
                }
            }

            while (animating > 0)
                yield return null;
        }

        Vector3 GetInspectPeerPosition(int index)
        {
            var slot = GetSlotPosition(index);
            return slot + new Vector3(0f, -0.01f, inspectPeerZOffset);
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
            System.Action onComplete)
        {
            yield return AnimateCardToSlot(card, position, scale, duration);
            onComplete?.Invoke();
        }

        IEnumerator AnimateCardHover(CardView card, bool entering)
        {
            var index = storeCards.IndexOf(card);
            if (index < 0)
                yield break;

            var slot = GetSlotPosition(index);
            if (entering)
            {
                var hoverPosition = slot + new Vector3(0f, hoverLift, hoverForward);
                yield return AnimateCardToSlot(card, hoverPosition, storeCardScale * hoverScaleMultiplier, hoverAnimDuration);
            }
            else
            {
                yield return AnimateCardToSlot(card, slot, storeCardScale, hoverAnimDuration);
            }
        }

        static IEnumerator AnimateCardToSlot(CardView card, Vector3 position, float scale, float duration)
        {
            var animator = card.GetComponent<CardAnimator>();
            if (animator != null)
                yield return animator.AnimateToRoutine(position, scale, duration, CardView.TableRotation);
            else
            {
                card.transform.position = position;
                card.transform.rotation = CardView.TableRotation;
                card.SetCardScale(scale);
            }
        }

        Vector3 GetSlotPosition(int index)
        {
            return GetRowPosition(index, storeSlotCount);
        }

        Vector3 GetRowPosition(int index, int count)
        {
            var startX = rowCenter.x - ((count - 1) * rowSpacing * 0.5f);
            return new Vector3(startX + (index * rowSpacing), rowCenter.y + cardLift, rowCenter.z);
        }

        Vector3 GetRiverBundlePosition(int index, int count)
        {
            var center = rowCenter + new Vector3(0f, cardLift + 0.04f, 0f);
            var spread = (count - 1) * 0.0125f;
            return center + new Vector3((index * 0.025f) - spread, index * 0.01f, index * 0.045f);
        }

        Vector3 GetHideButtonTablePosition(RectTransform hideButton)
        {
            var camera = Camera.main;
            if (hideButton == null || camera == null)
                return rowCenter + new Vector3(-6f, cardLift, -5.5f);

            var corners = new Vector3[4];
            hideButton.GetWorldCorners(corners);
            var screenCenter = (corners[0] + corners[2]) * 0.5f;
            var ray = camera.ScreenPointToRay(screenCenter);
            var planeY = rowCenter.y + cardLift;
            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

            return plane.Raycast(ray, out var distance)
                ? ray.GetPoint(distance)
                : rowCenter + new Vector3(-6f, cardLift, -5.5f);
        }

        static IEnumerator AnimateCardVanishAndNotify(
            CardView card,
            Vector3 target,
            float targetScale,
            float duration,
            float arcHeight,
            float delay,
            System.Action onComplete)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            yield return AnimateCardVanishToTarget(card, target, targetScale, duration, arcHeight);
            onComplete?.Invoke();
        }

        static IEnumerator AnimateCardVanishToTarget(
            CardView card,
            Vector3 target,
            float targetScale,
            float duration,
            float arcHeight)
        {
            if (card == null)
                yield break;

            var start = card.transform.position;
            var startScale = card.CardScale;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                var position = Vector3.Lerp(start, target, t);
                position.y += arcHeight * Mathf.Sin(t * Mathf.PI);
                card.transform.position = position;
                card.SetCardScale(Mathf.Lerp(startScale, targetScale, t));
                yield return null;
            }

            card.transform.position = target;
            card.SetCardScale(targetScale);
        }

        IEnumerator FlipWithDelay(CardAnimator animator, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            yield return animator.FlipFaceUpRoutine(flipDuration);
        }

        void ClearStore()
        {
            selectedCard = null;
            hoveredCard = null;

            for (var i = storeCards.Count - 1; i >= 0; i--)
            {
                if (storeCards[i] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(storeCards[i].gameObject);
                    else
#endif
                        Destroy(storeCards[i].gameObject);
                }
            }

            storeCards.Clear();
        }
    }
}
