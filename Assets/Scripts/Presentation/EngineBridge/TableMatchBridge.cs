using System;
using System.Collections.Generic;
using LegendsOfTheUniverse.Rules;
using UnityEngine;
using Willbound.Engine;
using GameEvent = Willbound.Engine.GameEvent;
using StoreActionKind = Willbound.Engine.StoreActionKind;
using CardType = Willbound.Engine.CardType;

namespace LegendsOfTheUniverse.Presentation.EngineBridge
{
    /// <summary>
    /// Unity adapter: actions in via Apply(), facts out via IMatchView.OnEvent.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-120)]
    public sealed class TableMatchBridge : MonoBehaviour, IMatchView
    {
        const int MaxAutoPasses = 24;

        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] TurnFlowController turnFlow;
        [SerializeField] int localPlayerId;
        [SerializeField] int rngSeed = 42;
        [SerializeField] string localIconPrintingId = "gk-01";
        [SerializeField] string opponentIconPrintingId = "gk-01";

        MatchRunner runner;
        InMemoryCardDatabase database;
        string pendingLocalIconId;
        List<string> pendingLocalHandIds;
        readonly Queue<GameEvent> eventQueue = new Queue<GameEvent>();

        public bool IsActive => runner != null;
        public MatchRunner Runner => runner;
        public int LocalPlayerId => localPlayerId;

        public event Action<TurnStep, bool> PhaseStateChanged;
        public event Action<string> EngineError;
        public event Action<IReadOnlyList<GameEvent>> EngineEventsApplied;

        void Awake()
        {
            if (playmatZones == null)
                playmatZones = GetComponent<PlaymatZonesView>();
            if (turnFlow == null)
                turnFlow = GetComponent<TurnFlowController>();
        }

        void OnDestroy()
        {
            if (botRoutine != null)
            {
                StopCoroutine(botRoutine);
                botRoutine = null;
            }

            botActing = false;
        }

        public void BeginSoloMatch() => BeginMatch();

        /// <summary>Carries the Icon and kept opening hand the player actually chose into the real match.
        /// Call before BeginMatch(); any card that doesn't resolve to a known printing is simply skipped
        /// and its hand slot is filled from the deck instead.</summary>
        public void ConfigureLocalPlayer(string iconPrintingId, IReadOnlyList<string> handPrintingIds)
        {
            pendingLocalIconId = iconPrintingId;
            pendingLocalHandIds = handPrintingIds != null ? new List<string>(handPrintingIds) : null;
        }

        public void BeginMatch()
        {
            try
            {
                var printings = EngineCatalog.LoadPrintings();
                database = new InMemoryCardDatabase(printings);
                var rng = new SeededRng(rngSeed);
                var localIconId = ResolveIconId(printings, pendingLocalIconId ?? localIconPrintingId);
                var localHandIds = FilterKnownPrintingIds(printings, pendingLocalHandIds);
                var players = new List<SetupPlayer>
                {
                    new SetupPlayer
                    {
                        PlayerId = 0,
                        IconId = localIconId,
                        DeckIds = EngineCatalog.DefaultDeck(localIconId, 30),
                        HandIds = localHandIds,
                    },
                    new SetupPlayer
                    {
                        PlayerId = 1,
                        IconId = ResolveIconId(printings, opponentIconPrintingId),
                        DeckIds = EngineCatalog.DefaultDeck(opponentIconPrintingId, 30),
                    },
                };

                var match = MatchSetup.Create(database, rng, players, localPlayerId);
                runner = new MatchRunner(match, database, rng, this);
                SyncHudFromEngine();
                RaisePhaseChanged(false);
                Debug.Log("[TableMatchBridge] Engine B match started.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TableMatchBridge] Failed to start engine match: {ex.Message}");
                EngineError?.Invoke("Rules engine failed to start — solo UI will continue without engine validation.");
            }
        }

        static string ResolveIconId(IReadOnlyList<CardPrinting> printings, string requestedId)
        {
            CardPrinting firstIcon = null;
            for (var i = 0; i < printings.Count; i++)
            {
                var p = printings[i];
                if (p.Type != CardType.Icon)
                    continue;
                if (p.Id == requestedId)
                    return requestedId;
                firstIcon ??= p;
            }

            return firstIcon?.Id ?? requestedId;
        }

        static List<string> FilterKnownPrintingIds(IReadOnlyList<CardPrinting> printings, List<string> requestedIds)
        {
            var result = new List<string>();
            if (requestedIds == null)
                return result;

            for (var i = 0; i < requestedIds.Count; i++)
            {
                var id = requestedIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;

                for (var p = 0; p < printings.Count; p++)
                {
                    if (printings[p].Id != id)
                        continue;

                    // Icons only ever belong in Zone.Field via spec.IconId — never as a hand card,
                    // even if the same character also has ordinary card art (e.g. a numbered-set print).
                    if (printings[p].Type != CardType.Icon)
                        result.Add(id);
                    break;
                }
            }

            return result;
        }

        public ApplyResult TryApply(PlayerAction action)
        {
            if (!IsActive)
                return ApplyResult.Fail("Engine not active.", null);

            var result = ApplyWithAutoPass(action);
            if (!result.Success)
            {
                EngineError?.Invoke(result.Error);
                return result;
            }

            ProcessEvents(result.Events);
            SyncHudFromEngine();
            RaisePhaseChanged(false);
            EngineEventsApplied?.Invoke(result.Events);
            return result;
        }

        public bool TryPassPriority(out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            var result = ApplyWithAutoPass(new PlayerAction
            {
                Kind = PlayerActionKind.Pass,
                PlayerId = localPlayerId,
            });

            if (!result.Success)
            {
                error = result.Error;
                EngineError?.Invoke(error);
                return false;
            }

            ProcessEvents(result.Events);
            SyncHudFromEngine();
            RaisePhaseChanged(false);
            return true;
        }

        public bool TryPlayCard(int cardInstanceId, out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            var result = ApplyWithAutoPass(new PlayerAction
            {
                Kind = PlayerActionKind.PlayCard,
                PlayerId = localPlayerId,
                CardInstanceId = cardInstanceId,
            });

            if (!result.Success)
            {
                error = result.Error;
                EngineError?.Invoke(error);
                return false;
            }

            ProcessEvents(result.Events);
            SyncHudFromEngine();
            RaisePhaseChanged(false);
            return true;
        }

        public bool TryStoreBuy(int storeSlotIndex, out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            if (!EnginePhaseMapper.StoreAllowed(runner.Match.Phase, runner.Match, localPlayerId))
            {
                error = "Store actions are only available during your Main phase (once per turn).";
                return false;
            }

            var result = ApplyWithAutoPass(new PlayerAction
            {
                Kind = PlayerActionKind.StoreBuy,
                PlayerId = localPlayerId,
                StoreSlotIndex = storeSlotIndex,
                StoreKind = StoreActionKind.Buy,
            });

            if (!result.Success)
            {
                error = result.Error;
                EngineError?.Invoke(error);
                return false;
            }

            ProcessEvents(result.Events);
            SyncHudFromEngine();
            RaisePhaseChanged(false);
            return true;
        }

        public bool TryStoreSell(int handCardInstanceId, out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            if (!EnginePhaseMapper.StoreAllowed(runner.Match.Phase, runner.Match, localPlayerId))
            {
                error = "Store actions are only available during your Main phase (once per turn).";
                return false;
            }

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var result = ApplyThenAdvance(new PlayerAction
            {
                Kind = PlayerActionKind.StoreSell,
                PlayerId = localPlayerId,
                HandCardInstanceId = handCardInstanceId,
                StoreKind = StoreActionKind.Sell,
            });

            if (!result.Success)
            {
                error = result.Error;
                EngineError?.Invoke(error);
                return false;
            }

            PublishResult(result);
            KickNonLocalActors();
            return true;
        }

        public bool TryStoreSell(int handCardInstanceId, out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            if (!EnginePhaseMapper.StoreAllowed(runner.Match.Phase, runner.Match, localPlayerId))
            {
                error = "Store actions are only available during your Main phase (once per turn).";
                return false;
            }

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var result = ApplyThenAdvance(new PlayerAction
            {
                Kind = PlayerActionKind.StoreSell,
                PlayerId = localPlayerId,
                HandCardInstanceId = handCardInstanceId,
                StoreKind = StoreActionKind.Sell,
            });

            if (!result.Success)
            {
                error = result.Error;
                EngineError?.Invoke(error);
                return false;
            }

            PublishResult(result);
            KickNonLocalActors();
            return true;
        }

        ApplyResult ApplyThenAdvance(PlayerAction action)
        {
            return runner.Apply(action);
        }

        void PublishResult(ApplyResult result)
        {
            if (result == null || !result.Success)
                return;

            ProcessEvents(result.Events);
            SyncHudFromEngine();
            RaisePhaseChanged(false);
            return true;
        }

        ApplyResult ApplyWithAutoPass(PlayerAction action)
        {
            var result = runner.Apply(action);
            if (!result.Success)
                return result;

            AutoPassNonLocalPriority();
            return result;
        }

        void AutoPassNonLocalPriority()
        {
            if (!IsActive)
                return;

            for (var i = 0; i < MaxAutoPasses; i++)
            {
                var match = runner.Match;
                if (match.WinnerId.HasValue)
                    break;

                if (match.PriorityPlayerId == localPlayerId)
                    break;

                var priorityPlayer = match.GetPlayer(match.PriorityPlayerId);
                if (priorityPlayer == null || priorityPlayer.Lost)
                    break;

                var passResult = runner.Apply(new PlayerAction
                {
                    Kind = PlayerActionKind.Pass,
                    PlayerId = match.PriorityPlayerId,
                });

                if (!passResult.Success)
                    break;

                ProcessEvents(passResult.Events);
            }
        }

        public void OnEvent(GameEvent e)
        {
            if (e != null)
                eventQueue.Enqueue(e);
        }

        void ProcessEvents(IReadOnlyList<GameEvent> events)
        {
            if (events == null)
                return;

            for (var i = 0; i < events.Count; i++)
                HandleEvent(events[i]);
        }

        void HandleEvent(GameEvent e)
        {
            switch (e.Kind)
            {
                case EventKind.WillSet:
                    if (TryInt(e, "player", out var willPlayer) && willPlayer == localPlayerId && TryInt(e, "amount", out var will))
                        playmatZones?.SetWillPool(will);
                    break;
                case EventKind.RoundChanged:
                    if (TryInt(e, "round", out var round))
                        playmatZones?.SetRound(round);
                    break;
                case EventKind.WorthChanged:
                    if (TryInt(e, "player", out var worthPlayer) && worthPlayer == localPlayerId)
                    {
                        var player = runner.Match.GetPlayer(localPlayerId);
                        if (player != null)
                            playmatZones?.SetWorth(player.Worth);
                    }
                    break;
                case EventKind.PhaseChanged:
                    RaisePhaseChanged(false);
                    break;
                case EventKind.TurnStarted:
                    SyncHudFromEngine();
                    RaisePhaseChanged(false);
                    break;
                case EventKind.WillPaid:
                    if (TryInt(e, "player", out var paidPlayer) && paidPlayer == localPlayerId && TryInt(e, "amount", out var paidAmount))
                    {
                        var player = runner.Match.GetPlayer(localPlayerId);
                        if (player != null)
                            playmatZones?.SetWillPool(player.Will);
                    }
                    break;
                case EventKind.Exhausted:
                case EventKind.Readied:
                case EventKind.HealthChanged:
                case EventKind.PressDeclared:
                case EventKind.HoldDeclared:
                case EventKind.ClashLocked:
                case EventKind.StackPushed:
                case EventKind.StackPopped:
                case EventKind.PermanentEntered:
                case EventKind.Silenced:
                    break;
                case EventKind.PlayerWon:
                    Debug.Log("[TableMatchBridge] Match won.");
                    break;
            }
        }

        void SyncHudFromEngine()
        {
            if (!IsActive)
                return;

            var match = runner.Match;
            var player = match.GetPlayer(localPlayerId);
            playmatZones?.SetRound(match.Round);
            if (player != null)
            {
                playmatZones?.SetWillPool(player.Will);
                playmatZones?.SetWorth(player.Worth);
                playmatZones?.SetHonor(player.Honor);
            }
        }

        public TurnStep CurrentTurnStep =>
            IsActive ? EnginePhaseMapper.ToTurnStep(runner.Match.Phase) : TurnStep.WillSite;

        public int CurrentRound => IsActive ? runner.Match.Round : 1;

        public int CurrentWillPool
        {
            get
            {
                if (!IsActive)
                    return 0;
                var player = runner.Match.GetPlayer(localPlayerId);
                return player?.Will ?? 0;
            }
        }

        public bool IsStoreAllowed =>
            IsActive && EnginePhaseMapper.StoreAllowed(runner.Match.Phase, runner.Match, localPlayerId);

        public bool ShouldShowNextPhaseButton
        {
            get
            {
                if (!IsActive)
                    return false;
                var step = CurrentTurnStep;
                return step == TurnStep.WillSite || step == TurnStep.Main;
            }
        }

        public bool ShouldShowEndTurnButton =>
            IsActive && CurrentTurnStep == TurnStep.Clash;

        void RaisePhaseChanged(bool discardPending)
        {
            PhaseStateChanged?.Invoke(CurrentTurnStep, discardPending);
        }

        static bool TryInt(GameEvent e, string key, out int value)
        {
            value = 0;
            if (e?.Data == null || !e.Data.TryGetValue(key, out var raw) || raw == null)
                return false;
            try
            {
                value = Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
