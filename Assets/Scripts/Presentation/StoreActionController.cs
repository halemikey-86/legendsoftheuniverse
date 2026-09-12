using System.Collections;

using LegendsOfTheUniverse.Presentation.EngineBridge;

using UnityEngine;



namespace LegendsOfTheUniverse.Presentation

{

    [DisallowMultipleComponent]

    public class StoreActionController : MonoBehaviour

    {

        [Header("References")]

        [SerializeField] HandView handView;

        [SerializeField] StoreView storeView;

        [SerializeField] TableMatchBridge matchBridge;



        [Header("Actions")]

        [SerializeField] float actionAnimDuration = 0.35f;



        StoreActionMode currentMode = StoreActionMode.None;

        CardView tradeHandCard;



        public StoreActionMode CurrentMode => currentMode;



        public void Init(HandView hand, StoreView store, TableMatchBridge bridge = null)

        {

            handView = hand;

            storeView = store;

            matchBridge = bridge;

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



            if (matchBridge != null && matchBridge.IsActive)

            {

                if (!matchBridge.TryStoreBuy(slotIndex, out _))

                    yield break;

            }



            storeView.DetachCard(storeCard);

            yield return handView.AdoptCardRoutine(storeCard, actionAnimDuration);

            currentMode = StoreActionMode.None;

        }



        IEnumerator SellRoutine(CardView handCard)

        {
            if (matchBridge == null || !matchBridge.IsActive || handCard.EngineCardInstanceId == null)
                yield break;

            var instanceId = handCard.EngineCardInstanceId.Value;
            if (!handView.DetachHandCard(handCard))
                yield break;

            if (!matchBridge.TryStoreSell(instanceId, out _))
            {
                handView.ReattachHandCard(handCard);
                handView.RelayoutHand();
                currentMode = StoreActionMode.None;
                yield break;
            }

            handView.RelayoutHand();

            var slotIndex = FindStoreSlotFor(instanceId);
            if (slotIndex >= 0)
                yield return storeView.AdmitHandCardRoutine(handCard, slotIndex, actionAnimDuration);
            else
                yield return handView.DestroyCardRoutine(handCard, actionAnimDuration);

            currentMode = StoreActionMode.None;

        }

        int FindStoreSlotFor(int instanceId)
        {
            var store = matchBridge.Runner.Match.Store;
            for (var i = 0; i < store.Length; i++)
            {
                if (store[i] != null && store[i].InstanceId == instanceId)
                    return i;
            }

            return -1;
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
    }

}


