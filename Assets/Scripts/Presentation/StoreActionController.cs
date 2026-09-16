using System;
using System.Collections;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using Zone = Willbound.Engine.Zone;

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
        bool storeActionBusy;

        public StoreActionMode CurrentMode => currentMode;
        public event Action ModeChanged;

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

            SetCurrentMode(mode);
            tradeHandCard = null;
            handView?.ClearInspectSelection();
            storeView?.ClearInspectSelection();
            handView?.RefreshPlayableOutlines();
        }

        void SetCurrentMode(StoreActionMode mode)
        {
            if (currentMode == mode)
                return;

            currentMode = mode;
            ModeChanged?.Invoke();
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
                    BeginSell(card);
                    break;
                case StoreActionMode.Trade:
                    tradeHandCard = tradeHandCard == card ? null : card;
                    break;
                default:
                    handView.HandleInspectCard(card);
                    break;
            }
        }

        public void BeginSell(CardView card)
        {
            if (storeActionBusy || card == null || handView == null || !handView.HandKept)
                return;

            SetCurrentMode(StoreActionMode.Sell);
            StartCoroutine(SellRoutine(card));
        }

        IEnumerator BuyRoutine(CardView storeCard)
        {
            var slotIndex = storeView.IndexOf(storeCard);
            if (slotIndex < 0)
                yield break;

            int? boughtInstanceId = storeCard.EngineCardInstanceId;
            storeView.DetachCard(storeCard);

            if (matchBridge != null && matchBridge.IsActive)
            {
                if (!boughtInstanceId.HasValue)
                {
                    var engineStore = matchBridge.Runner.Match.Store;
                    if (slotIndex >= 0 && slotIndex < engineStore.Length)
                        boughtInstanceId = engineStore[slotIndex]?.InstanceId;
                }

                if (!matchBridge.TryStoreBuy(slotIndex, out _))
                {
                    yield return storeView.AdmitHandCardRoutine(storeCard, slotIndex, actionAnimDuration);
                    SetCurrentMode(StoreActionMode.None);
                    yield break;
                }
            }

            if (boughtInstanceId.HasValue)
                storeCard.SetEngineCardInstanceId(boughtInstanceId);
            yield return handView.AdoptCardRoutine(storeCard, actionAnimDuration);

            SetCurrentMode(StoreActionMode.None);
            handView?.RefreshPlayableOutlines();
        }

        IEnumerator SellRoutine(CardView handCard)
        {
            storeActionBusy = true;
            try
            {
                if (matchBridge == null || !matchBridge.IsActive)
                {
                    matchBridge?.NotifyError("Cannot sell right now.");
                    yield break;
                }

                if (handCard == null || handCard.EngineCardInstanceId == null)
                {
                    matchBridge.NotifyError("That card can't be sold.");
                    yield break;
                }

                var instanceId = handCard.EngineCardInstanceId.Value;

                if (!matchBridge.TryStoreSell(instanceId, out _))
                    yield break;

                var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
                if (player != null)
                {
                    matchBridge.RefreshHud();
                    PlaymatZonesView.Instance?.SetWorth(player.Worth);
                }

                var engineCard = matchBridge.Runner.Match.GetCard(instanceId);
                if (engineCard == null || engineCard.Zone == Zone.Hand)
                {
                    matchBridge.NotifyError("Sell is waiting on the stack. Pass to resolve it.");
                    yield break;
                }

                if (handView.ContainsHandCard(handCard))
                    handView.DetachHandCard(handCard);

                handView.RelayoutHand();

                if (!handView.TryTakeLeavingCard(instanceId, out var sold) || sold == null)
                    sold = handCard;

                var slotIndex = FindStoreSlotFor(instanceId);
                if (slotIndex < 0)
                    slotIndex = PlaymatZones.StoreSlotCount - 1;

                if (sold != null && storeView != null)
                    yield return storeView.AdmitHandCardRoutine(sold, slotIndex, actionAnimDuration);

                if (player != null)
                {
                    matchBridge.RefreshHud();
                    PlaymatZonesView.Instance?.SetWorth(player.Worth);
                }

                SetCurrentMode(StoreActionMode.None);
                handView.RefreshPlayableOutlines();
            }
            finally
            {
                storeActionBusy = false;
            }
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
            var slotIndex = storeView.IndexOf(storeCard);
            if (slotIndex < 0 || handCard.EngineCardInstanceId == null)
                yield break;

            if (matchBridge != null && matchBridge.IsActive
                && !matchBridge.TryStoreTrade(slotIndex, handCard.EngineCardInstanceId.Value, out _))
            {
                SetCurrentMode(StoreActionMode.None);
                tradeHandCard = null;
                yield break;
            }

            if (!handView.DetachHandCard(handCard))
                yield break;

            storeView.DetachCard(storeCard);
            yield return storeView.AdmitHandCardRoutine(handCard, slotIndex, actionAnimDuration);
            yield return handView.AdoptCardRoutine(storeCard, actionAnimDuration);

            tradeHandCard = null;
            SetCurrentMode(StoreActionMode.None);
            handView?.RefreshPlayableOutlines();
        }
    }
}
