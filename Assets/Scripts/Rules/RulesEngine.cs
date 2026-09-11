using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LegendsOfTheUniverse.Rules
{
    public sealed class RulesEngine
    {
        readonly List<GameEvent> eventLog = new();
        readonly List<IPresentationEvents> presentationListeners = new();
        readonly HashSet<int> priorityPasses = new();

        public GameState State { get; private set; }

        public IReadOnlyList<GameEvent> EventLog => eventLog;

        public void Subscribe(IPresentationEvents listener)
        {
            if (listener != null && !presentationListeners.Contains(listener))
                presentationListeners.Add(listener);
        }

        public void StartGame(GameState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            State.ActivePlayerIndex = 0;
            BeginTurn();
        }

        public void StartJamesEndlessDemo(int? rngSeed = null)
        {
            StartGame(EndlessDeckSetup.CreateJamesEndlessDemo(rngSeed));
        }

        public void BeginTurn()
        {
            var player = State.ActivePlayer;
            if (player.IsEliminated)
            {
                AdvanceActivePlayer();
                if (State.GameOver)
                    return;

                BeginTurn();
                return;
            }

            State.CurrentStep = TurnStep.Start;
            Emit(GameEventKind.TurnStart, $"Player {player.PlayerId}");

            ApplyStartOfTurnWill(player);
            ClearTurnFlags(player);

            AdvanceToStep(TurnStep.WillSite);
        }

        void ApplyStartOfTurnWill(PlayerState player)
        {
            var grant = State.WillGrantForRound();
            player.WillPool = grant;
            Emit(GameEventKind.WillChanged, $"Player {player.PlayerId} Will={grant} (round {State.Round})");
        }

        void ClearTurnFlags(PlayerState player)
        {
            player.HasTakenStoreActionThisTurn = false;
            player.HasPlayedWillSiteThisTurn = false;
            player.HasPressedOrHeldThisTurn = false;
            EndlessCombatEngine.OnTurnStart(State);
        }

        public void AdvanceToStep(TurnStep step)
        {
            State.CurrentStep = step;
            if (step == TurnStep.Clash)
                EndlessCombatEngine.OnClashStepStart(State);

            GivePriorityToActivePlayer();
            Emit(GameEventKind.StepStart, step.ToString());
        }

        void GivePriorityToActivePlayer()
        {
            State.PriorityPlayerIndex = State.ActivePlayerIndex;
            priorityPasses.Clear();
            Emit(GameEventKind.PriorityChanged, $"Priority player {State.PriorityPlayer.PlayerId}");
        }

        public RulesAnswer HandlePriorityAction(PriorityActionKind action, int actingPlayerId, StackItem pendingStackItem = null)
        {
            EnsureGameRunning();

            if (State.PriorityPlayer.PlayerId != actingPlayerId)
                return RulesAnswer.Reject("Only the player with priority may act.");

            switch (action)
            {
                case PriorityActionKind.PlaySurge:
                case PriorityActionKind.ActivateSpell:
                    if (pendingStackItem == null)
                        return RulesAnswer.Reject("Stack item required.");

                    State.Stack.Push(pendingStackItem);
                    priorityPasses.Clear();
                    Emit(GameEventKind.StackChanged, $"Push {pendingStackItem.EffectId}");
                    PassPriorityLeft();
                    break;

                case PriorityActionKind.Pass:
                    priorityPasses.Add(actingPlayerId);
                    if (AllLivingPlayersPassed())
                    {
                        if (!State.Stack.IsEmpty)
                            ResolveTopOfStack();
                        else
                            EndPriorityWindowForStep();
                    }
                    else
                    {
                        PassPriorityLeft();
                    }

                    break;

                default:
                    return RulesAnswer.Reject("Unknown priority action.");
            }

            RunStateChecks();
            return BuildAnswer("Priority action processed.");
        }

        bool AllLivingPlayersPassed()
        {
            var living = State.Players.Where(p => !p.IsEliminated).Select(p => p.PlayerId).ToHashSet();
            return living.SetEquals(priorityPasses);
        }

        void PassPriorityLeft()
        {
            do
            {
                State.PriorityPlayerIndex = (State.PriorityPlayerIndex + 1) % State.Players.Count;
            }
            while (State.PriorityPlayer.IsEliminated);

            Emit(GameEventKind.PriorityChanged, $"Priority player {State.PriorityPlayer.PlayerId}");
        }

        void ResolveTopOfStack()
        {
            var item = State.Stack.Pop();
            Emit(GameEventKind.StackChanged, $"Resolve {item.EffectId}");
            RunStateChecks();
            GivePriorityToActivePlayer();
        }

        void EndPriorityWindowForStep()
        {
            priorityPasses.Clear();

            switch (State.CurrentStep)
            {
                case TurnStep.WillSite:
                    AdvanceToStep(TurnStep.Main);
                    break;
                case TurnStep.Main:
                    AdvanceToStep(TurnStep.Clash);
                    break;
                case TurnStep.Clash:
                    EndTurn();
                    break;
                default:
                    break;
            }
        }

        public RulesAnswer TryPlayWillSite(CardInstance willSite, int actingPlayerId)
        {
            EnsureGameRunning();

            if (State.CurrentStep != TurnStep.WillSite)
                return RulesAnswer.Reject("Will sites are played during the Will site step.");

            if (State.ActivePlayer.PlayerId != actingPlayerId)
                return RulesAnswer.Reject("Not your turn.");

            if (willSite.PrintedType != CardType.Will)
                return RulesAnswer.Reject("Only Will sites may be played in this step.");

            var player = State.ActivePlayer;
            if (player.HasPlayedWillSiteThisTurn)
                return RulesAnswer.Reject("At most one Will site per turn.");

            MoveCard(willSite, player.Hand, player.Willwell, ZoneType.Willwell);
            player.HasPlayedWillSiteThisTurn = true;
            willSite.LastAppliedTimestamp = State.NextTimestamp();

            RunStateChecks();
            return BuildAnswer("Will site played. Will pool unchanged.");
        }

        public RulesAnswer TryPayWillCost(int actingPlayerId, int cost)
        {
            var player = State.GetPlayer(actingPlayerId);
            if (player == null)
                return RulesAnswer.Reject("Unknown player.");

            if (cost < 0)
                return RulesAnswer.Reject("Invalid cost.");

            if (player.WillPool < cost)
                return RulesAnswer.Reject("Insufficient Will.");

            player.WillPool -= cost;
            Emit(GameEventKind.CostsPaid, $"Player {actingPlayerId} paid {cost} Will");
            Emit(GameEventKind.WillChanged, $"Player {actingPlayerId} Will={player.WillPool}");
            return BuildAnswer($"Paid {cost} Will.");
        }

        public RulesAnswer TryStoreBuy(CardInstance storeCard, int actingPlayerId, int storePrice)
        {
            EnsureGameRunning();

            if (State.CurrentStep != TurnStep.Main)
                return RulesAnswer.Reject("Store actions happen during Main.");

            var player = State.GetPlayer(actingPlayerId);
            if (player == null || player.PlayerId != State.ActivePlayer.PlayerId)
                return RulesAnswer.Reject("Not your turn.");

            if (player.HasTakenStoreActionThisTurn)
                return RulesAnswer.Reject("One Store action per turn.");

            if (!player.CanAddToDeck && !CardGoesToHand(storeCard))
                return RulesAnswer.Reject($"Deck cannot exceed {GameConstants.MaxDeckSize} cards.");

            var price = Math.Max(0, storePrice);
            if (player.Worth < price)
                return RulesAnswer.Reject("Insufficient Worth.");

            player.Worth -= price;
            player.HasTakenStoreActionThisTurn = true;

            RemoveFromStore(storeCard);
            player.Hand.Add(storeCard);
            storeCard.Zone = ZoneType.Hand;
            storeCard.ControllerId = actingPlayerId;
            State.Sold.Add(storeCard);

            RefillStoreFromSupply();
            Emit(GameEventKind.WorthChanged, $"Player {actingPlayerId} Worth={player.Worth}");
            Emit(GameEventKind.StoreChanged, "Buy");

            RunStateChecks();
            return BuildAnswer("Buy resolved.");
        }

        static bool CardGoesToHand(CardInstance card)
        {
            return card.PrintedType != CardType.Will;
        }

        void RemoveFromStore(CardInstance card)
        {
            for (var i = 0; i < State.Store.Length; i++)
            {
                if (State.Store[i] == card)
                    State.Store[i] = null;
            }
        }

        void RefillStoreFromSupply()
        {
            for (var i = 0; i < State.Store.Length; i++)
            {
                if (State.Store[i] != null || State.Supply.Count == 0)
                    continue;

                var card = State.Supply[0];
                State.Supply.RemoveAt(0);
                State.Store[i] = card;
                card.Zone = ZoneType.Store;
            }
        }

        public RulesAnswer ApplyPressDamage(CardInstance attacker, CardInstance target, int strikeAmount,
            bool targetIsHolding, bool sanjayBlackLeg = false)
        {
            EnsureGameRunning();

            if (State.CurrentStep != TurnStep.Clash)
                return RulesAnswer.Reject("Press and Hold happen during Clash.");

            var pendingTarget = EndlessCombatEngine.ConsumePendingPressTarget(State);
            if (pendingTarget != null)
                target = pendingTarget;

            var attackerPlayer = State.GetPlayer(attacker.ControllerId);
            var defenderPlayer = State.GetPlayer(target.ControllerId);
            if (attackerPlayer == null || defenderPlayer == null)
                return RulesAnswer.Reject("Invalid Press participants.");

            if (!EndlessCombatEngine.CanPressThisClash(State, attackerPlayer))
                return RulesAnswer.Reject("No Press remaining this Clash.");

            var modifiedStrike = strikeAmount;
            if (sanjayBlackLeg && !EndlessCombatEngine.TrySanjayBlackLeg(State, attacker))
                return RulesAnswer.Reject("Black Leg unavailable.");

            if (sanjayBlackLeg)
                modifiedStrike += 1;

            var guardApplied = EndlessCombatEngine.ModifyGuardForPress(
                State, attacker, target, attackerPlayer, defenderPlayer, targetIsHolding, modifiedStrike);
            var damage = Math.Max(0, modifiedStrike - guardApplied);
            target.Damage += damage;
            attacker.Exhausted = true;

            EndlessCombatEngine.RecordPress(State, attacker, target, damage);
            EndlessCombatEngine.ClearPressGuardModifiers(State, attacker);
            if (attacker.PrintedType == CardType.Companion)
                EndlessCombatEngine.RecordOpposingCompanionAbility(State, attacker, "Press");
            Emit(GameEventKind.DamageDealt, $"{attacker.CardId} → {target.CardId} for {damage}");
            RunStateChecks();
            return BuildAnswer($"Damage={damage}.");
        }

        public RulesAnswer PayEddieRubberGuard(CardInstance eddie)
        {
            var player = State.GetPlayer(eddie?.ControllerId ?? -1);
            if (player == null)
                return RulesAnswer.Reject("Unknown controller.");

            return EndlessCombatEngine.TryPayEddieRubberGuard(this, eddie, player)
                ? BuildAnswer("Rubber Guard paid.")
                : RulesAnswer.Reject("Cannot pay Rubber Guard.");
        }

        public RulesAnswer RetargetLastPress(CardInstance damon, CardInstance newTarget)
        {
            return EndlessCombatEngine.TryDempseyRollRetarget(this, damon, newTarget)
                ? BuildAnswer("Press retargeted.")
                : RulesAnswer.Reject("Cannot retarget this Press.");
        }

        public RulesAnswer ApplyZaneSecondTarget(CardInstance zane, CardInstance secondTarget)
        {
            return EndlessCombatEngine.TryZaneSecondTarget(this, zane, secondTarget)
                ? BuildAnswer("Second target damaged.")
                : RulesAnswer.Reject("Cannot apply Three-Blade Style.");
        }

        public RulesAnswer IronGripRetarget(CardInstance kaito, CardInstance newTarget)
        {
            return EndlessCombatEngine.TryIronGripRetarget(State, kaito, newTarget)
                ? BuildAnswer("Press target changed.")
                : RulesAnswer.Reject("Cannot retarget with Iron Grip.");
        }

        public RulesAnswer ActivateCopycat(CardInstance kiro)
        {
            if (!EndlessCombatEngine.TryActivateCopycat(this, kiro))
                return RulesAnswer.Reject("Copycat unavailable.");

            var ability = EndlessCombatEngine.GetCopycatAbilityName(State);
            var source = EndlessCombatEngine.GetCopycatSourceCardId(State);
            Emit(GameEventKind.StackChanged, $"Copycat: {ability} ({source})");
            return BuildAnswer($"Copycat: {ability}.");
        }

        public RulesAnswer ApplyIncomingClashDamage(CardInstance victim, int amount)
        {
            EnsureGameRunning();

            if (State.CurrentStep != TurnStep.Clash)
                return RulesAnswer.Reject("Clash damage only during Clash.");

            var damageToVictim = EndlessCombatEngine.ResolveIncomingClashDamage(
                State, victim, amount, out var redirectTarget);
            victim.Damage += damageToVictim;

            if (redirectTarget != null && damageToVictim < amount)
            {
                redirectTarget.Damage += amount - damageToVictim;
                Emit(GameEventKind.DamageDealt,
                    $"Redirected 1 from {victim.CardId} → {redirectTarget.CardId}");
            }

            RunStateChecks();
            return BuildAnswer($"Incoming damage={damageToVictim}.");
        }

        public RulesAnswer TryMoveFromField(CardInstance card, PlayerState controller, string reasonCardName)
        {
            if (!EndlessCombatEngine.CanMoveFromField(card, reasonCardName))
                return RulesAnswer.Reject("Ronan cannot be moved unless named.");

            RemoveFromZoneLists(card, controller);
            return BuildAnswer("Move allowed.");
        }

        public void RunStateChecks()
        {
            CheckDamageRemovals();
            CheckBondHosts();
            CheckIconEliminations();
            CheckEmptyDeckLosses();
            CheckGameOver();
        }

        void CheckDamageRemovals()
        {
            foreach (var player in State.Players.Where(p => !p.IsEliminated))
            {
                var allCards = EnumerateControlledCards(player).ToList();
                foreach (var card in allCards)
                {
                    if (!card.IsRemovedByDamage)
                        continue;

                    if (card == player.Icon && EndlessCombatEngine.TryEndlessReplacement(this, card, player))
                        continue;

                    RemoveCard(card, player);
                }
            }
        }

        void CheckBondHosts()
        {
            foreach (var player in State.Players.Where(p => !p.IsEliminated))
            {
                foreach (var bond in player.Field.Where(c => c.PrintedType == CardType.Bond).ToList())
                {
                    if (!bond.AttachedToInstanceId.HasValue)
                    {
                        RemoveCard(bond, player);
                        continue;
                    }

                    var hostExists = State.Players.Any(p =>
                        !p.IsEliminated &&
                        EnumerateControlledCards(p).Any(c => c.InstanceId == bond.AttachedToInstanceId));

                    if (!hostExists)
                        RemoveCard(bond, player);
                }
            }
        }

        void CheckIconEliminations()
        {
            foreach (var player in State.Players.Where(p => !p.IsEliminated))
            {
                if (player.Icon == null)
                    continue;

                if (player.Icon.Sealed && player.Icon.RemainingHealth <= 0)
                    continue;

                if (player.Icon.RemainingHealth > 0)
                    continue;

                if (EndlessCombatEngine.TryEndlessReplacement(this, player.Icon, player))
                    continue;

                EliminatePlayer(player);
            }
        }

        void EliminatePlayer(PlayerState player)
        {
            player.IsEliminated = true;

            MoveAllToOut(player.Hand, player);
            MoveAllToOut(player.Field, player);
            MoveAllToOut(player.Willwell, player);
            MoveAllToOut(player.Deck, player);

            if (player.Icon != null)
            {
                player.OutOfPlay.Add(player.Icon);
                player.Icon.Zone = ZoneType.OutOfPlay;
                player.Icon = null;
            }

            Emit(GameEventKind.PlayerOut, $"Player {player.PlayerId} eliminated");
        }

        void CheckEmptyDeckLosses()
        {
            foreach (var player in State.Players.Where(p => !p.IsEliminated))
            {
                if (player.DeckCount == 0)
                    EliminatePlayer(player);
            }
        }

        void CheckGameOver()
        {
            var living = State.Players.Where(p => !p.IsEliminated).ToList();
            if (living.Count <= 1)
            {
                State.GameOver = true;
                State.WinnerPlayerId = living.Count == 1 ? living[0].PlayerId : null;
                Emit(GameEventKind.GameOver, State.WinnerPlayerId?.ToString() ?? "none");
            }
        }

        void RemoveCard(CardInstance card, PlayerState controller)
        {
            if (card == controller.Icon && EndlessCombatEngine.TryEndlessReplacement(this, card, controller))
                return;

            RemoveFromZoneLists(card, controller);
            controller.OutOfPlay.Add(card);
            card.Zone = ZoneType.OutOfPlay;

            if (card.IsToken)
            {
                controller.OutOfPlay.Remove(card);
                Emit(GameEventKind.Removed, $"{card.CardId} token vanished");
                return;
            }

            Emit(GameEventKind.Removed, card.CardId);
        }

        static void RemoveFromZoneLists(CardInstance card, PlayerState controller)
        {
            controller.Hand.Remove(card);
            controller.Field.Remove(card);
            controller.Willwell.Remove(card);
            controller.Deck.Remove(card);
        }

        static void MoveAllToOut(List<CardInstance> from, PlayerState controller)
        {
            foreach (var card in from.ToList())
            {
                from.Remove(card);
                controller.OutOfPlay.Add(card);
                card.Zone = ZoneType.OutOfPlay;
            }
        }

        static IEnumerable<CardInstance> EnumerateControlledCards(PlayerState player)
        {
            if (player.Icon != null)
                yield return player.Icon;

            foreach (var card in player.Hand)
                yield return card;
            foreach (var card in player.Field)
                yield return card;
            foreach (var card in player.Willwell)
                yield return card;
            foreach (var card in player.Deck)
                yield return card;
        }

        static void MoveCard(CardInstance card, List<CardInstance> from, List<CardInstance> to, ZoneType zone)
        {
            from.Remove(card);
            to.Add(card);
            card.Zone = zone;
        }

        void EndTurn()
        {
            State.CurrentStep = TurnStep.End;

            var wasLastInSeating = State.ActivePlayerIndex == State.Players.Count - 1;
            AdvanceActivePlayer();

            if (wasLastInSeating)
                IncrementRound();

            if (!State.GameOver)
                BeginTurn();
        }

        void IncrementRound()
        {
            State.Round++;
            Emit(GameEventKind.RoundChanged, State.Round.ToString());
        }

        void AdvanceActivePlayer()
        {
            var start = State.ActivePlayerIndex;
            do
            {
                State.ActivePlayerIndex = (State.ActivePlayerIndex + 1) % State.Players.Count;
                if (State.ActivePlayerIndex == start)
                    break;
            }
            while (State.ActivePlayer.IsEliminated);
        }

        void EnsureGameRunning()
        {
            if (State == null)
                throw new InvalidOperationException("Game not started.");

            if (State.GameOver)
                throw new InvalidOperationException("Game is over.");
        }

        void Emit(GameEventKind kind, string detail)
        {
            var gameEvent = new GameEvent(kind, detail);
            eventLog.Add(gameEvent);

            foreach (var listener in presentationListeners)
                listener.OnGameEvent(gameEvent);
        }

        public RulesAnswer TryPlayCard(int actingPlayerId, string cardName)
        {
            var prep = CardEngine.PreparePlay(this, actingPlayerId, cardName);
            if (!prep.Legal)
                return RulesAnswer.Reject(prep.Reason);

            var pay = TryPayWillCost(actingPlayerId, prep.PlayCost);
            if (!pay.Legal)
                return pay;

            RunStateChecks();
            return BuildAnswer($"Play {cardName} for {prep.PlayCost} Will → {prep.Destination}.");
        }

        public string LookupCardJson(string cardName)
        {
            return CardEngine.LookupJson(cardName);
        }

        public RulesAnswer BuildAnswer(string whatHappened)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Turn: player {State.ActivePlayer.PlayerId}, step {State.CurrentStep}");
            sb.AppendLine($"Priority: player {State.PriorityPlayer.PlayerId}");
            sb.AppendLine($"Round: {State.Round} (Will grant {State.WillGrantForRound()})");

            foreach (var player in State.Players.Where(p => !p.IsEliminated))
                sb.AppendLine($"P{player.PlayerId} Will={player.WillPool} Worth={player.Worth} Honor={player.Honor} Deck={player.DeckCount}");

            sb.AppendLine($"Stack: {FormatStack()}");
            sb.AppendLine($"Happened: {whatHappened}");
            sb.AppendLine($"Legal (priority holder): {FormatLegalActions()}");
            return new RulesAnswer(true, sb.ToString());
        }

        static RulesAnswer Reject(string reason)
        {
            return new RulesAnswer(false, reason);
        }

        string FormatStack()
        {
            if (State.Stack.IsEmpty)
                return "(empty)";

            return string.Join(" | ", State.Stack.Items.Select(i => i.EffectId));
        }

        string FormatLegalActions()
        {
            if (State.GameOver)
                return "none";

            var actions = new List<string> { "pass" };

            switch (State.CurrentStep)
            {
                case TurnStep.WillSite:
                    if (!State.ActivePlayer.HasPlayedWillSiteThisTurn)
                        actions.Add("play_will_site");
                    break;
                case TurnStep.Main:
                    if (!State.ActivePlayer.HasTakenStoreActionThisTurn)
                        actions.Add("store_action");
                    actions.Add("play_permanent");
                    actions.Add("activate_spell");
                    actions.Add("play_surge");
                    break;
                case TurnStep.Clash:
                    actions.Add("press");
                    actions.Add("hold");
                    break;
            }

            return string.Join(", ", actions);
        }
    }

    public readonly struct RulesAnswer
    {
        public RulesAnswer(bool legal, string summary)
        {
            Legal = legal;
            Summary = summary;
        }

        public bool Legal { get; }
        public string Summary { get; }

        public static RulesAnswer Reject(string reason) => new(false, reason);
    }
}
