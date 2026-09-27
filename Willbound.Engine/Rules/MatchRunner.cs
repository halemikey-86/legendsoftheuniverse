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
        internal ICardDatabase Database => database;

        public MatchRunner(Match match, ICardDatabase database, IRng rng, IMatchView view = null)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            this.view = view;
        }

        public static MatchRunner FromSetup(
            IEnumerable<CardPrinting> printings,
            IList<SetupPlayer> players,
            int seed = 1,
            int? firstActive = 0,
            bool randomizeSeatOrder = false,
            bool enablePregameFlow = false)
        {
            var db = new InMemoryCardDatabase(printings);
            var rng = new SeededRng(seed);
            var match = MatchSetup.Create(db, rng, players, firstActive, randomizeSeatOrder, enablePregameFlow);
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

        /// <summary>True when this player currently has priority and can legally play the card from hand.</summary>
        public bool CanPlay(int playerId, int cardInstanceId)
        {
            if (Match.WinnerId.HasValue)
                return false;

            var player = Match.GetPlayer(playerId);
            if (player == null || player.Lost)
                return false;

            if (playerId != Match.PriorityPlayerId)
                return false;

            return ValidatePlayCard(player, cardInstanceId) == null;
        }

        Match CloneMatchForFail() => Match;

        string ValidateAndApply(PlayerAction action)
        {
            if (Match.WinnerId.HasValue)
                return "Match is over.";

            var player = Match.GetPlayer(action.PlayerId);
            if (player == null || player.Lost)
                return "Invalid player.";

            if (IsPregameAction(action.Kind))
                return ApplyPregameAction(player, action);

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
                case PlayerActionKind.Activate:
                    return Activate(player, action);
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

        // Pregame flow (Phase.LegendaryDraft / Phase.Mulligan). Each seated player resolves their own
        // step independently of Match.PriorityPlayerId — these aren't Stack actions, there's no priority
        // window before the match has started.
        static bool IsPregameAction(PlayerActionKind kind) =>
            kind == PlayerActionKind.PickLegendaryIcon || kind == PlayerActionKind.CycleLegendaryIcon
            || kind == PlayerActionKind.KeepHand || kind == PlayerActionKind.CycleHandCard || kind == PlayerActionKind.Mulligan;

        string ApplyPregameAction(Player player, PlayerAction action)
        {
            switch (action.Kind)
            {
                case PlayerActionKind.PickLegendaryIcon: return PickLegendaryIcon(player, action);
                case PlayerActionKind.CycleLegendaryIcon: return CycleLegendaryIcon(player);
                case PlayerActionKind.KeepHand: return KeepHand(player);
                case PlayerActionKind.CycleHandCard: return CycleHandCard(player, action);
                case PlayerActionKind.Mulligan: return MulliganHand(player);
                default: return "Unsupported pregame action.";
            }
        }

        string PickLegendaryIcon(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.LegendaryDraft)
                return "Not in Legendary Icon draft.";
            if (player.LegendaryDraftDone)
                return "Already drafted.";

            CardInstance chosen = null;
            for (var i = 0; i < player.LegendaryChoices.Count; i++)
            {
                if (player.LegendaryChoices[i].InstanceId == action.CardInstanceId)
                {
                    chosen = player.LegendaryChoices[i];
                    break;
                }
            }

            if (chosen == null)
                return "Not one of your offered Legendary Icons.";

            chosen.Zone = Zone.Field;
            chosen.ControllerId = player.Id;
            chosen.Ready = true;
            chosen.Exhausted = false;
            player.Icon = chosen;
            player.Field.Add(chosen);

            for (var i = 0; i < player.LegendaryChoices.Count; i++)
            {
                var declined = player.LegendaryChoices[i];
                if (declined.InstanceId == chosen.InstanceId)
                    continue;
                declined.Zone = Zone.Supply;
                declined.ControllerId = -1;
                Match.Supply.Add(declined);
            }

            player.LegendaryChoices.Clear();
            player.LegendaryDraftDone = true;
            Emit(EventKind.LegendaryIconPicked, "player", player.Id, "instanceId", chosen.InstanceId);

            MatchSetup.AdvancePregameIfReady(Match, rng);
            return null;
        }

        string CycleLegendaryIcon(Player player)
        {
            if (Match.Phase != Phase.LegendaryDraft)
                return "Not in Legendary Icon draft.";
            if (player.LegendaryDraftDone)
                return "Already drafted.";
            if (player.LegendaryCycleUsed)
                return "Legendary Icon cycle already used.";

            player.LegendaryChoices.Clear();
            player.LegendaryChoices = MatchSetup.RollLegendaryChoices(Match, database, rng, player.Id);
            player.LegendaryCycleUsed = true;
            player.SkipFirstWill = true;
            Emit(EventKind.LegendaryIconOffered, "player", player.Id);
            return null;
        }

        string KeepHand(Player player)
        {
            if (Match.Phase != Phase.Mulligan)
                return "Not in the Mulligan step.";
            if (player.MulliganStepDone)
                return "Already resolved your opening hand.";

            player.MulliganStepDone = true;
            Emit(EventKind.HandKept, "player", player.Id);
            MatchSetup.AdvancePregameIfReady(Match, rng);
            return null;
        }

        string CycleHandCard(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.Mulligan)
                return "Not in the Mulligan step.";
            if (player.MulliganStepDone)
                return "Already resolved your opening hand.";

            var card = Match.GetCard(action.CardInstanceId ?? -1);
            if (card == null || !player.Hand.Contains(card))
                return "Card not in hand.";
            if (player.Deck.Count == 0)
                return "Deck is empty.";

            player.Hand.Remove(card);
            card.Zone = Zone.Deck;
            player.Deck.Add(card);

            var replacement = player.Deck[0];
            player.Deck.RemoveAt(0);
            replacement.Zone = Zone.Hand;
            player.Hand.Add(replacement);

            player.MulliganStepDone = true;
            Emit(EventKind.HandCycled, "player", player.Id, "instanceId", card.InstanceId);
            MatchSetup.AdvancePregameIfReady(Match, rng);
            return null;
        }

        string MulliganHand(Player player)
        {
            if (Match.Phase != Phase.Mulligan)
                return "Not in the Mulligan step.";
            if (player.MulliganStepDone)
                return "Already resolved your opening hand.";

            for (var i = 0; i < player.Hand.Count; i++)
            {
                player.Hand[i].Zone = Zone.Deck;
                player.Deck.Add(player.Hand[i]);
            }

            player.Hand.Clear();
            rng.Shuffle(player.Deck);

            for (var h = 0; h < MatchConstants.MulliganHandSize && player.Deck.Count > 0; h++)
            {
                var card = player.Deck[0];
                player.Deck.RemoveAt(0);
                card.Zone = Zone.Hand;
                player.Hand.Add(card);
            }

            player.MulliganStepDone = true;
            Emit(EventKind.HandMulliganed, "player", player.Id);
            MatchSetup.AdvancePregameIfReady(Match, rng);
            return null;
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
            Match.StrikeBonusThisClash.Clear();
            Match.GuardBonusThisClash.Clear();
            Match.BodiesAwaitingDeclare.Clear();

            FireHookOnAllFieldCards(EffectWhen.OnClashBegin);

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

        /// <summary>OnClashBegin/OnClashEnd (C0/C8) fire on every permanent on every Field, not
        /// just the active player's — the step boundary is global.</summary>
        void FireHookOnAllFieldCards(EffectWhen hook)
        {
            for (var p = 0; p < Match.Players.Count; p++)
            {
                var owner = Match.Players[p];
                for (var i = 0; i < owner.Field.Count; i++)
                    EffectInterpreter.FireOwnHook(this, owner.Field[i], hook, new EffectContext(owner.Field[i], owner));
            }
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

            var targetError = ValidateTargetingKeywords(player, target);
            if (targetError != null)
                return targetError;

            ApplyGrantKeywordOnDeclareEffects(source);

            // Doubleteam must land before CreatePressStack snapshots Strike/Guard, or the bonus
            // it just set would never be read.
            if (action.DoubleteamHelperId.HasValue)
                ApplyDoubleteam(source, Match.GetCard(action.DoubleteamHelperId.Value));

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

            Match.BodiesAwaitingDeclare.Remove(source.InstanceId);
            Match.PressesDeclaredThisClash++;

            if (Match.BodiesAwaitingDeclare.Count == 0)
            {
                Match.ClashPhase = ClashPhase.C2_Answers;
                Match.AnswerOfferIndex = 0;
            }

            return null;
        }

        /// <summary>4.6.36/37 — Closed blocks an opponent from choosing this as a Press/Answer
        /// target at all; Toll charges the announcer N extra Will now or the target is illegal.</summary>
        string ValidateTargetingKeywords(Player announcer, CardInstance target)
        {
            if (target.ControllerId != announcer.Id && target.HasKeyword(Keyword.Closed))
                return "Target is Closed.";

            var toll = target.Toll;
            if (toll > 0)
            {
                if (announcer.Will < toll)
                    return "Toll unpaid: insufficient Will.";
                announcer.Will -= toll;
                Emit(EventKind.WillPaid, "player", announcer.Id, "amount", toll);
            }

            return null;
        }

        /// <summary>4.8.50 — GrantKeywordOnDeclare is evaluated against the declaring body's own
        /// abilities every declare, for any card (not gated by printing id). Only "no Press
        /// declared yet this Clash" is a supported filter today — see EffectInterpreter.EvaluateDeclareFilter.</summary>
        void ApplyGrantKeywordOnDeclareEffects(CardInstance source)
        {
            for (var a = 0; a < source.Printing.Abilities.Count; a++)
            {
                var ability = source.Printing.Abilities[a];
                for (var e = 0; e < ability.Effects.Count; e++)
                {
                    var effect = ability.Effects[e];
                    if (effect.Op != EffectOp.GrantKeywordOnDeclare)
                        continue;
                    if (!EffectInterpreter.EvaluateDeclareFilter(effect.Filter, Match))
                        continue;
                    if (string.IsNullOrEmpty(effect.Keyword) || !Enum.TryParse<Keyword>(effect.Keyword, true, out var keyword))
                        continue;
                    source.KeywordsNow.Add(keyword);
                    Emit(EventKind.KeywordGranted, "instanceId", source.InstanceId, "keyword", keyword.ToString(), "duration", "ThisClash");
                }
            }
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
                StrikeSnapshot = source.Strike + (Match.StrikeBonusThisClash.TryGetValue(source.InstanceId, out var s) ? s : 0),
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

            Match.Stack.Add(stackObj);
            Emit(EventKind.StackPushed, "stackId", stackObj.StackId, "type", StackObjectType.Press.ToString());
            Emit(EventKind.PressDeclared, "stackId", stackObj.StackId, "source", source.InstanceId, "target", target.InstanceId, "wave", wave);
            EffectInterpreter.FireOwnHook(this, source, EffectWhen.OnPressDeclared, new EffectContext(source, player, target));
        }

        void ApplyDoubleteam(CardInstance source, CardInstance helper)
        {
            if (helper == null || helper.ControllerId != source.ControllerId || helper.Zone != Zone.Field)
                return;
            if (helper.Printing.Type != CardType.Companion)
                return;
            // 4.8.48 — the helper's Strike boosts this Press now; its Guard boosts the same
            // pressing body (`source`) if it's later Pressed itself this Clash.
            Match.StrikeBonusThisClash[source.InstanceId] = helper.Strike;
            Match.GuardBonusThisClash[source.InstanceId] = helper.Guard;
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
            EffectInterpreter.FireOwnHook(this, source, EffectWhen.OnHold, new EffectContext(source, player));

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

            var targetError = ValidateTargetingKeywords(player, target);
            if (targetError != null)
                return targetError;

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
            EffectInterpreter.FireOwnHook(this, source, EffectWhen.OnAnswer, new EffectContext(source, player, target));
            return null;
        }

        string PlayCard(Player player, PlayerAction action)
        {
            var error = ValidatePlayCard(player, action.CardInstanceId ?? -1);
            if (error != null)
                return error;

            var card = Match.GetCard(action.CardInstanceId ?? -1);
            player.Will -= card.Printing.WillCost;
            if (card.Printing.WillCost > 0)
                Emit(EventKind.WillPaid, "player", player.Id, "amount", card.Printing.WillCost);

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
            if (action.TargetInstanceId.HasValue)
                stackObj.Targets.Add(action.TargetInstanceId.Value);
            Match.Stack.Add(stackObj);
            Emit(EventKind.StackPushed, "stackId", stackObj.StackId, "type", StackObjectType.PlayCard.ToString());
            Match.Passed.Clear();
            GivePriorityToNextLiving();
            return null;
        }

        string ValidatePlayCard(Player player, int cardInstanceId)
        {
            var card = Match.GetCard(cardInstanceId);
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

        /// <summary>7.2's PlayerAction.Activate — pay costWill, put the ability on the Stack.
        /// Its effects[] run when it resolves (ResolveActivate), same as any other ability.</summary>
        string Activate(Player player, PlayerAction action)
        {
            var source = Match.GetCard(action.CardInstanceId ?? -1);
            if (source == null || source.ControllerId != player.Id || source.Zone is not (Zone.Field or Zone.Willwell))
                return "Invalid activation source.";

            var index = action.AbilityIndex ?? -1;
            if (index < 0 || index >= source.Printing.Abilities.Count)
                return "Invalid ability index.";

            var ability = source.Printing.Abilities[index];
            if (ability.Timing != Timing.Activated)
                return "That ability is not Activated.";

            var onceKey = $"{source.InstanceId}:{index}";
            if (ability.OncePerTurn && source.FlagsThisTurn.Contains("Activated:" + onceKey))
                return "Already activated this turn.";
            if (ability.OncePerClash && source.FlagsThisClash.Contains("Activated:" + onceKey))
                return "Already activated this Clash.";
            if (ability.OncePerGame && player.OncePerGameFlags.Contains(onceKey))
                return "Already activated this game.";

            if (player.Will < ability.CostWill)
                return "Insufficient Will.";
            player.Will -= ability.CostWill;
            if (ability.CostWill > 0)
                Emit(EventKind.WillPaid, "player", player.Id, "amount", ability.CostWill);

            if (ability.OncePerTurn)
                source.FlagsThisTurn.Add("Activated:" + onceKey);
            if (ability.OncePerClash)
                source.FlagsThisClash.Add("Activated:" + onceKey);
            if (ability.OncePerGame)
                player.OncePerGameFlags.Add(onceKey);

            var stackObj = new StackObject
            {
                StackId = Match.NextStack(),
                Timestamp = Match.NextTs(),
                Type = StackObjectType.Activate,
                ControllerId = player.Id,
                SourceInstanceId = source.InstanceId,
                AbilityIndex = index,
                PaidWill = ability.CostWill,
            };
            if (action.TargetInstanceId.HasValue)
                stackObj.Targets.Add(action.TargetInstanceId.Value);
            Match.Stack.Add(stackObj);
            Emit(EventKind.StackPushed, "stackId", stackObj.StackId, "type", StackObjectType.Activate.ToString());
            Match.Passed.Clear();
            GivePriorityToNextLiving();
            return null;
        }

        void ResolveActivate(StackObject obj)
        {
            var player = Match.GetPlayer(obj.ControllerId);
            var source = Match.GetCard(obj.SourceInstanceId ?? -1);
            if (player == null || source == null || !obj.AbilityIndex.HasValue)
                return;
            if (obj.AbilityIndex.Value < 0 || obj.AbilityIndex.Value >= source.Printing.Abilities.Count)
                return;

            var ability = source.Printing.Abilities[obj.AbilityIndex.Value];
            var chosenTarget = obj.Targets.Count > 0 ? Match.GetCard(obj.Targets[0]) : null;
            EffectInterpreter.RunAbility(this, ability, new EffectContext(source, player, chosenTarget));
        }

        string StoreAction(Player player, PlayerAction action)
        {
            if (Match.Phase != Phase.Main || player.Id != Match.ActivePlayerId)
                return "Store only on your Main.";
            if (player.StoreActionsThisTurn >= 1)
                return "One Store action per turn."; // test 26

            var kind = action.StoreKind ?? MapStoreKind(action.Kind);
            var paidWorth = 0;

            if (kind == StoreActionKind.Buy)
            {
                var card = StoreCardAt(action.StoreSlotIndex);
                if (card == null)
                    return "Empty store slot.";
                var cost = card.Printing != null ? card.Printing.StoreWorth : 0;
                if (player.Worth < cost)
                    return "Insufficient Worth.";
                paidWorth = PayWorth(player, cost);
            }
            else if (kind == StoreActionKind.Trade)
            {
                var storeCard = StoreCardAt(action.StoreSlotIndex);
                var handCard = FindHandCard(player, action.HandCardInstanceId);
                if (storeCard == null || handCard == null)
                    return "Trade needs a hand card and a store card.";
                var storeCost = storeCard.Printing != null ? storeCard.Printing.StoreWorth : 0;
                var handCost = handCard.Printing != null ? handCard.Printing.StoreWorth : 0;
                var extra = storeCost > handCost ? storeCost - handCost : 0;
                if (player.Worth < extra)
                    return "Insufficient Worth.";
                paidWorth = PayWorth(player, extra);
            }
            else if (kind == StoreActionKind.Sell)
            {
                if (!action.HandCardInstanceId.HasValue)
                    return "No card selected to sell.";
                if (FindHandCard(player, action.HandCardInstanceId) == null)
                    return "No card selected to sell.";
                if (player.BoughtThisTurn.Contains(action.HandCardInstanceId.Value))
                    return "Cannot sell a card you just bought this turn.";
            }
            else if (kind == StoreActionKind.Row)
            {
                if (player.Worth < 2)
                    return "Row costs 2 Worth.";
                paidWorth = PayWorth(player, 2);
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
            if (obj.PaidWill > 0)
                Emit(EventKind.WillSet, "player", controller.Id, "amount", controller.Will);
            if (obj.PaidWorth > 0)
                GainWorth(controller, obj.PaidWorth);
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
                case StackObjectType.Activate:
                    ResolveActivate(obj);
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

                if (card.Printing.Type == CardType.Bond)
                {
                    var host = obj.Targets.Count > 0 ? Match.GetCard(obj.Targets[0]) : null;
                    host ??= player.Icon; // 4.7.42 — a legal default when no host was chosen
                    AttachBond(card, host);
                }

                EffectInterpreter.FireOwnHook(this, card, EffectWhen.OnEnter, new EffectContext(card, player));
            }
            else
            {
                // Surge / Algorithm — 4.7.40/41: run its effects, then it goes to Removed.
                EffectInterpreter.RunImmediate(this, card, new EffectContext(card, player, obj.Targets.Count > 0 ? Match.GetCard(obj.Targets[0]) : null));
                Match.Removed.Add(card);
                card.Zone = Zone.Removed;
            }
        }

        CardInstance StoreCardAt(int? slotIndex)
        {
            if (!slotIndex.HasValue || slotIndex.Value < 0 || slotIndex.Value >= Match.Store.Length)
                return null;
            return Match.Store[slotIndex.Value];
        }

        static CardInstance FindHandCard(Player player, int? instanceId)
        {
            if (player == null || !instanceId.HasValue)
                return null;
            for (var i = 0; i < player.Hand.Count; i++)
            {
                if (player.Hand[i].InstanceId == instanceId.Value)
                    return player.Hand[i];
            }

            return null;
        }

        internal int PayWorth(Player player, int amount)
        {
            if (amount <= 0)
                return 0;
            player.Worth -= amount;
            EmitWorthChanged(player, -amount);
            return amount;
        }

        internal int GainWorth(Player player, int amount)
        {
            if (player == null || amount <= 0)
                return 0;
            player.Worth += amount;
            EmitWorthChanged(player, amount);
            return amount;
        }

        void EmitWorthChanged(Player player, int delta)
        {
            Emit(EventKind.WorthChanged, "player", player.Id, "delta", delta, "amount", player.Worth);
        }

        internal void GainHonor(Player player, int amount)
        {
            if (player == null || amount == 0)
                return;
            player.Honor += amount;
            Emit(EventKind.HonorChanged, "player", player.Id, "delta", amount, "amount", player.Honor);
        }

        /// <summary>Generic card-effect draw (5.3 Draw op). Shares rule 4.4.18's empty-deck-loses
        /// consequence with the Start-step draw in MatchSetup.ApplyStartAutomatic.</summary>
        internal void DrawCards(Player player, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (player.Deck.Count == 0)
                {
                    var events = new List<GameEvent>();
                    StateChecks.EliminatePlayer(Match, player, LossReason.EmptyDeckDraw, events, Match.NextTs());
                    RecordEvents(events);
                    return;
                }

                var card = player.Deck[0];
                player.Deck.RemoveAt(0);
                card.Zone = Zone.Hand;
                player.Hand.Add(card);
                Emit(EventKind.CardDrew, "player", player.Id, "instanceId", card.InstanceId);
            }
        }

        internal void RecordEvents(List<GameEvent> events)
        {
            pendingEvents.AddRange(events);
            Match.EventLog.AddRange(events);
        }

        /// <summary>Base rule 4.7.42 — a resolving Bond attaches to its chosen host. Also reachable
        /// from card effects via the AttachBond op (e.g. an ability that re-homes a Bond).</summary>
        internal void AttachBond(CardInstance bond, CardInstance host)
        {
            if (bond == null || host == null || host.Zone != Zone.Field || host.ControllerId != bond.ControllerId)
                return;
            bond.HostInstanceId = host.InstanceId;
            Emit(EventKind.BondAttached, "bond", bond.InstanceId, "host", host.InstanceId);
        }

        internal void SilenceStackObject(int stackId)
        {
            var target = FindStack(stackId);
            if (target == null)
                return;
            RefundSilenced(target);
            target.Fizzled = true;
            Match.Stack.Remove(target);
            Emit(EventKind.Silenced, "stackId", target.StackId);
            Emit(EventKind.StackPopped, "stackId", target.StackId, "fizzled", true);
        }

        /// <summary>LookStore (new op, no fixed Engine B rule number) — peek the top N Supply cards,
        /// placing each into an open Store slot if one exists; anything that doesn't fit goes to the
        /// bottom of Supply rather than being lost.</summary>
        internal void PeekSupplyIntoStore(int count)
        {
            var lookedAt = new List<CardInstance>();
            for (var i = 0; i < count && Match.Supply.Count > 0; i++)
            {
                lookedAt.Add(Match.Supply[0]);
                Match.Supply.RemoveAt(0);
            }

            for (var i = 0; i < lookedAt.Count; i++)
            {
                var card = lookedAt[i];
                var placed = false;
                for (var s = 0; s < Match.Store.Length; s++)
                {
                    if (Match.Store[s] != null)
                        continue;
                    Match.Store[s] = card;
                    card.Zone = Zone.Store;
                    card.ControllerId = -1;
                    Emit(EventKind.CardMoved, "instanceId", card.InstanceId, "zone", Zone.Store.ToString());
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    card.Zone = Zone.Supply;
                    Match.Supply.Add(card);
                }
            }
        }

        /// <summary>Sell always puts the card into the Store. If every slot is full, the last
        /// Store card is pushed to the bottom of Supply so the sold card stays on the river.</summary>
        void PlaceSoldCardInStore(CardInstance card)
        {
            for (var i = 0; i < Match.Store.Length; i++)
            {
                if (Match.Store[i] != null)
                    continue;

                Match.Store[i] = card;
                card.Zone = Zone.Store;
                card.ControllerId = -1;
                return;
            }

            var last = Match.Store.Length - 1;
            var displaced = Match.Store[last];
            if (displaced != null)
            {
                displaced.Zone = Zone.Supply;
                displaced.ControllerId = -1;
                Match.Supply.Add(displaced);
                Emit(EventKind.CardMoved, "instanceId", displaced.InstanceId, "zone", Zone.Supply.ToString());
            }

            Match.Store[last] = card;
            card.Zone = Zone.Store;
            card.ControllerId = -1;
        }

        void ResolveStore(StackObject obj)
        {
            var player = Match.GetPlayer(obj.ControllerId);
            switch (obj.StoreKind)
            {
                case StoreActionKind.Buy:
                    {
                        var card = StoreCardAt(obj.StoreSlotIndex);
                        if (card != null && obj.StoreSlotIndex.HasValue)
                        {
                            Match.Store[obj.StoreSlotIndex.Value] = null;
                            card.Zone = Zone.Hand;
                            card.ControllerId = player.Id;
                            player.Hand.Add(card);
                            player.BoughtThisTurn.Add(card.InstanceId);
                            Emit(EventKind.CardMoved, "instanceId", card.InstanceId, "zone", Zone.Hand.ToString());
                        }
                    }
                    break;
                case StoreActionKind.Trade:
                    {
                        var storeCard = StoreCardAt(obj.StoreSlotIndex);
                        var handCard = FindHandCard(player, obj.HandCardInstanceId);
                        if (storeCard != null && handCard != null && obj.StoreSlotIndex.HasValue)
                        {
                            player.Hand.Remove(handCard);
                            Match.Store[obj.StoreSlotIndex.Value] = handCard;
                            handCard.Zone = Zone.Store;
                            handCard.ControllerId = -1;

                            storeCard.Zone = Zone.Hand;
                            storeCard.ControllerId = player.Id;
                            player.Hand.Add(storeCard);
                            player.BoughtThisTurn.Add(storeCard.InstanceId);
                            Emit(EventKind.CardMoved, "instanceId", storeCard.InstanceId, "zone", Zone.Hand.ToString());
                            Emit(EventKind.CardMoved, "instanceId", handCard.InstanceId, "zone", Zone.Store.ToString());
                        }
                    }
                    break;
                case StoreActionKind.Sell:
                    {
                        var card = FindHandCard(player, obj.HandCardInstanceId);
                        if (card != null)
                        {
                            player.Hand.Remove(card);
                            var gained = card.Printing != null ? card.Printing.StoreWorth : 0;
                            if (gained < 0)
                                gained = 0;
                            GainWorth(player, gained);
                            PlaceSoldCardInStore(card);
                            Emit(EventKind.CardMoved, "instanceId", card.InstanceId, "zone", card.Zone.ToString());
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
                case StoreActionKind.List:
                    {
                        // 4.12.97 — look at the top 2 of Supply; one enters Store, one goes to the bottom of Supply.
                        if (Match.Supply.Count == 0)
                            break;

                        var first = Match.Supply[0];
                        var second = Match.Supply.Count > 1 ? Match.Supply[1] : null;
                        Match.Supply.RemoveAt(0);
                        if (second != null)
                            Match.Supply.RemoveAt(0); // second shifted down to index 0 once first left

                        var placed = false;
                        for (var i = 0; i < Match.Store.Length; i++)
                        {
                            if (Match.Store[i] != null)
                                continue;
                            Match.Store[i] = first;
                            first.Zone = Zone.Store;
                            first.ControllerId = -1;
                            Emit(EventKind.CardMoved, "instanceId", first.InstanceId, "zone", Zone.Store.ToString());
                            placed = true;
                            break;
                        }

                        if (!placed)
                            Match.Supply.Insert(0, first); // no open Store slot; leave on top for the next refill (4.12.99)

                        if (second != null)
                        {
                            second.Zone = Zone.Supply;
                            Match.Supply.Add(second);
                        }
                    }
                    break;
                case StoreActionKind.Keep:
                    break;
            }

            if (obj.StoreKind.HasValue)
                EffectInterpreter.FireOnStoreAction(this, player, obj.StoreKind.Value);
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
            FireHookOnAllFieldCards(EffectWhen.OnClashEnd);
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
                // 4.8.60 — Aggression Presses, including every Bazerk wave 0 (always tagged
                // Aggression at declare — see CreatePressStack), deal here. Nothing else does:
                // an ordinary non-Aggression wave-0 press belongs in C6 only.
                if (!press.Keywords.Contains(Keyword.Aggression))
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
            if (damage > 0)
            {
                target.DamageMarked += damage;
                press.DamageDealt = damage;
            }

            Emit(EventKind.DamageDealt, "source", source.InstanceId, "target", target.InstanceId, "amount", damage, "wave", press.Wave, "absolute", absolute);
            Emit(EventKind.HealthChanged, "instanceId", target.InstanceId, "current", target.CurrentHealth, "printed", target.Health);

            if (damage <= 0)
                return;

            if (press.Keywords.Contains(Keyword.Gashing) && target.Printing.Type == CardType.Companion)
            {
                target.DamageMarked = target.Health;
                Emit(EventKind.HealthChanged, "instanceId", target.InstanceId, "current", target.CurrentHealth, "printed", target.Health);
            }

            if (target.CurrentHealth <= 0)
                press.RemovedTarget = true;

            var controller = Match.GetPlayer(source.ControllerId);
            EffectInterpreter.FireOwnHook(this, source, EffectWhen.OnPressDealtDamage, new EffectContext(source, controller, target, damage));
            if (press.RemovedTarget)
                EffectInterpreter.FireOwnHook(this, source, EffectWhen.OnPressRemovedBody, new EffectContext(source, controller, target, damage));

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
                Emit(EventKind.HealthChanged, "instanceId", icon.InstanceId, "current", icon.CurrentHealth, "printed", icon.Health);
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
            FireRemovalHooks(events);
        }

        /// <summary>OnRemoved for anything the state-based checks (4.10) pulled off a Field this
        /// pass. A ceased Token isn't retrievable afterward (it lands in no zone list at all), so
        /// it doesn't get a hook fire here — a documented, untested edge case.</summary>
        void FireRemovalHooks(List<GameEvent> events)
        {
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Kind != EventKind.PermanentLeft)
                    continue;
                if (!e.Data.TryGetValue("zone", out var zoneObj) || !(zoneObj is string zoneStr) || zoneStr != Zone.Field.ToString())
                    continue;
                if (!e.Data.TryGetValue("instanceId", out var idObj) || !(idObj is int instanceId))
                    continue;
                var card = Match.GetCard(instanceId);
                if (card == null)
                    continue;
                EffectInterpreter.FireOwnHook(this, card, EffectWhen.OnRemoved, new EffectContext(card, Match.GetPlayer(card.ControllerId)));
            }
        }

        internal void Emit(EventKind kind, params object[] keyValuePairs)
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
