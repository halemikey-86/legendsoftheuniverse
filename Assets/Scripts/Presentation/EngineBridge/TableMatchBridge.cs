using System;
using System.Collections.Generic;
using LegendsOfTheUniverse.Rules;
using UnityEngine;
using Willbound.Engine;

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
        [SerializeField] string localIconPrintingId = "VANILLA-ICON";
        [SerializeField] string opponentIconPrintingId = "VANILLA-ICON";

        MatchRunner runner;
        InMemoryCardDatabase database;
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

        public void BeginSoloMatch()
        {
            try
            {
                var printings = EngineCatalog.LoadPrintings();
                database = new InMemoryCardDatabase(printings);
                var rng = new SeededRng(rngSeed);
                var players = new List<SetupPlayer>
                {
                    new SetupPlayer
                    {
                        PlayerId = 0,
                        IconId = localIconPrintingId,
                        DeckIds = EngineCatalog.DefaultDeck(30),
                    },
                    new SetupPlayer
                    {
                        PlayerId = 1,
                        IconId = opponentIconPrintingId,
                        DeckIds = EngineCatalog.DefaultDeck(30),
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

        public bool TryStoreKeep(out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            var result = ApplyWithAutoPass(new PlayerAction
            {
                Kind = PlayerActionKind.StoreKeep,
                PlayerId = localPlayerId,
                StoreKind = StoreActionKind.Keep,
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
