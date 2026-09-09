using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Store — dealt face-down into a horizontal row, then flipped. List View stacks on the right.
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
        [SerializeField] float rowSpacing = PlaymatZones.StoreSpacing;
        [SerializeField] float storeCardScale = PlaymatZones.StoreCardScale;
        [SerializeField] float cardLift = 0.03f;

        [Header("List Stack (right side)")]
        [SerializeField] Vector3 listAnchor = PlaymatZones.ListViewAnchor;
        [SerializeField] float listVerticalSpacing = 3.1f;

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
        StoreActionController storeActions;
        HandView handView;
        CardView selectedCard;
        CardView hoveredCard;
        bool isListView;

        public bool IsDealing { get; private set; }
        public bool IsListView => isListView;
        public bool CanInteractCards => !IsDealing && handView != null && handView.HandKept;
        public bool HasInspectSelection => selectedCard != null;

        public void ClearStoreRuntime()
        {
            isListView = false;
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
            if (!CanInteractCards || !storeCards.Contains(card) || selectedCard != null)
                return;

            if (handView != null && handView.HasInspectSelection)
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

        public IEnumerator DealStoreRoutine(Vector3 deckPosition)
        {
            ClearStore();
            isListView = false;
            IsDealing = true;

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
            EnableStoreClicks();
            IsDealing = false;
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

        public void ToggleListView()
        {
            if (storeCards.Count == 0 || IsDealing)
                return;

            selectedCard = null;
            hoveredCard = null;
            isListView = !isListView;
            StartCoroutine(AnimateLayoutChange());
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

        IEnumerator AnimateLayoutChange()
        {
            for (var i = 0; i < storeCards.Count; i++)
            {
                var card = storeCards[i];
                if (card == null)
                    continue;

                var target = GetSlotPosition(i);
                var animator = card.GetComponent<CardAnimator>();
                if (animator != null)
                    StartCoroutine(animator.AnimateToRoutine(target, storeCardScale, layoutAnimDuration, CardView.TableRotation));
                else
                {
                    card.transform.position = target;
                    card.transform.rotation = CardView.TableRotation;
                    card.SetCardScale(storeCardScale);
                }
            }

            yield return new WaitForSeconds(layoutAnimDuration);
        }

        IEnumerator LayoutStoreRoutine()
        {
            hoveredCard = null;

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
            return isListView
                ? GetListPosition(index, storeSlotCount)
                : GetRowPosition(index, storeSlotCount);
        }

        Vector3 GetRowPosition(int index, int count)
        {
            var startX = rowCenter.x - ((count - 1) * rowSpacing * 0.5f);
            return new Vector3(startX + (index * rowSpacing), rowCenter.y + cardLift, rowCenter.z);
        }

        Vector3 GetListPosition(int index, int count)
        {
            var startZ = listAnchor.z - ((count - 1) * listVerticalSpacing * 0.5f);
            return new Vector3(listAnchor.x, listAnchor.y + cardLift + (index * 0.002f), startZ + (index * listVerticalSpacing));
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
