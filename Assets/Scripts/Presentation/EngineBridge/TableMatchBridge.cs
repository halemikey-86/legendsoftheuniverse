using System;
using System.Collections;
using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation.Menu;
using LegendsOfTheUniverse.Rules;
using UnityEngine;
using Willbound.Engine;
using GameEvent = Willbound.Engine.GameEvent;
using CardInstance = Willbound.Engine.CardInstance;
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
        const int MaxBotActions = 64;

        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] TurnFlowController turnFlow;
        [SerializeField] int localPlayerId;
        [SerializeField] int rngSeed = 42;
        [SerializeField] string localIconPrintingId = "gk-01";
        [SerializeField] string opponentIconPrintingId = "gk-01";
        [SerializeField] float botActionDelay = 0.4f;

        MatchRunner runner;
        InMemoryCardDatabase database;
        readonly Queue<GameEvent> eventQueue = new Queue<GameEvent>();
        readonly HeuristicBot bot = new HeuristicBot();
        Coroutine botRoutine;
        bool botActing;

        public bool IsActive => runner != null;
        public MatchRunner Runner => runner;
        public int LocalPlayerId => localPlayerId;
        public bool IsBotMatch => MatchLaunch.Mode == MatchMode.Bot;
        public bool IsLocalActivePlayer =>
            IsActive && runner.Match.ActivePlayerId == localPlayerId;
        public bool HasLocalPriority =>
            IsActive && runner.Match.PriorityPlayerId == localPlayerId && !botActing;

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

        public void BeginMatch()
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
                        IconId = ResolveIconId(printings, localIconPrintingId),
                        DeckIds = EngineCatalog.DefaultDeck(30),
                    },
                    new SetupPlayer
                    {
                        PlayerId = 1,
                        IconId = ResolveIconId(printings, opponentIconPrintingId),
                        DeckIds = EngineCatalog.DefaultDeck(30),
                    },
                };

                var match = MatchSetup.Create(database, rng, players, localPlayerId);
                runner = new MatchRunner(match, database, rng, this);
                SyncHudFromEngine();
                RaisePhaseChanged(false);
                Debug.Log(IsBotMatch
                    ? "[TableMatchBridge] Bot match started."
                    : "[TableMatchBridge] Engine B match started.");
                KickNonLocalActors();
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

        public ApplyResult TryApply(PlayerAction action)
        {
            if (!IsActive)
                return ApplyResult.Fail("Engine not active.", null);

            if (botActing && action.PlayerId == localPlayerId)
                return ApplyResult.Fail("Opponent is acting.", runner.Match);

            var result = ApplyThenAdvance(action);
            if (!result.Success)
            {
                EngineError?.Invoke(result.Error);
                return result;
            }

            PublishResult(result);
            KickNonLocalActors();
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

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var result = ApplyThenAdvance(new PlayerAction
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

            PublishResult(result);
            KickNonLocalActors();
            return true;
        }

        public bool TryResolveClash(out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var match = runner.Match;
            if (match.Phase == Phase.Clash && match.ClashPhase == ClashPhase.C1_ActiveDeclare)
            {
                var leftover = new List<int>(match.BodiesAwaitingDeclare);
                for (var i = 0; i < leftover.Count; i++)
                {
                    var hold = runner.Apply(new PlayerAction
                    {
                        Kind = PlayerActionKind.DeclareHold,
                        PlayerId = localPlayerId,
                        CardInstanceId = leftover[i],
                    });
                    if (hold.Success)
                        PublishResult(hold);
                }
            }

            return TryPassPriority(out error);
        }

        public bool CanPlayCard(int cardInstanceId)
        {
            if (!IsActive)
                return false;

            var match = runner.Match;
            if (match.WinnerId.HasValue)
                return false;

            var player = match.GetPlayer(localPlayerId);
            if (player == null || player.Lost)
                return false;

            if (botActing || localPlayerId != match.PriorityPlayerId)
                return false;

            var card = match.GetCard(cardInstanceId);
            if (card?.Printing == null || !player.Hand.Contains(card))
                return false;

            var printing = card.Printing;
            if (printing.Type == CardType.WillSite)
            {
                if (match.Phase != Phase.Site || player.Id != match.ActivePlayerId)
                    return false;
                if (player.SitesPlayedThisTurn >= 1)
                    return false;
            }
            else if (printing.Type == CardType.Algorithm)
            {
                if (player.Id != match.ActivePlayerId)
                    return false;
            }
            else if (printing.Type == CardType.Surge || HasNowTiming(card))
            {
                // Now is legal with priority.
            }
            else if (player.Id != match.ActivePlayerId || match.Phase != Phase.Main)
            {
                return false;
            }

            return player.Will >= printing.WillCost;
        }

        static bool HasNowTiming(CardInstance card)
        {
            var abilities = card.Printing?.Abilities;
            if (abilities == null)
                return false;

            for (var i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].Timing == Timing.Now)
                    return true;
            }

            return false;
        }

        public bool TryPlayCard(int cardInstanceId, out string error)
        {
            error = null;
            if (!IsActive)
            {
                error = "Engine not active.";
                return false;
            }

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var result = ApplyThenAdvance(new PlayerAction
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

            PublishResult(result);
            KickNonLocalActors();
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

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var result = ApplyThenAdvance(new PlayerAction
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

            PublishResult(result);
            KickNonLocalActors();
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

            if (botActing)
            {
                error = "Opponent is acting.";
                return false;
            }

            var result = ApplyThenAdvance(new PlayerAction
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
            EngineEventsApplied?.Invoke(result.Events);
        }

        void KickNonLocalActors()
        {
            if (!IsActive)
                return;

            if (!IsBotMatch)
            {
                AutoPassNonLocalPriority();
                return;
            }

            if (botActing)
                return;

            var match = runner.Match;
            if (match.PriorityPlayerId == localPlayerId && !ShouldAutoYieldLocalPriority(match))
                return;

            botRoutine = StartCoroutine(BotPlayRoutine());
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

        IEnumerator BotPlayRoutine()
        {
            botActing = true;
            try
            {
                for (var i = 0; i < MaxBotActions; i++)
                {
                    if (!IsActive)
                        yield break;

                    var match = runner.Match;
                    if (match.WinnerId.HasValue)
                        yield break;

                    if (match.PriorityPlayerId == localPlayerId)
                    {
                        if (!ShouldAutoYieldLocalPriority(match))
                            yield break;

                        var beforePhase = match.Phase;
                        var beforeClash = match.ClashPhase;
                        var yieldResult = runner.Apply(new PlayerAction
                        {
                            Kind = PlayerActionKind.Pass,
                            PlayerId = localPlayerId,
                        });
                        if (!yieldResult.Success)
                            yield break;

                        PublishResult(yieldResult);
                        if (runner.Match.PriorityPlayerId == localPlayerId
                            && runner.Match.Phase == beforePhase
                            && runner.Match.ClashPhase == beforeClash)
                            yield break;
                        continue;
                    }

                    var actor = match.GetPlayer(match.PriorityPlayerId);
                    if (actor == null || actor.Lost)
                        yield break;

                    var action = bot.Choose(runner, match.PriorityPlayerId);
                    if (action.Kind != PlayerActionKind.Pass)
                    {
                        if (botActionDelay > 0f)
                            yield return new WaitForSeconds(botActionDelay);
                        else
                            yield return null;

                        if (!IsActive || runner.Match.WinnerId.HasValue)
                            yield break;
                        if (runner.Match.PriorityPlayerId == localPlayerId)
                            continue;

                        action = bot.Choose(runner, runner.Match.PriorityPlayerId);
                    }

                    var result = runner.Apply(action);
                    if (!result.Success)
                    {
                        result = runner.Apply(new PlayerAction
                        {
                            Kind = PlayerActionKind.Pass,
                            PlayerId = runner.Match.PriorityPlayerId,
                        });
                        if (!result.Success)
                            yield break;
                    }

                    PublishResult(result);
                }
            }
            finally
            {
                botActing = false;
                botRoutine = null;
            }
        }

        bool ShouldAutoYieldLocalPriority(Match match)
        {
            if (match.Phase == Phase.Clash && match.ClashPhase == ClashPhase.C2_Answers
                && StackIsOnlyPresses(match))
            {
                var local = match.GetPlayer(localPlayerId);
                return local == null || !HasReadyClashBody(local);
            }

            if (match.ActivePlayerId == localPlayerId)
                return OwnPlayWaitingToResolve(match);

            return !LocalHasResponse(match);
        }

        static bool StackIsOnlyPresses(Match match)
        {
            if (match.Stack == null || match.Stack.Count == 0)
                return true;

            for (var i = 0; i < match.Stack.Count; i++)
            {
                if (match.Stack[i].Type != StackObjectType.Press)
                    return false;
            }

            return true;
        }

        bool OwnPlayWaitingToResolve(Match match)
        {
            if (match.Stack == null || match.Stack.Count == 0)
                return false;

            var top = match.Stack[match.Stack.Count - 1];
            return top.ControllerId == localPlayerId && top.Type != StackObjectType.Press;
        }

        bool LocalHasResponse(Match match)
        {
            if (match.Phase == Phase.Clash && match.ClashPhase == ClashPhase.C2_Answers
                && match.Stack != null && match.Stack.Count > 0)
            {
                var local = match.GetPlayer(localPlayerId);
                if (local != null && HasReadyClashBody(local))
                    return true;
            }

            if (match.Stack == null || match.Stack.Count == 0)
                return false;

            var player = match.GetPlayer(localPlayerId);
            if (player == null)
                return false;

            for (var i = 0; i < player.Hand.Count; i++)
            {
                var card = player.Hand[i];
                if (card?.Printing == null)
                    continue;
                if (card.Printing.Type != CardType.Surge && !HasNowTiming(card))
                    continue;
                if (EnginePlayRules.CanPlayFromHand(match, localPlayerId, card.InstanceId))
                    return true;
            }

            return false;
        }

        static bool HasReadyClashBody(Player player)
        {
            for (var i = 0; i < player.Field.Count; i++)
            {
                var body = player.Field[i];
                if (body == null || !body.Ready || body.Exhausted || body.CurrentHealth <= 0)
                    continue;
                var type = body.Printing?.Type;
                if (type == CardType.Icon || type == CardType.Companion || type == CardType.Token)
                    return true;
            }

            return false;
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
                if (!HasLocalPriority || !IsLocalActivePlayer)
                    return false;
                var step = CurrentTurnStep;
                return step == TurnStep.WillSite || step == TurnStep.Main;
            }
        }

        public bool ShouldShowEndTurnButton =>
            HasLocalPriority && IsLocalActivePlayer && CurrentTurnStep == TurnStep.Clash;

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
