using Willbound.Engine;

namespace LegendsOfTheUniverse.Presentation.EngineBridge
{
    /// <summary>
    /// Deterministic opponent: play cheap bodies, Press the opposing Icon, Answer when able.
    /// Does not mutate match state — only proposes one <see cref="PlayerAction"/>.
    /// </summary>
    public sealed class HeuristicBot
    {
        public PlayerAction Choose(MatchRunner runner, int playerId)
        {
            if (runner?.Match == null)
                return Pass(playerId);

            var match = runner.Match;
            var player = match.GetPlayer(playerId);
            if (player == null || player.Lost)
                return Pass(playerId);

            var respond = ChooseStackResponse(match, player);
            if (respond != null)
                return respond;

            if (match.Phase == Phase.Clash)
            {
                var clash = ChooseClash(match, player);
                if (clash != null)
                    return clash;
            }

            var play = ChoosePlay(match, player);
            if (play != null)
                return play;

            var store = ChooseStore(match, player);
            if (store != null)
                return store;

            return Pass(playerId);
        }

        static PlayerAction ChooseStackResponse(Match match, Player player)
        {
            if (match.Stack == null || match.Stack.Count == 0)
                return null;

            var top = match.Stack[match.Stack.Count - 1];
            if (top.ControllerId == player.Id)
                return null;

            for (var i = 0; i < player.Hand.Count; i++)
            {
                var card = player.Hand[i];
                if (card?.Printing == null)
                    continue;
                if (card.Printing.Type != CardType.Surge && !EnginePlayRules.HasTiming(card, Timing.Now))
                    continue;
                if (!EnginePlayRules.CanPlayFromHand(match, player.Id, card.InstanceId))
                    continue;

                return new PlayerAction
                {
                    Kind = PlayerActionKind.PlayCard,
                    PlayerId = player.Id,
                    CardInstanceId = card.InstanceId,
                };
            }

            return null;
        }

        static PlayerAction ChooseClash(Match match, Player player)
        {
            if (match.ClashPhase == ClashPhase.C1_ActiveDeclare && player.Id == match.ActivePlayerId)
            {
                if (match.BodiesAwaitingDeclare == null || match.BodiesAwaitingDeclare.Count == 0)
                    return null;

                var attackerId = match.BodiesAwaitingDeclare[0];
                var attacker = match.GetCard(attackerId);
                var target = BestPressTarget(match, player.Id);
                if (attacker != null && target != null)
                {
                    return new PlayerAction
                    {
                        Kind = PlayerActionKind.DeclarePress,
                        PlayerId = player.Id,
                        CardInstanceId = attackerId,
                        TargetInstanceId = target.InstanceId,
                    };
                }

                return new PlayerAction
                {
                    Kind = PlayerActionKind.DeclareHold,
                    PlayerId = player.Id,
                    CardInstanceId = attackerId,
                };
            }

            if (match.ClashPhase == ClashPhase.C2_Answers)
            {
                var answer = ChooseAnswer(match, player);
                if (answer != null)
                    return answer;
            }

            return null;
        }

        static PlayerAction ChooseAnswer(Match match, Player player)
        {
            if (match.Stack == null)
                return null;

            CardInstance attacker = null;
            for (var i = match.Stack.Count - 1; i >= 0; i--)
            {
                var obj = match.Stack[i];
                if (obj.Type != StackObjectType.Press || obj.ControllerId == player.Id)
                    continue;
                if (obj.SourceInstanceId is int sourceId)
                {
                    attacker = match.GetCard(sourceId);
                    if (attacker != null)
                        break;
                }
            }

            if (attacker == null)
                return null;

            var answerer = BestAnswerBody(player);
            if (answerer == null)
                return null;

            return new PlayerAction
            {
                Kind = PlayerActionKind.Answer,
                PlayerId = player.Id,
                CardInstanceId = answerer.InstanceId,
                TargetInstanceId = attacker.InstanceId,
            };
        }

        static PlayerAction ChoosePlay(Match match, Player player)
        {
            CardInstance best = null;
            var bestCost = int.MaxValue;
            var bestStrike = -1;

            for (var i = 0; i < player.Hand.Count; i++)
            {
                var card = player.Hand[i];
                if (card?.Printing == null)
                    continue;
                if (!EnginePlayRules.CanPlayFromHand(match, player.Id, card.InstanceId))
                    continue;

                var printing = card.Printing;
                if (printing.Type == CardType.Surge || EnginePlayRules.HasTiming(card, Timing.Now))
                    continue;

                var cost = printing.WillCost;
                var strike = printing.Strike;
                if (best == null || cost < bestCost || (cost == bestCost && strike > bestStrike))
                {
                    best = card;
                    bestCost = cost;
                    bestStrike = strike;
                }
            }

            if (best == null)
                return null;

            return new PlayerAction
            {
                Kind = PlayerActionKind.PlayCard,
                PlayerId = player.Id,
                CardInstanceId = best.InstanceId,
            };
        }

        static PlayerAction ChooseStore(Match match, Player player)
        {
            if (match.Phase != Phase.Main || player.Id != match.ActivePlayerId)
                return null;
            if (player.StoreActionsThisTurn >= 1 || match.Store == null)
                return null;

            var bestSlot = -1;
            var bestCost = int.MaxValue;
            var bestStrike = -1;
            for (var i = 0; i < match.Store.Length; i++)
            {
                var card = match.Store[i];
                if (card?.Printing == null)
                    continue;
                if (card.Printing.Type != CardType.Companion && card.Printing.Type != CardType.Relic)
                    continue;
                var cost = card.Printing.StoreWorth;
                if (player.Worth < cost)
                    continue;
                var strike = card.Printing.Strike;
                if (bestSlot < 0 || cost < bestCost || (cost == bestCost && strike > bestStrike))
                {
                    bestSlot = i;
                    bestCost = cost;
                    bestStrike = strike;
                }
            }

            if (bestSlot < 0)
                return null;

            return new PlayerAction
            {
                Kind = PlayerActionKind.StoreBuy,
                PlayerId = player.Id,
                StoreSlotIndex = bestSlot,
                StoreKind = StoreActionKind.Buy,
            };
        }

        static CardInstance BestPressTarget(Match match, int attackerPlayerId)
        {
            CardInstance best = null;
            for (var p = 0; p < match.Players.Count; p++)
            {
                var opponent = match.Players[p];
                if (opponent == null || opponent.Id == attackerPlayerId || !opponent.IsAlive)
                    continue;

                if (IsLegalPressTarget(opponent.Icon))
                    return opponent.Icon;

                for (var i = 0; i < opponent.Field.Count; i++)
                {
                    var body = opponent.Field[i];
                    if (!IsLegalPressTarget(body))
                        continue;
                    if (best == null || body.CurrentHealth < best.CurrentHealth)
                        best = body;
                }
            }

            return best;
        }

        static bool IsLegalPressTarget(CardInstance body)
        {
            if (body == null || body.Zone != Zone.Field || body.CurrentHealth <= 0)
                return false;
            if (HasKeyword(body, Keyword.Closed))
                return false;
            var type = body.Printing?.Type;
            return type == CardType.Icon || type == CardType.Companion || type == CardType.Token;
        }

        static CardInstance BestAnswerBody(Player player)
        {
            CardInstance best = null;
            for (var i = 0; i < player.Field.Count; i++)
            {
                var body = player.Field[i];
                if (body == null || !body.Ready || body.Exhausted || body.CurrentHealth <= 0)
                    continue;
                var type = body.Printing?.Type;
                if (type != CardType.Icon && type != CardType.Companion && type != CardType.Token)
                    continue;
                if (best == null || body.Strike > best.Strike)
                    best = body;
            }

            return best;
        }

        static bool HasKeyword(CardInstance card, Keyword keyword)
        {
            if (card.HasKeyword(keyword))
                return true;
            var names = card.Printing?.Keywords;
            if (names == null)
                return false;
            var expected = keyword.ToString();
            for (var i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], expected, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static PlayerAction Pass(int playerId) =>
            new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = playerId };
    }
}
