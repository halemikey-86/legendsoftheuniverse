using System;
using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class MatchRunner
    {
        readonly ICardDatabase database;
        readonly IRng rng;
        readonly IMatchView view;
        readonly List<GameEvent> pendingEvents = new List<GameEvent>();

        public Match Match { get; private set; }

        public MatchRunner(Match match, ICardDatabase database, IRng rng, IMatchView view = null)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.view = view;
        }

        public static MatchRunner FromSetup(IEnumerable<CardPrinting> printings, IList<SetupPlayer> players, int seed = 1, int firstActive = 0)
        {
            var db = new InMemoryCardDatabase(printings);
            var rng = new SeededRng(seed);
            var match = MatchSetup.Create(db, rng, players, firstActive);
            return new MatchRunner(match, db, rng);
        }

        public ApplyResult Apply(PlayerAction action)
        {
            pendingEvents.Clear();
            var snapshot = CloneMatchForFail();

            try
            {
                var error = ValidateAndApply(action);
                if (error != null)
                    return ApplyResult.Fail(error, snapshot);

                RunChecks();
                FlushEvents();
                return ApplyResult.Ok(Match, pendingEvents.ToArray());
            }
            catch (Exception ex)
            {
                return ApplyResult.Fail(ex.Message, snapshot);
            }
        }

        Match CloneMatchForFail() => Match;

        string ValidateAndApply(PlayerAction action)
        {
            if (Match.WinnerId.HasValue)
                return "Match is over.";

            var player = Match.GetPlayer(action.PlayerId);
            if (player == null || player.Lost)
                return "Invalid player.";

            if (action.PlayerId != Match.PriorityPlayerId && action.Kind != PlayerActionKind.DeclarePress
                && action.Kind != PlayerActionKind.DeclareHold && action.Kind != PlayerActionKind.Answer)
                return "Not your priority.";

            switch (action.Kind)
            {
                case PlayerActionKind.Pass:
                    return Pass(player);
                case PlayerActionKind.PlayCard:
                    return PlayCard(player, action);
                case PlayerActionKind.DeclarePress:
                    return DeclarePress(player, action);
                case PlayerActionKind.DeclareHold:
                    return DeclareHold(player, action);
                case PlayerActionKind.Answer:
                    return AnswerPress(player, action);
                case PlayerActionKind.Silence:
                    return Silence(player, action);
                case PlayerActionKind.StoreBuy:
                case PlayerActionKind.StoreSell:
                case PlayerActionKind.StoreTrade:
                case PlayerActionKind.StoreKeep:
                case PlayerActionKind.StoreList:
                case PlayerActionKind.StoreRow:
                    return StoreAction(player, action);
                default:
                    return "Unsupported action.";
            }
        }

        string Pass(Player player)
        {
            Match.Passed.Add(player.Id);
            if (!AllLivingPassed())
            {
                GivePriorityToNextLiving();
                Emit(EventKind.PriorityChanged, "player", Match.PriorityPlayerId);
                return null;
            }

            Match.Passed.Clear();
            if (Match.Stack.Count > 0)
            {
                ResolveTopStack();
                Match.PriorityPlayerId = Match.ActivePlayerId;
                Emit(EventKind.PriorityChanged, "player", Match.PriorityPlayerId);
                return null;
            }

            AdvanceStepOrClashPhase();
            return null;
        }

        bool AllLivingPassed()
        {
            var living = Match.LivingPlayers();
            if (living.Count == 0)
                return true;
            for (var i = 0; i < living.Count; i++)
            {
                if (!Match.Passed.Contains(living[i].Id))
                    return false;
            }

            return true;
        }

        void GivePriorityToNextLiving()
        {
            var living = Match.LivingPlayers();
            var idx = 0;
            for (var i = 0; i < living.Count; i++)
            {
                if (living[i].Id == Match.PriorityPlayerId)
                {
                    idx = i;
                    break;
                }
            }

            for (var step = 1; step <= living.Count; step++)
            {
                var next = living[(idx + step) % living.Count];
                if (!next.Lost)
                {
                    Match.PriorityPlayerId = next.Id;
                    return;
                }
            }
        }

        void AdvanceStepOrClashPhase()
        {
            if (Match.Phase == Phase.Clash)
            {
                AdvanceClashPhase();
                return;
            }

            switch (Match.Phase)
            {
                case Phase.Site:
                    Match.Phase = Phase.Main;
                    Match.PriorityPlayerId = Match.ActivePlayerId;
                    Emit(EventKind.PhaseChanged, "phase", Phase.Main.ToString());
                    break;
                case Phase.Main:
                    BeginClash();
                    break;
                case Phase.End:
                    EndTurn();
                    break;
                default:
                    break;
            }
        }

        void EndTurn()
        {
            var lastInRound = true;
            for (var i = 0; i < Match.Players.Count; i++)
            {
                if (!Match.Players[i].Lost)
                {
                    lastInRound = Match.Players[i].Id == Match.ActivePlayerId;
                    if (!lastInRound)
                        break;
                }
            }

            var activeIdx = IndexOfPlayer(Match.ActivePlayerId);
            var nextIdx = (activeIdx + 1) % Match.Players.Count;
            var wrapped = false;
            while (Match.Players[nextIdx].Lost)
            {
                nextIdx = (nextIdx + 1) % Match.Players.Count;
                if (nextIdx == activeIdx)
                {
                    wrapped = true;
                    break;
                }
            }

            if (wrapped || nextIdx <= activeIdx)
                Match.Round++;

            MatchSetup.AdvanceActivePlayer(Match);
            MatchSetup.BeginTurn(Match, rng);
            Emit(EventKind.TurnStarted, "player", Match.ActivePlayerId);
            if (Match.Round > 1)
                Emit(EventKind.RoundChanged, "round", Match.Round);
        }

        int IndexOfPlayer(int playerId)
        {
            for (var i = 0; i < Match.Players.Count; i++)
            {
                if (Match.Players[i].Id == playerId)
                    return i;
            }

            return 0;
        }

        void BeginClash()
        {
            Match.Phase = Phase.Clash;
            Match.ClashPhase = ClashPhase.C0_Begin;
            Match.ClashQueue.Clear();
            Match.ClashLocked = false;
            Match.PressesDeclaredThisClash = 0;
            Match.FirstPressAggressionGranted = false;
            Match.DoubleteamStrikeBonus.Clear();
            Match.DoubleteamGuardBonus.Clear();
            Match.BodiesAwaitingDeclare.Clear();

            var active = Match.GetPlayer(Match.ActivePlayerId);
            for (var i = 0; i < active.Field.Count; i++)
            {
                var body = active.Field[i];
                if (body.Printing.Type is CardType.Icon or CardType.Companion or CardType.Token)
                {
                    if (body.Ready && !body.Exhausted && body.CurrentHealth > 0)
                        Match.BodiesAwaitingDeclare.Add(body.InstanceId);
                }
            }

            Match.ClashPhase = ClashPhase.C1_ActiveDeclare;
            Match.PriorityPlayerId = Match.ActivePlayerId;
            Emit(EventKind.PhaseChanged, "phase", Phase.Clash.ToString(), "clashPhase", ClashPhase.C1_ActiveDeclare.ToString());
        }

        string DeclarePress(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.Clash || Match.ClashPhase != ClashPhase.C1_ActiveDeclare)
                return "Not in active declare.";
            if (player.Id != Match.ActivePlayerId)
                return "Only active player declares Press.";

            var source = Match.GetCard(action.CardInstanceId ?? -1);
            var target = Match.GetCard(action.TargetInstanceId ?? -1);
            if (source == null || target == null)
                return "Invalid Press target.";
            if (source.ControllerId != player.Id || source.Zone != Zone.Field)
                return "Invalid attacker.";
            if (!source.Ready || source.Exhausted)
                return "Body not Ready.";
            if (!Match.BodiesAwaitingDeclare.Contains(source.InstanceId))
                return "Already declared.";

            ApplyJarJarFirstPressGrant(source);

            if (source.HasKeyword(Keyword.Bazerk))
            {
                CreatePressStack(player, source, target, wave: 0, withAggression: true);
                CreatePressStack(player, source, target, wave: 1, withAggression: false);
            }
            else
            {
                var aggression = source.HasKeyword(Keyword.Aggression);
                CreatePressStack(player, source, target, wave: 0, withAggression: aggression);
            }

            source.Exhausted = true;
            source.Ready = false;
            Emit(EventKind.Exhausted, "instanceId", source.InstanceId);

            if (source.HasKeyword(Keyword.Still))
                Emit(EventKind.HoldDeclared, "source", source.InstanceId);

            if (action.DoubleteamHelperId.HasValue)
                ApplyDoubleteam(source, Match.GetCard(action.DoubleteamHelperId.Value));

            Match.BodiesAwaitingDeclare.Remove(source.InstanceId);
            Match.PressesDeclaredThisClash++;

            if (Match.BodiesAwaitingDeclare.Count == 0)
            {
                Match.ClashPhase = ClashPhase.C2_Answers;
                Match.AnswerOfferIndex = 0;
            }

            return null;
        }

        void ApplyJarJarFirstPressGrant(CardInstance source)
        {
            if (source.Printing.Id != "SITH-001")
                return;
            if (Match.FirstPressAggressionGranted)
                return;
            Match.FirstPressAggressionGranted = true;
            source.FlagsThisClash.Add("FirstPressAggression");
            source.KeywordsNow.Add(Keyword.Aggression);
        }

        void CreatePressStack(Player player, CardInstance source, CardInstance target, int wave, bool withAggression)
        {
            var stackObj = new StackObject
            {
                StackId = Match.NextStack(),
                Timestamp = Match.NextTs(),
                Type = StackObjectType.Press,
                ControllerId = player.Id,
                SourceInstanceId = source.InstanceId,
                Wave = wave,
                StrikeSnapshot = source.Strike + (Match.DoubleteamStrikeBonus.TryGetValue(source.InstanceId, out var s) ? s : 0),
            };
            stackObj.Targets.Add(target.InstanceId);
            if (withAggression || (wave == 0 && source.HasKeyword(Keyword.Aggression)))
                stackObj.Keywords.Add(Keyword.Aggression);
            if (source.HasKeyword(Keyword.Absolute))
                stackObj.Keywords.Add(Keyword.Absolute);
            if (source.HasKeyword(Keyword.Gashing))
                stackObj.Keywords.Add(Keyword.Gashing);
            if (source.HasKeyword(Keyword.HeavyHitter))
                stackObj.Keywords.Add(Keyword.HeavyHitter);
            if (source.HasKeyword(Keyword.Drain))
                stackObj.Keywords.Add(Keyword.Drain);
            if (source.FlagsThisClash.Contains("FirstPressAggression"))
                stackObj.Keywords.Add(Keyword.Aggression);

            Match.Stack.Add(stackObj);
            Emit(EventKind.StackPushed, "stackId", stackObj.StackId, "type", StackObjectType.Press.ToString());
            Emit(EventKind.PressDeclared, "stackId", stackObj.StackId, "source", source.InstanceId, "target", target.InstanceId, "wave", wave);
        }

        void ApplyDoubleteam(CardInstance source, CardInstance helper)
        {
            if (helper == null || helper.ControllerId != source.ControllerId || helper.Zone != Zone.Field)
                return;
            if (helper.Printing.Type != CardType.Companion)
                return;
            Match.DoubleteamStrikeBonus[source.InstanceId] = helper.Strike;
        }

        string DeclareHold(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.Clash || Match.ClashPhase != ClashPhase.C1_ActiveDeclare)
                return "Not in active declare.";
            if (player.Id != Match.ActivePlayerId)
                return "Only active player declares Hold.";

            var source = Match.GetCard(action.CardInstanceId ?? -1);
            if (source == null || source.ControllerId != player.Id)
                return "Invalid Hold.";
            if (!Match.BodiesAwaitingDeclare.Contains(source.InstanceId))
                return "Already declared.";

            source.Exhausted = true;
            source.Ready = false;
            Match.BodiesAwaitingDeclare.Remove(source.InstanceId);
            Emit(EventKind.HoldDeclared, "source", source.InstanceId);
            Emit(EventKind.Exhausted, "instanceId", source.InstanceId);

            if (Match.BodiesAwaitingDeclare.Count == 0)
            {
                Match.ClashPhase = ClashPhase.C2_Answers;
                Match.AnswerOfferIndex = 0;
            }

            return null;
        }

        string AnswerPress(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.Clash || Match.ClashPhase != ClashPhase.C2_Answers)
                return "Not in answer window.";

            var source = Match.GetCard(action.CardInstanceId ?? -1);
            var target = Match.GetCard(action.TargetInstanceId ?? -1);
            if (source == null || target == null)
                return "Invalid Answer.";
            if (source.ControllerId != player.Id || !source.Ready || source.Exhausted)
                return "Answer illegal.";

            var stackObj = new StackObject
            {
                StackId = Match.NextStack(),
                Timestamp = Match.NextTs(),
                Type = StackObjectType.Press,
                ControllerId = player.Id,
                SourceInstanceId = source.InstanceId,
                Wave = 0,
                StrikeSnapshot = source.Strike,
                AnsweredToStackId = action.StackObjectId,
            };
            stackObj.Targets.Add(target.InstanceId);
            if (source.HasKeyword(Keyword.Aggression))
                stackObj.Keywords.Add(Keyword.Aggression);
            if (source.HasKeyword(Keyword.Absolute))
                stackObj.Keywords.Add(Keyword.Absolute);
            if (source.HasKeyword(Keyword.Gashing))
                stackObj.Keywords.Add(Keyword.Gashing);
            Match.Stack.Add(stackObj);
            source.Exhausted = true;
            Emit(EventKind.PressAnswered, "stackId", stackObj.StackId, "source", source.InstanceId, "target", target.InstanceId);
            return null;
        }

        string PlayCard(Player player, PlayerAction action)
        {
            var card = Match.GetCard(action.CardInstanceId ?? -1);
            if (card == null || !player.Hand.Contains(card))
                return "Card not in hand.";

            if (card.Printing.Type == CardType.WillSite)
            {
                if (Match.Phase != Phase.Site || player.Id != Match.ActivePlayerId)
                    return "Will Site only on your Site step.";
                if (player.SitesPlayedThisTurn >= 1)
                    return "One Will Site per turn."; // 4.10.25 / test 25
            }
            else if (card.Printing.Type == CardType.Algorithm)
            {
                if (player.Id != Match.ActivePlayerId)
                    return "Then illegal on opponent turn."; // test 28
                if (card.Printing.Abilities.Count > 0 && card.Printing.Abilities[0].Timing == Timing.Then && player.Id != Match.ActivePlayerId)
                    return "Then illegal.";
            }
            else if (card.Printing.Type == CardType.Surge || HasTiming(card, Timing.Now))
            {
                // Now legal anytime with priority — test 29
            }
            else if (player.Id != Match.ActivePlayerId || Match.Phase != Phase.Main)
            {
                return "Cannot play that card now.";
            }

            if (player.Will < card.Printing.WillCost)
                return "Insufficient Will.";

            player.Will -= card.Printing.WillCost;

            var stackObj = new StackObject
            {
                StackId = Match.NextStack(),
                Timestamp = Match.NextTs(),
                Type = StackObjectType.PlayCard,
                ControllerId = player.Id,
                SourceInstanceId = card.InstanceId,
                PrintingId = card.Printing.Id,
                PaidWill = card.Printing.WillCost,
            };
            Match.Stack.Add(stackObj);
            Emit(EventKind.StackPushed, "stackId", stackObj.StackId, "type", StackObjectType.PlayCard.ToString());
            Match.Passed.Clear();
            GivePriorityToNextLiving();
            return null;
        }

        static bool HasTiming(CardInstance card, Timing timing)
        {
            for (var i = 0; i < card.Printing.Abilities.Count; i++)
            {
                if (card.Printing.Abilities[i].Timing == timing)
                    return true;
            }

            return false;
        }

        string StoreAction(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.Main || player.Id != Match.ActivePlayerId)
                return "Store only on your Main.";
            if (player.StoreActionsThisTurn >= 1)
                return "One Store action per turn."; // test 26

            var kind = action.StoreKind ?? MapStoreKind(action.Kind);
            var paidWorth = action.PaidWorth;

            if (kind == StoreActionKind.Sell)
            {
                if (!action.HandCardInstanceId.HasValue)
                    return "No card selected to sell.";
                if (player.BoughtThisTurn.Contains(action.HandCardInstanceId.Value))
                    return "Cannot sell a card you just bought this turn.";
            }

            if (kind == StoreActionKind.Row && player.Worth < 2)
                return "Row costs 2 Worth.";

            var rowCost = kind == StoreActionKind.Row ? 2 : 0;
            if (rowCost > 0 && player.Worth < rowCost)
                return "Row costs 2 Worth.";

            if (rowCost > 0)
            {
                player.Worth -= rowCost;
                paidWorth = rowCost;
            }

            var stackObj = new StackObject
            {
                StackId = Match.NextStack(),
                Timestamp = Match.NextTs(),
                Type = StackObjectType.StoreAction,
                ControllerId = player.Id,
                StoreKind = kind,
                StoreSlotIndex = action.StoreSlotIndex,
                HandCardInstanceId = action.HandCardInstanceId,
                PaidWorth = paidWorth,
            };
            Match.Stack.Add(stackObj);
            player.StoreActionsThisTurn++;
            Match.Passed.Clear();
            GivePriorityToNextLiving();
            Emit(EventKind.StackPushed, "stackId", stackObj.StackId, "type", StackObjectType.StoreAction.ToString());
            return null;
        }

        static StoreActionKind MapStoreKind(PlayerActionKind kind)
        {
            switch (kind)
            {
                case PlayerActionKind.StoreBuy: return StoreActionKind.Buy;
                case PlayerActionKind.StoreSell: return StoreActionKind.Sell;
                case PlayerActionKind.StoreTrade: return StoreActionKind.Trade;
                case PlayerActionKind.StoreKeep: return StoreActionKind.Keep;
                case PlayerActionKind.StoreList: return StoreActionKind.List;
                case PlayerActionKind.StoreRow: return StoreActionKind.Row;
                default: return StoreActionKind.Keep;
            }
        }

        string Silence(Player player, PlayerAction action)
        {
            var targetStack = FindStack(action.StackObjectId ?? -1);
            if (targetStack == null)
                return "No stack object.";

            // test 10/11 — Silence refunds Will/Worth, not Press exhaust
            RefundSilenced(targetStack);
            targetStack.Fizzled = true;
            Match.Stack.Remove(targetStack);
            Emit(EventKind.Silenced, "stackId", targetStack.StackId);
            Emit(EventKind.StackPopped, "stackId", targetStack.StackId, "fizzled", true);
            Match.Passed.Clear();
            Match.PriorityPlayerId = Match.ActivePlayerId;
            return null;
        }

        void RefundSilenced(StackObject obj)
        {
            var controller = Match.GetPlayer(obj.ControllerId);
            if (controller == null)
                return;
            controller.Will += obj.PaidWill;
            controller.Worth += obj.PaidWorth;
            if (obj.PaidWill > 0)
                Emit(EventKind.WillSet, "player", controller.Id, "amount", controller.Will);
            if (obj.PaidWorth > 0)
                Emit(EventKind.WorthChanged, "player", controller.Id, "delta", obj.PaidWorth);
        }

        StackObject FindStack(int stackId)
        {
            for (var i = Match.Stack.Count - 1; i >= 0; i--)
            {
                if (Match.Stack[i].StackId == stackId)
                    return Match.Stack[i];
            }

            return null;
        }

        void ResolveTopStack()
        {
            if (Match.Stack.Count == 0)
                return;

            var obj = Match.Stack[Match.Stack.Count - 1];
            Match.Stack.RemoveAt(Match.Stack.Count - 1);

            if (obj.Fizzled)
            {
                Emit(EventKind.StackPopped, "stackId", obj.StackId, "fizzled", true);
                return;
            }

            switch (obj.Type)
            {
                case StackObjectType.PlayCard:
                    ResolvePlayCard(obj);
                    break;
                case StackObjectType.StoreAction:
                    ResolveStore(obj);
                    break;
                case StackObjectType.Press:
                    if (Match.Phase == Phase.Clash && !Match.ClashLocked)
                    {
                        Match.Stack.Add(obj);
                        LockClashAndResolve();
                        return;
                    }
                    break;
                case StackObjectType.Silence:
                    break;
            }

            Emit(EventKind.StackPopped, "stackId", obj.StackId, "fizzled", false);
        }

        void ResolvePlayCard(StackObject obj)
        {
            var player = Match.GetPlayer(obj.ControllerId);
            if (player == null)
                return;

            CardInstance card = null;
            for (var i = 0; i < player.Hand.Count; i++)
            {
                if (player.Hand[i].InstanceId == obj.SourceInstanceId)
                {
                    card = player.Hand[i];
                    break;
                }
            }

            card ??= Match.GetCard(obj.SourceInstanceId ?? -1);
            if (card == null)
                return;

            player.Hand.Remove(card);

            if (card.Printing.Type == CardType.WillSite)
            {
                player.Willwell.Add(card);
                card.Zone = Zone.Willwell;
                player.SitesPlayedThisTurn++;
            }
            else if (card.Printing.Type is CardType.Companion or CardType.Relic or CardType.Bond or CardType.Icon)
            {
                player.Field.Add(card);
                card.Zone = Zone.Field;
                card.Ready = true;
                card.Exhausted = false;
                Emit(EventKind.PermanentEntered, "instanceId", card.InstanceId, "zone", Zone.Field.ToString());
            }
            else
            {
                Match.Removed.Add(card);
                card.Zone = Zone.Removed;
            }
        }

        void ResolveStore(StackObject obj)
        {
            var player = Match.GetPlayer(obj.ControllerId);
            switch (obj.StoreKind)
            {
                case StoreActionKind.Buy:
                    if (obj.StoreSlotIndex.HasValue && obj.StoreSlotIndex.Value >= 0 && obj.StoreSlotIndex.Value < Match.Store.Length)
                    {
                        var card = Match.Store[obj.StoreSlotIndex.Value];
                        if (card != null)
                        {
                            player.Worth -= card.Printing.StoreWorth;
                            Match.Store[obj.StoreSlotIndex.Value] = null;
                            card.Zone = Zone.Hand;
                            card.ControllerId = player.Id;
                            player.Hand.Add(card);
                            player.BoughtThisTurn.Add(card.InstanceId);
                            Emit(EventKind.WorthChanged, "player", player.Id, "delta", -card.Printing.StoreWorth);
                        }
                    }
                    break;
                case StoreActionKind.Sell:
                    if (obj.HandCardInstanceId.HasValue)
                    {
                        CardInstance card = null;
                        for (var i = 0; i < player.Hand.Count; i++)
                        {
                            if (player.Hand[i].InstanceId == obj.HandCardInstanceId.Value)
                            {
                                card = player.Hand[i];
                                break;
                            }
                        }

                        if (card != null)
                        {
                            player.Hand.Remove(card);
                            player.Worth += 1;

                            var placed = false;
                            for (var i = 0; i < Match.Store.Length; i++)
                            {
                                if (Match.Store[i] == null)
                                {
                                    Match.Store[i] = card;
                                    card.Zone = Zone.Store;
                                    card.ControllerId = -1;
                                    placed = true;
                                    break;
                                }
                            }

                            if (!placed)
                            {
                                card.Zone = Zone.Supply;
                                Match.Supply.Add(card);
                            }

                            Emit(EventKind.WorthChanged, "player", player.Id, "delta", 1);
                        }
                    }
                    break;
                case StoreActionKind.Row:
                    for (var i = 0; i < Match.Store.Length; i++)
                    {
                        if (Match.Store[i] != null)
                        {
                            Match.Supply.Add(Match.Store[i]);
                            Match.Store[i].Zone = Zone.Supply;
                            Match.Store[i] = null;
                        }
                    }
                    MatchSetup.RefillStore(Match, rng);
                    break;
                case StoreActionKind.Keep:
                    break;
            }
        }

        void LockClashAndResolve()
        {
            if (Match.ClashLocked)
                return;

            for (var i = Match.Stack.Count - 1; i >= 0; i--)
            {
                if (Match.Stack[i].Type != StackObjectType.Press)
                    return;
            }

            Match.ClashLocked = true;
            Match.ClashPhase = ClashPhase.C3_Interact;
            while (Match.Stack.Count > 0)
            {
                var press = Match.Stack[Match.Stack.Count - 1];
                Match.Stack.RemoveAt(Match.Stack.Count - 1);
                Match.ClashQueue.Add(ToQueuedPress(press));
            }

            Emit(EventKind.ClashLocked, "pressIds", Match.ClashQueue.Count);
            ResolveAggressionWave();
            SkipRemovedPresses();
            ResolveNormalWave();
            ResolveAftereffects();
            Match.ClashPhase = ClashPhase.C8_End;
            Match.Phase = Phase.End;
            Match.PriorityPlayerId = Match.ActivePlayerId;
        }

        QueuedPress ToQueuedPress(StackObject obj)
        {
            return new QueuedPress
            {
                StackId = obj.StackId,
                Timestamp = obj.Timestamp,
                ControllerId = obj.ControllerId,
                SourceInstanceId = obj.SourceInstanceId ?? -1,
                TargetInstanceId = obj.Targets.Count > 0 ? obj.Targets[0] : -1,
                Wave = obj.Wave,
                StrikeSnapshot = obj.StrikeSnapshot,
                Keywords = new HashSet<Keyword>(obj.Keywords),
            };
        }

        void ResolveAggressionWave()
        {
            Match.ClashPhase = ClashPhase.C4_AggressionWave;
            for (var i = 0; i < Match.ClashQueue.Count; i++)
            {
                var press = Match.ClashQueue[i];
                if (press.Skipped)
                    continue;
                if (!press.Keywords.Contains(Keyword.Aggression) && press.Wave != 0)
                    continue;
                if (press.Wave == 1 && !Match.GetCard(press.SourceInstanceId).HasKeyword(Keyword.Bazerk))
                    continue;
                DealPressDamage(press);
            }
        }

        void SkipRemovedPresses()
        {
            Match.ClashPhase = ClashPhase.C5_Skip;
            for (var i = 0; i < Match.ClashQueue.Count; i++)
            {
                var press = Match.ClashQueue[i];
                var source = Match.GetCard(press.SourceInstanceId);
                var target = Match.GetCard(press.TargetInstanceId);
                if (source == null || source.Zone != Zone.Field || target == null || target.Zone != Zone.Field)
                    press.Skipped = true;
                if (target != null && target.CurrentHealth <= 0)
                    press.Skipped = true;
            }
        }

        void ResolveNormalWave()
        {
            Match.ClashPhase = ClashPhase.C6_NormalWave;
            for (var i = 0; i < Match.ClashQueue.Count; i++)
            {
                var press = Match.ClashQueue[i];
                if (press.Skipped)
                    continue;
                if (press.Keywords.Contains(Keyword.Aggression) && press.Wave == 0)
                    continue;
                DealPressDamage(press);
            }
        }

        void DealPressDamage(QueuedPress press)
        {
            var source = Match.GetCard(press.SourceInstanceId);
            var target = Match.GetCard(press.TargetInstanceId);
            if (source == null || target == null)
                return;

            var strike = press.StrikeSnapshot > 0 ? press.StrikeSnapshot : source.Strike;
            var guard = DamageMath.EffectiveGuard(target, Match);
            var absolute = press.Keywords.Contains(Keyword.Absolute);
            var damage = DamageMath.ComputeDamage(strike, guard, absolute);
            if (damage <= 0)
                return;

            var before = target.CurrentHealth;
            target.DamageMarked += damage;
            press.DamageDealt = damage;
            Emit(EventKind.DamageDealt, "source", source.InstanceId, "target", target.InstanceId, "amount", damage, "wave", press.Wave, "absolute", absolute);

            if (press.Keywords.Contains(Keyword.Gashing) && target.Printing.Type == CardType.Companion && damage > 0)
            {
                target.DamageMarked = target.Health;
            }

            if (target.CurrentHealth <= 0)
                press.RemovedTarget = true;

            if (source.Printing.Id == "SITH-001" && damage > 0 && source.FlagsThisClash.Contains("FirstPressAggression"))
            {
                source.PutCounter("anger", 1);
                Emit(EventKind.CounterPut, "instanceId", source.InstanceId, "name", "anger", "amount", 1);
            }

            if (press.Keywords.Contains(Keyword.Drain))
            {
                var icon = Match.GetPlayer(source.ControllerId)?.Icon;
                if (icon != null)
                {
                    icon.DamageMarked = Math.Max(0, icon.DamageMarked - damage);
                    Emit(EventKind.HealthChanged, "instanceId", icon.InstanceId, "current", icon.CurrentHealth, "printed", icon.Health);
                }
            }

            RunChecks();
        }

        void ResolveAftereffects()
        {
            Match.ClashPhase = ClashPhase.C7_Aftereffects;
            for (var i = 0; i < Match.ClashQueue.Count; i++)
            {
                var press = Match.ClashQueue[i];
                if (!press.Keywords.Contains(Keyword.HeavyHitter) || !press.RemovedTarget)
                    continue;
                var target = Match.GetCard(press.TargetInstanceId);
                if (target == null || target.Printing.Type != CardType.Companion)
                    continue;
                var leftover = press.DamageDealt - target.Health;
                if (leftover <= 0)
                    continue;
                var owner = Match.GetPlayer(target.ControllerId);
                if (owner?.Icon == null)
                    continue;
                var icon = owner.Icon;
                var iconDamage = DamageMath.ComputeDamage(leftover, icon.Guard, false);
                icon.DamageMarked += iconDamage;
                Emit(EventKind.DamageDealt, "source", press.SourceInstanceId, "target", icon.InstanceId, "amount", iconDamage, "wave", -1, "absolute", false);
            }

            RunChecks();
        }

        void AdvanceClashPhase()
        {
            if (Match.ClashPhase == ClashPhase.C1_ActiveDeclare && Match.BodiesAwaitingDeclare.Count == 0)
            {
                Match.ClashPhase = ClashPhase.C2_Answers;
                return;
            }

            if (Match.ClashPhase == ClashPhase.C2_Answers)
            {
                Match.ClashPhase = ClashPhase.C3_Interact;
                Match.PriorityPlayerId = Match.ActivePlayerId;
                if (Match.Stack.Count > 0)
                    LockClashAndResolve();
                else
                    Match.Phase = Phase.End;
                return;
            }

            if (Match.ClashPhase == ClashPhase.C8_End || Match.ClashPhase == ClashPhase.C7_Aftereffects)
            {
                Match.Phase = Phase.End;
                Match.ClashPhase = ClashPhase.None;
            }
        }

        void RunChecks()
        {
            var events = StateChecks.Run(Match);
            pendingEvents.AddRange(events);
            Match.EventLog.AddRange(events);
        }

        void Emit(EventKind kind, params object[] keyValuePairs)
        {
            var data = new Dictionary<string, object>();
            for (var i = 0; i + 1 < keyValuePairs.Length; i += 2)
                data[keyValuePairs[i].ToString()] = keyValuePairs[i + 1];
            var ev = GameEvent.Create(kind, Match.NextTs(), data);
            pendingEvents.Add(ev);
            Match.EventLog.Add(ev);
            view?.OnEvent(ev);
        }

        void FlushEvents()
        {
            for (var i = 0; i < pendingEvents.Count; i++)
                view?.OnEvent(pendingEvents[i]);
        }

        // Test helpers
        public void ForceStartStep() => MatchSetup.BeginTurn(Match, rng);

        public void SetRound(int round) => Match.Round = round;

        public void SetPlayerWill(int playerId, int will)
        {
            Match.GetPlayer(playerId).Will = will;
        }

        public void AdvanceToMain()
        {
            Match.Phase = Phase.Main;
            Match.PriorityPlayerId = Match.ActivePlayerId;
        }

        public void AdvanceToClash() => BeginClash();

        public void FinishClashPipeline()
        {
            Match.ClashPhase = ClashPhase.C2_Answers;
            LockClashAndResolve();
        }
    }
}
