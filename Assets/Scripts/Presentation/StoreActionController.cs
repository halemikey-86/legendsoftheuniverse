using System.Collections;
using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using LegendsOfTheUniverse.Rules;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    public class StoreActionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] HandView handView;
        [SerializeField] StoreView storeView;
        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] TableMatchBridge matchBridge;

        [Header("Actions")]
        [SerializeField] float actionAnimDuration = 0.35f;

        readonly StoreCardActionModal actionModal = new();
        Transform uiRoot;
        Camera tableCamera;

        CardView pendingTradeHandCard;
        CardView pendingTradeStoreCard;
        int localWorth = GameConstants.StartingWorth;

        public bool IsModalVisible => actionModal.IsVisible;
        public StoreActionMode CurrentMode =>
            pendingTradeHandCard != null || pendingTradeStoreCard != null
                ? StoreActionMode.Trade
                : StoreActionMode.None;

        public void Init(
            HandView hand,
            StoreView store,
            TableMatchBridge bridge,
            PlaymatZonesView zones,
            Transform canvasRoot,
            Camera camera)
        {
            handView = hand;
            storeView = store;
            matchBridge = bridge;
            playmatZones = zones;
            uiRoot = canvasRoot;
            tableCamera = camera;
            actionModal.EnsureBuilt(canvasRoot, camera);
            SyncWorthFromEngine();

            var reader = GetComponent<CardSideReaderView>()
                ?? gameObject.AddComponent<CardSideReaderView>();

            reader.SetUiRoot(canvasRoot);
            reader.ConfigureStoreActions(
                card => StartCoroutine(BuyRoutine(card)),
                card => StartCoroutine(SellRoutine(card)),
                BeginTradeFromStore,
                BeginTradeFromHand,
                CanBuyCard,
                CanSellCard,
                CanStoreTradeCard,
                CanHandTradeCard);
        }

        public void TickModal() => actionModal.Tick();

        public void SetMode(StoreActionMode mode)
        {
            if (mode == StoreActionMode.None)
            {
                ClearPendingTrade();
                actionModal.Hide();
            }
        }

        public void ClearStoreInspectSelection()
        {
            actionModal.Hide();
        }

        public void HandleStoreCardClicked(CardView card)
        {
            if (!CanUseStore())
                return;

            if (pendingTradeHandCard != null)
            {
                if (CanTrade(pendingTradeHandCard, card))
                {
                    StartCoroutine(TradeRoutine(pendingTradeHandCard, card));
                    ClearPendingTrade();
                }
                else
                {
                    ClearPendingTrade();
                    OpenStoreCardReader(card);
                }

                return;
            }

            OpenStoreCardReader(card);
        }

        public void HandleHandCardClicked(CardView card)
        {
            if (!CanUseStore())
                return;

            if (pendingTradeStoreCard != null)
            {
                if (CanTrade(card, pendingTradeStoreCard))
                {
                    StartCoroutine(TradeRoutine(card, pendingTradeStoreCard));
                    ClearPendingTrade();
                }
                else
                {
                    ClearPendingTrade();
                    OpenHandCardReader(card);
                }

                return;
            }

            OpenHandCardReader(card);
        }

        public void OpenStoreCardReader(CardView card)
        {
            if (storeView == null || !storeView.ContainsStoreCard(card))
                return;

            if (!CanUseStore())
            {
                CardSideReaderView.Instance?.Show(card, subtitle: "Finish setup to use the shop.");
                return;
            }

            var cost = StoreCardPricing.GetStoreWorth(card, storeView, matchBridge);
            var worth = GetCurrentWorth();
            var subtitle = storeView.CanInteractCards
                ? $"Store cost: {cost} Worth · You have {worth}"
                : GetStoreTransactionBlockReason() ?? $"Store cost: {cost} Worth · You have {worth}";

            CardSideReaderView.Instance?.Show(
                card,
                subtitle,
                showStoreActions: true,
                showHandActions: false);
        }

        public void OpenHandCardReader(CardView card)
        {
            if (handView == null || !handView.ContainsHandCard(card))
                return;

            if (!CanUseStore())
            {
                CardSideReaderView.Instance?.Show(card, subtitle: "Finish setup to use the shop.");
                return;
            }

            var worth = StoreCardPricing.GetStoreWorth(card, storeView, matchBridge);
            var canTrade = HasMatchingStoreCard(worth);
            var subtitle = storeView != null && storeView.CanInteractCards
                ? (canTrade ? $"Trade value: {worth} Worth" : "No equal-value store card")
                : GetStoreTransactionBlockReason() ?? "Sell and trade unlock during your Main phase.";

            CardSideReaderView.Instance?.Show(
                card,
                subtitle,
                showStoreActions: false,
                showHandActions: true);
        }

        void BeginTradeFromHand(CardView handCard)
        {
            pendingTradeHandCard = handCard;
            pendingTradeStoreCard = null;
        }

        public bool TryCompleteStoreTrade(CardView storeCard)
        {
            if (pendingTradeHandCard == null)
                return false;

            if (!CanTrade(pendingTradeHandCard, storeCard))
                return false;

            StartCoroutine(TradeRoutine(pendingTradeHandCard, storeCard));
            ClearPendingTrade();
            return true;
        }

        void BeginTradeFromStore(CardView storeCard)
        {
            pendingTradeStoreCard = storeCard;
            pendingTradeHandCard = null;
        }

        void ClearPendingTrade()
        {
            pendingTradeHandCard = null;
            pendingTradeStoreCard = null;
        }

        bool CanUseStore() => handView != null && handView.HandKept && storeView != null;

        bool CanBuyCard(CardView card)
        {
            if (card == null || storeView == null || !storeView.CanInteractCards)
                return false;

            return GetCurrentWorth() >= StoreCardPricing.GetStoreWorth(card, storeView, matchBridge)
                && HasStoreActionAvailable();
        }

        bool CanSellCard(CardView _) => storeView != null && storeView.CanInteractCards && HasStoreActionAvailable();

        bool CanStoreTradeCard(CardView card) =>
            storeView != null
            && storeView.CanInteractCards
            && HasStoreActionAvailable()
            && HasMatchingHandCard(StoreCardPricing.GetStoreWorth(card, storeView, matchBridge));

        bool CanHandTradeCard(CardView card) =>
            storeView != null
            && storeView.CanInteractCards
            && HasStoreActionAvailable()
            && HasMatchingStoreCard(StoreCardPricing.GetStoreWorth(card, storeView, matchBridge));

        string GetStoreTransactionBlockReason()
        {
            if (storeView == null || !storeView.CanOpenMarketMenu)
                return "Shop opens after you keep your hand and choose an icon.";

            if (storeView.CanInteractCards)
                return null;

            if (matchBridge != null && matchBridge.IsActive)
            {
                if (!matchBridge.IsStoreAllowed)
                    return "Buy, sell, and trade are available during your Main phase (once per turn).";

                var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
                if (player != null && player.StoreActionsThisTurn >= 1)
                    return "You already used your store action this turn.";
            }

            return "Buy, sell, and trade unlock during your Main phase.";
        }

        bool HasStoreActionAvailable()
        {
            if (matchBridge != null && matchBridge.IsActive)
            {
                var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
                return player != null && player.StoreActionsThisTurn < 1 && matchBridge.IsStoreAllowed;
            }

            return true;
        }

        bool HasMatchingHandCard(int storeWorth)
        {
            if (handView == null)
                return false;

            for (var i = 0; i < handView.KeptHandCards.Count; i++)
            {
                var card = handView.KeptHandCards[i];
                if (card == null)
                    continue;

                if (StoreCardPricing.GetStoreWorth(card, storeView, matchBridge) == storeWorth)
                    return true;
            }

            return false;
        }

        bool HasMatchingStoreCard(int handWorth)
        {
            if (storeView == null)
                return false;

            for (var i = 0; i < storeView.StoreCards.Count; i++)
            {
                var card = storeView.StoreCards[i];
                if (card == null)
                    continue;

                if (StoreCardPricing.GetStoreWorth(card, storeView, matchBridge) == handWorth)
                    return true;
            }

            return false;
        }

        bool CanTrade(CardView handCard, CardView storeCard)
        {
            if (handCard == null || storeCard == null)
                return false;

            var handWorth = StoreCardPricing.GetStoreWorth(handCard, storeView, matchBridge);
            var storeWorth = StoreCardPricing.GetStoreWorth(storeCard, storeView, matchBridge);
            return handWorth == storeWorth;
        }

        IEnumerator BuyRoutine(CardView storeCard)
        {
            if (storeView == null || handView == null || !CanBuyCard(storeCard))
                yield break;

            var slotIndex = storeView.IndexOf(storeCard);
            if (slotIndex < 0)
                yield break;

            var cost = StoreCardPricing.GetStoreWorth(storeCard, storeView, matchBridge);
            if (GetCurrentWorth() < cost)
                yield break;

            if (matchBridge != null && matchBridge.IsActive)
            {
                if (!matchBridge.TryStoreBuy(slotIndex, out _))
                    yield break;

                matchBridge.TryResolveStoreStack(out _);
                SyncWorthFromEngine();
            }
            else if (!TrySpendLocalWorth(cost))
            {
                yield break;
            }

            storeView.DetachCard(storeCard);
            yield return handView.AdoptCardRoutine(storeCard, actionAnimDuration);
            yield return storeView.RefillSlotRoutine(slotIndex, GetSupplyPosition());
        }

        IEnumerator SellRoutine(CardView handCard)
        {
            yield return handView.DestroyCardRoutine(handCard, actionAnimDuration);
        }

        IEnumerator TradeRoutine(CardView handCard, CardView storeCard)
        {
            if (!CanTrade(handCard, storeCard) || !HasStoreActionAvailable())
                yield break;

            var handFront = handCard.FrontTexture;
            var storeFront = storeCard.FrontTexture;

            handCard.SetFrontTexture(storeFront);
            storeCard.SetFrontTexture(handFront);
            yield return null;
        }

        int GetCurrentWorth()
        {
            if (matchBridge != null && matchBridge.IsActive)
            {
                var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
                if (player != null)
                    return player.Worth;
            }

            if (playmatZones != null)
                return playmatZones.Worth != null ? playmatZones.Worth.Value : localWorth;

            return localWorth;
        }

        void SyncWorthFromEngine()
        {
            if (matchBridge == null || !matchBridge.IsActive || playmatZones == null)
                return;

            var player = matchBridge.Runner.Match.GetPlayer(matchBridge.LocalPlayerId);
            if (player != null)
                playmatZones.SetWorth(player.Worth);
        }

        bool TrySpendLocalWorth(int cost)
        {
            if (cost <= 0)
                return true;

            var worth = GetCurrentWorth();
            if (worth < cost)
                return false;

            localWorth = worth - cost;
            playmatZones?.SetWorth(localWorth);
            return true;
        }

        Vector3 GetSupplyPosition() => PlaymatZones.Supply;
    }
}
