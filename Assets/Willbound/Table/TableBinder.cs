using System;
using System.Collections.Generic;
using UnityEngine;
using Willbound.Engine;

namespace Willbound.Table
{
    /// <summary>
    /// Sole type that reads match JSON. Offline: builds from Engine B Match.
    /// Everyone else reads bound TableSnapshot + view events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableBinder : MonoBehaviour
    {
        [SerializeField] OfflineTableActionHost offlineActionHost;
        ITableActionHost actionHost;

        TableSnapshot snapshot = new();
        readonly Dictionary<int, CardView> boundCards = new();

        public TableSnapshot Snapshot => snapshot;
        public event Action<TableSnapshot> SnapshotChanged;
        public event Action<GameEvent> EventReceived;

        void Awake()
        {
            if (actionHost == null)
            {
                if (offlineActionHost == null)
                    offlineActionHost = GetComponent<OfflineTableActionHost>();
                actionHost = offlineActionHost;
            }
        }

        public void BindHost(ITableActionHost host)
        {
            actionHost = host;
            RefreshSnapshot();
        }

        /// <summary>Engine C entry — only JSON ingress point.</summary>
        public void ApplySnapshotJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            Debug.LogWarning("TableBinder: JSON snapshot parsing not wired yet (Engine C).");
        }

        public void RefreshSnapshot()
        {
            if (actionHost == null || !actionHost.IsActive)
                return;

            snapshot = actionHost.GetSnapshot();
            SnapshotChanged?.Invoke(snapshot);
        }

        public void OnEngineEvent(GameEvent e)
        {
            if (e == null)
                return;

            EventReceived?.Invoke(e);
            RefreshSnapshot();
        }

        public void RegisterCardView(int instanceId, CardView view)
        {
            if (instanceId <= 0 || view == null)
                return;

            boundCards[instanceId] = view;
            view.BindInstance(instanceId);
        }

        public bool TryGetCardView(int instanceId, out CardView view) =>
            boundCards.TryGetValue(instanceId, out view);

        public bool CanPickup(int instanceId)
        {
            if (actionHost == null || !actionHost.IsActive || instanceId <= 0)
                return false;

            var snap = snapshot;
            if (snap.PriorityPlayerId != snap.LocalPlayerId)
                return false;

            for (var i = 0; i < snap.DeclareQueue.Count; i++)
            {
                if (snap.DeclareQueue[i] == instanceId)
                    return snap.ClashPhase == ClashPhase.C1_ActiveDeclare;
            }

            for (var i = 0; i < snap.LocalHand.Count; i++)
            {
                if (snap.LocalHand[i].InstanceId == instanceId)
                    return snap.Phase == Phase.Main || snap.Phase == Phase.Site;
            }

            return false;
        }

        public bool IsBodyInDeclareQueue(int instanceId)
        {
            for (var i = 0; i < snapshot.DeclareQueue.Count; i++)
            {
                if (snapshot.DeclareQueue[i] == instanceId)
                    return true;
            }

            return false;
        }

        bool IsLegalDropInternal(int instanceId, TableZoneKind zone, int? targetInstanceId)
        {
            if (actionHost == null || !actionHost.IsActive)
                return false;

            var snap = snapshot;
            if (snap.PriorityPlayerId != snap.LocalPlayerId)
                return false;

            if (IsBodyInDeclareQueue(instanceId))
            {
                return zone switch
                {
                    TableZoneKind.HoldPlate => snap.ClashPhase == ClashPhase.C1_ActiveDeclare,
                    TableZoneKind.Field => snap.ClashPhase == ClashPhase.C1_ActiveDeclare
                        && (targetInstanceId.HasValue || DefaultPressTargetId().HasValue),
                    _ => false,
                };
            }

            CardSnapshot card = null;
            for (var i = 0; i < snap.LocalHand.Count; i++)
            {
                if (snap.LocalHand[i].InstanceId == instanceId)
                {
                    card = snap.LocalHand[i];
                    break;
                }
            }

            if (card == null)
                return false;

            return zone switch
            {
                TableZoneKind.Field when snap.Phase == Phase.Main =>
                    card.Type is CardType.Companion or CardType.Relic or CardType.Bond or CardType.Icon,
                TableZoneKind.Willwell when snap.Phase == Phase.Site =>
                    card.Type == CardType.WillSite,
                TableZoneKind.StackWell when snap.Phase == Phase.Main =>
                    card.Type is CardType.Surge or CardType.Algorithm,
                _ => false,
            };
        }

        public bool IsLegalDrop(int instanceId, TableZoneKind zone, int? targetInstanceId = null) =>
            IsLegalDropInternal(instanceId, zone, targetInstanceId);

        public bool IsLegalPressTarget(int attackerInstanceId, int targetInstanceId)
        {
            if (actionHost == null || !actionHost.IsActive)
                return false;

            var snap = snapshot;
            if (snap.PriorityPlayerId != snap.LocalPlayerId)
                return false;
            if (snap.ClashPhase != ClashPhase.C1_ActiveDeclare)
                return false;
            if (!IsBodyInDeclareQueue(attackerInstanceId) || attackerInstanceId == targetInstanceId)
                return false;

            for (var i = 0; i < snap.OpponentField.Count; i++)
            {
                var card = snap.OpponentField[i];
                if (card == null || card.InstanceId != targetInstanceId)
                    continue;
                return card.Type is CardType.Icon or CardType.Companion or CardType.Token;
            }

            return false;
        }

        public int? DefaultPressTargetId()
        {
            var snap = snapshot;
            int? iconId = null;
            int? bodyId = null;
            for (var i = 0; i < snap.OpponentField.Count; i++)
            {
                var card = snap.OpponentField[i];
                if (card == null)
                    continue;
                if (card.Type == CardType.Icon)
                    iconId = card.InstanceId;
                else if (bodyId == null && card.Type is CardType.Companion or CardType.Token)
                    bodyId = card.InstanceId;
            }

            return iconId ?? bodyId;
        }

        public PlayerAction BuildDropAction(int instanceId, TableZoneKind zone, int? targetInstanceId = null, int? storeSlotIndex = null)
        {
            var action = new PlayerAction
            {
                PlayerId = snapshot.LocalPlayerId,
                CardInstanceId = instanceId,
            };

            if (IsBodyInDeclareQueue(instanceId))
            {
                if (zone == TableZoneKind.HoldPlate)
                {
                    action.Kind = PlayerActionKind.DeclareHold;
                    action.CardInstanceId = instanceId;
                    return action;
                }

                action.Kind = PlayerActionKind.DeclarePress;
                action.CardInstanceId = instanceId;
                action.TargetInstanceId = targetInstanceId;
                return action;
            }

            switch (zone)
            {
                case TableZoneKind.Field:
                case TableZoneKind.Willwell:
                case TableZoneKind.StackWell:
                    action.Kind = PlayerActionKind.PlayCard;
                    break;
                case TableZoneKind.Hand:
                    action.Kind = PlayerActionKind.StoreBuy;
                    action.StoreSlotIndex = storeSlotIndex;
                    action.StoreKind = StoreActionKind.Buy;
                    break;
                case TableZoneKind.Store:
                    action.Kind = PlayerActionKind.StoreSell;
                    action.HandCardInstanceId = instanceId;
                    action.StoreKind = StoreActionKind.Sell;
                    break;
                default:
                    action.Kind = PlayerActionKind.Pass;
                    break;
            }

            return action;
        }

        public static TableSnapshot BuildSnapshot(Match match, int localPlayerId)
        {
            var snap = new TableSnapshot
            {
                LocalPlayerId = localPlayerId,
                Phase = match.Phase,
                ClashPhase = match.ClashPhase,
                ActivePlayerId = match.ActivePlayerId,
                PriorityPlayerId = match.PriorityPlayerId,
                ClashLocked = match.ClashLocked,
                DeclareQueue = match.BodiesAwaitingDeclare != null
                    ? new List<int>(match.BodiesAwaitingDeclare)
                    : new List<int>(),
                StackCount = match.Stack?.Count ?? 0,
            };

            var local = match.GetPlayer(localPlayerId);
            if (local != null)
            {
                snap.LocalWill = local.Will;
                snap.LocalWorth = local.Worth;
                snap.LocalHonor = local.Honor;
                snap.LocalHand = BuildCardList(local.Hand);
                snap.LocalField = BuildFieldList(local);
                snap.LocalWillwell = BuildCardList(local.Willwell);
            }

            var opponentId = localPlayerId == 0 ? 1 : 0;
            var opponent = match.GetPlayer(opponentId);
            if (opponent != null)
            {
                snap.OpponentField = BuildFieldList(opponent);
                snap.OpponentWillwell = BuildCardList(opponent.Willwell);
            }

            for (var i = 0; i < match.Store.Length; i++)
                snap.Store[i] = match.Store[i] != null ? ToCardSnapshot(match.Store[i]) : null;

            return snap;
        }

        static List<CardSnapshot> BuildFieldList(Player player)
        {
            var list = new List<CardSnapshot>();
            if (player.Icon != null)
                list.Add(ToCardSnapshot(player.Icon));
            AddCards(list, player.Field);
            return list;
        }

        static List<CardSnapshot> BuildCardList(List<CardInstance> cards)
        {
            var list = new List<CardSnapshot>();
            AddCards(list, cards);
            return list;
        }

        static void AddCards(List<CardSnapshot> list, List<CardInstance> cards)
        {
            if (cards == null)
                return;

            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                    list.Add(ToCardSnapshot(cards[i]));
            }
        }

        static CardSnapshot ToCardSnapshot(CardInstance card)
        {
            return new CardSnapshot
            {
                InstanceId = card.InstanceId,
                PrintingId = card.Printing?.Id,
                Name = card.Printing?.Name,
                Type = card.Printing?.Type ?? CardType.Companion,
                WillCost = card.Printing?.WillCost ?? 0,
                StoreWorth = card.Printing?.StoreWorth ?? 0,
                Strike = card.Strike,
                Guard = card.Guard,
                Health = card.Health,
                CurrentHealth = card.CurrentHealth,
                Exhausted = card.Exhausted,
                Ready = card.Ready,
                Zone = card.Zone,
                ControllerId = card.ControllerId,
            };
        }
    }
}
