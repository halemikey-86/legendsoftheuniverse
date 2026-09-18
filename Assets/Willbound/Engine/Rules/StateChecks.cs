using System.Collections.Generic;

namespace Willbound.Engine
{
    public static class StateChecks
    {
        // 4.10 state-based checks
        public static List<GameEvent> Run(Match match)
        {
            var events = new List<GameEvent>();
            var ts = match.NextTs();

            for (var iteration = 0; iteration < 8; iteration++)
            {
                var changed = false;

                // 4.10.81 Companion/Token Health <= 0
                for (var p = 0; p < match.Players.Count; p++)
                {
                    var player = match.Players[p];
                    for (var i = player.Field.Count - 1; i >= 0; i--)
                    {
                        var card = player.Field[i];
                        if (card.Printing.Type != CardType.Companion && card.Printing.Type != CardType.Token)
                            continue;
                        if (card.CurrentHealth > 0)
                            continue;

                        if (TryStandAgain(card, events, ts))
                        {
                            changed = true;
                            continue;
                        }

                        if (card.IsToken || card.Printing.Type == CardType.Token)
                        {
                            CeaseToken(match, card, player, events, ts);
                            changed = true;
                            continue;
                        }

                        if (card.HasKeyword(Keyword.Sealed))
                            continue;

                        RemoveCard(match, card, player, events, ts, "HealthZero");
                        changed = true;
                    }

                    if (player.Icon != null && player.Icon.CurrentHealth <= 0 && !player.Lost)
                    {
                        if (TryStandAgain(player.Icon, events, ts))
                        {
                            changed = true;
                            continue;
                        }
                        EliminatePlayer(match, player, LossReason.IconZeroHealth, events, ts);
                        changed = true;
                    }
                }

                // 4.10.82 Bond host missing
                for (var p = 0; p < match.Players.Count; p++)
                {
                    var player = match.Players[p];
                    for (var i = player.Field.Count - 1; i >= 0; i--)
                    {
                        var card = player.Field[i];
                        if (card.Printing.Type != CardType.Bond || !card.HostInstanceId.HasValue)
                            continue;
                        var host = match.GetCard(card.HostInstanceId.Value);
                        if (host != null && host.Zone == Zone.Field)
                            continue;
                        RemoveCard(match, card, player, events, ts, "BondHostMissing");
                        changed = true;
                    }
                }

                if (!changed)
                    break;
            }

            // 4.10.85 Hunted >= 10 on Icon
            for (var p = 0; p < match.Players.Count; p++)
            {
                var player = match.Players[p];
                if (player.Lost || player.Icon == null)
                    continue;
                if (player.Icon.GetCounter("hunted") >= 10)
                    EliminatePlayer(match, player, LossReason.HuntedTen, events, ts);
            }

            CheckWinner(match, events, ts);
            if (events.Count > 0)
                events.Add(GameEvent.Create(EventKind.CheckRan, ts));

            return events;
        }

        static bool TryStandAgain(CardInstance card, List<GameEvent> events, int ts)
        {
            if (!card.HasKeyword(Keyword.StandAgain) || card.StandAgainUsed)
                return false;
            card.StandAgainUsed = true;
            card.DamageMarked = card.Health - 1;
            events.Add(GameEvent.Create(EventKind.HealthChanged, ts, new Dictionary<string, object>
            {
                ["instanceId"] = card.InstanceId,
                ["current"] = card.CurrentHealth,
                ["printed"] = card.Health,
            }));
            return true;
        }

        public static void RemoveCard(Match match, CardInstance card, Player owner, List<GameEvent> events, int ts, string reason)
        {
            owner.Field.Remove(card);
            card.Zone = Zone.Removed;
            card.Ready = false;
            match.Removed.Add(card);
            events.Add(GameEvent.Create(EventKind.PermanentLeft, ts, new Dictionary<string, object>
            {
                ["instanceId"] = card.InstanceId,
                ["zone"] = Zone.Field.ToString(),
                ["reason"] = reason,
            }));
        }

        static void CeaseToken(Match match, CardInstance card, Player owner, List<GameEvent> events, int ts)
        {
            owner.Field.Remove(card);
            events.Add(GameEvent.Create(EventKind.PermanentLeft, ts, new Dictionary<string, object>
            {
                ["instanceId"] = card.InstanceId,
                ["zone"] = Zone.Field.ToString(),
                ["reason"] = "TokenCease",
            }));
        }

        public static void EliminatePlayer(Match match, Player player, LossReason reason, List<GameEvent> events, int ts)
        {
            player.Lost = true;
            player.Deck.Clear();
            player.Hand.Clear();
            player.Field.Clear();
            player.Willwell.Clear();
            events.Add(GameEvent.Create(EventKind.PlayerLost, ts, new Dictionary<string, object>
            {
                ["player"] = player.Id,
                ["reason"] = reason.ToString(),
            }));
            CheckWinner(match, events, ts);
        }

        static void CheckWinner(Match match, List<GameEvent> events, int ts)
        {
            Player last = null;
            var count = 0;
            for (var i = 0; i < match.Players.Count; i++)
            {
                if (!match.Players[i].Lost && match.Players[i].Icon != null && match.Players[i].Icon.CurrentHealth > 0)
                {
                    last = match.Players[i];
                    count++;
                }
            }

            if (count == 1 && last != null)
            {
                match.WinnerId = last.Id;
                match.Phase = Phase.Halt;
                events.Add(GameEvent.Create(EventKind.PlayerWon, ts, new Dictionary<string, object>
                {
                    ["player"] = last.Id,
                    ["reason"] = "LastIconStanding",
                }));
            }
        }
    }
}
