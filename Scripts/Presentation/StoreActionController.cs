using System.Collections;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    public class StoreActionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] HandView handView;
        [SerializeField] StoreView storeView;
        [SerializeField] Transform deckPoint;

        [Header("Sell")]
        [SerializeField] ZoneView outOfPlayZone;
        [SerializeField] Vector3 outZonePosition = PlaymatZones.OutOfPlay;
        [SerializeField] float actionAnimDuration = 0.35f;

        StoreActionMode currentMode = StoreActionMode.None;
        CardView tradeHandCard;

        public StoreActionMode CurrentMode => currentMode;

        public void Init(HandView hand, StoreView store, Transform deck, ZoneView outZone = null)
        {
            handView = hand;
            storeView = store;
            deckPoint = deck;
            if (outZone != null)
                outOfPlayZone = outZone;
        }

        public void SetMode(StoreActionMode mode)
        {
            if (currentMode == mode)
                mode = StoreActionMode.None;

            currentMode = mode;
            tradeHandCard = null;
            handView?.ClearInspectSelection();
            storeView?.ClearInspectSelection();
        }

        public void ClearStoreInspectSelection()
        {
            storeView?.ClearInspectSelection();
        }

        public void HandleStoreCardAction(CardView card)
        {
            if (storeView == null || handView == null || !handView.HandKept)
                return;

            switch (currentMode)
            {
                case StoreActionMode.Buy:
                    StartCoroutine(BuyRoutine(card));
                    break;
                case StoreActionMode.Trade when tradeHandCard != null:
                    StartCoroutine(TradeRoutine(tradeHandCard, card));
                    break;
            }
        }

        public void HandleStoreCardClicked(CardView card)
        {
            HandleStoreCardAction(card);
        }

        public void HandleHandCardClicked(CardView card)
        {
            if (handView == null || !handView.HandKept)
                return;

            switch (currentMode)
            {
                case StoreActionMode.Sell:
                    StartCoroutine(SellRoutine(card));
                    break;
                case StoreActionMode.Trade:
                    tradeHandCard = tradeHandCard == card ? null : card;
                    break;
                default:
                    handView.HandleInspectCard(card);
                    break;
            }
        }

        IEnumerator BuyRoutine(CardView storeCard)
        {
            var slotIndex = storeView.IndexOf(storeCard);
            if (slotIndex < 0)
                yield break;

            var deckPosition = GetDeckPosition();
            storeView.DetachCard(storeCard);
            yield return handView.AdoptCardRoutine(storeCard, actionAnimDuration);
            yield return storeView.RefillSlotRoutine(slotIndex, deckPosition);

            currentMode = StoreActionMode.None;
        }

        IEnumerator SellRoutine(CardView handCard)
        {
            var target = outOfPlayZone != null ? outOfPlayZone.transform.position : outZonePosition;
            yield return handView.DiscardCardRoutine(handCard, target, actionAnimDuration);
            outOfPlayZone?.AddToPile(1);
            currentMode = StoreActionMode.None;
        }

        IEnumerator TradeRoutine(CardView handCard, CardView storeCard)
        {
            var handFront = handCard.FrontTexture;
            var storeFront = storeCard.FrontTexture;

            handCard.SetFrontTexture(storeFront);
            storeCard.SetFrontTexture(handFront);

            tradeHandCard = null;
            currentMode = StoreActionMode.None;
            yield return null;
        }

        Vector3 GetDeckPosition()
        {
            return deckPoint != null ? deckPoint.position : PlaymatZones.Supply;
        }
    }
}
