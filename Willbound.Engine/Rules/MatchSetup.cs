using System.Collections.Generic;

namespace Willbound.Engine
{
    public static class MatchSetup
    {
        // 4.1 match setup
        public static Match Create(
            ICardDatabase database,
            IRng rng,
            IList<SetupPlayer> setupPlayers,
            int firstActivePlayerId = 0)
        {
            var match = new Match
            {
                MatchId = System.Guid.NewGuid().ToString("N"),
                Round = 1,
                Phase = Phase.Setup,
                ActivePlayerId = firstActivePlayerId,
                PriorityPlayerId = firstActivePlayerId,
            };

            for (var i = 0; i < setupPlayers.Count; i++)
            {
                var spec = setupPlayers[i];
                var player = new Player
                {
                    Id = spec.PlayerId,
                    Name = spec.Name ?? $"Player {spec.PlayerId}",
                    Seat = i,
                    Worth = 3,
                    Honor = 0,
                    Will = 0,
                };

                player.Icon = CreateInstance(match, database, spec.IconId, spec.PlayerId, Zone.Field);
                player.Icon.Ready = true;
                player.Icon.Exhausted = false;
                player.Field.Add(player.Icon);

                for (var d = 0; d < spec.DeckIds.Count; d++)
                {
                    var card = CreateInstance(match, database, spec.DeckIds[d], spec.PlayerId, Zone.Deck);
                    player.Deck.Add(card);
                }

                rng.Shuffle(player.Deck);

                for (var h = 0; h < 5 && player.Deck.Count > 0; h++)
                {
                    var card = player.Deck[0];
                    player.Deck.RemoveAt(0);
                    card.Zone = Zone.Hand;
                    player.Hand.Add(card);
                }

                for (var f = 0; f < spec.StartInFieldIds.Count; f++)
                {
                    var card = CreateInstance(match, database, spec.StartInFieldIds[f], spec.PlayerId, Zone.Field);
                    card.Ready = true;
                    player.Field.Add(card);
                }

                match.Players.Add(player);
            }

            for (var s = 0; s < specSupplyIds(database, setupPlayers).Count; s++)
            {
                var card = CreateInstance(match, database, specSupplyIds(database, setupPlayers)[s], -1, Zone.Supply);
                match.Supply.Add(card);
            }

            RefillStore(match, rng);
            match.Phase = Phase.Start;
            BeginTurn(match, rng);
            return match;
        }

        static List<string> specSupplyIds(ICardDatabase database, IList<SetupPlayer> players)
        {
            var ids = new List<string>();
            foreach (var printing in database.All)
            {
                if (printing.Type == CardType.Icon)
                    continue;
                for (var i = 0; i < 3; i++)
                    ids.Add(printing.Id);
            }

            if (ids.Count < 20)
            {
                for (var i = ids.Count; i < 30; i++)
                    ids.Add("VANILLA-COMPANION");
            }

            return ids;
        }

        public static CardInstance CreateInstance(Match match, ICardDatabase database, string printingId, int ownerId, Zone zone)
        {
            if (!database.TryGet(printingId, out var printing))
                throw new System.ArgumentException($"Unknown printing {printingId}");

            var card = new CardInstance
            {
                InstanceId = match.NextInstance(),
                Printing = printing,
                OwnerId = ownerId,
                ControllerId = ownerId,
                Zone = zone,
                Ready = true,
                IsToken = printing.Type == CardType.Token,
            };

            card.KeywordsNow = KeywordParser.Parse(printing.Keywords);
            return card;
        }

        public static void BeginTurn(Match match, IRng rng)
        {
            var player = match.GetPlayer(match.ActivePlayerId);
            if (player.Lost)
            {
                AdvanceActivePlayer(match);
                if (match.WinnerId.HasValue)
                    return;
                BeginTurn(match, rng);
                return;
            }

            match.Phase = Phase.Start;
            match.ClashPhase = ClashPhase.None;
            match.Passed.Clear();
            match.StartStepComplete = false;
            match.DrawStepComplete = false;
            match.SiteStepComplete = false;
            match.EndStepComplete = false;
            player.StoreActionsThisTurn = 0;
            player.SitesPlayedThisTurn = 0;
            player.DeclaredClashThisTurn = false;

            ApplyStartAutomatic(match, player);
            match.Phase = Phase.Site;
            match.PriorityPlayerId = match.ActivePlayerId;
            match.Passed.Clear();
        }

        static void ApplyStartAutomatic(Match match, Player player)
        {
            // 4.4.16 Ready all permanents you control
            ReadiedAll(match, player);

            // 4.4.17 Will pool = min(Round, 8)
            var will = System.Math.Min(match.Round, 8);
            player.Will = will;
            match.EventLog.Add(GameEvent.Create(EventKind.WillSet, match.NextTs(), new Dictionary<string, object>
            {
                ["player"] = player.Id,
                ["amount"] = will,
            }));

            // 4.4.18 Draw 1
            if (player.Deck.Count == 0)
            {
                StateChecks.EliminatePlayer(match, player, LossReason.EmptyDeckDraw, match.EventLog, match.NextTs());
                return;
            }

            var drawn = player.Deck[0];
            player.Deck.RemoveAt(0);
            drawn.Zone = Zone.Hand;
            player.Hand.Add(drawn);
            match.EventLog.Add(GameEvent.Create(EventKind.CardDrew, match.NextTs(), new Dictionary<string, object>
            {
                ["player"] = player.Id,
                ["instanceId"] = drawn.InstanceId,
            }));

            match.StartStepComplete = true;
            match.DrawStepComplete = true;
        }

        static void ReadiedAll(Match match, Player player)
        {
            for (var i = 0; i < player.Field.Count; i++)
            {
                var card = player.Field[i];
                card.Exhausted = false;
                card.Ready = true;
                match.EventLog.Add(GameEvent.Create(EventKind.Readied, match.NextTs(), new Dictionary<string, object>
                {
                    ["instanceId"] = card.InstanceId,
                }));
            }

            if (player.Icon != null)
            {
                player.Icon.Exhausted = false;
                player.Icon.Ready = true;
            }
        }

        public static void RefillStore(Match match, IRng rng)
        {
            for (var i = 0; i < match.Store.Length; i++)
            {
                if (match.Store[i] != null || match.Supply.Count == 0)
                    continue;
                var card = match.Supply[0];
                match.Supply.RemoveAt(0);
                card.Zone = Zone.Store;
                card.ControllerId = -1;
                match.Store[i] = card;
            }

            match.EventLog.Add(GameEvent.Create(EventKind.StoreRefilled, match.NextTs()));
        }

        public static void AdvanceActivePlayer(Match match)
        {
            var start = match.ActivePlayerId;
            do
            {
                var idx = SeatIndex(match, match.ActivePlayerId);
                idx = (idx + 1) % match.Players.Count;
                match.ActivePlayerId = match.Players[idx].Id;
                if (match.GetPlayer(match.ActivePlayerId).IsAlive)
                    break;
            }
            while (match.ActivePlayerId != start);

            match.PriorityPlayerId = match.ActivePlayerId;
        }

        static int SeatIndex(Match match, int playerId)
        {
            for (var i = 0; i < match.Players.Count; i++)
            {
                if (match.Players[i].Id == playerId)
                    return i;
            }

            return 0;
        }
    }

    public sealed class SetupPlayer
    {
        public int PlayerId;
        public string Name;
        public string IconId;
        public List<string> DeckIds = new List<string>();
        public List<string> StartInFieldIds = new List<string>();
    }

    public static class KeywordParser
    {
        public static HashSet<Keyword> Parse(IList<string> keywords)
        {
            var set = new HashSet<Keyword>();
            if (keywords == null)
                return set;
            for (var i = 0; i < keywords.Count; i++)
            {
                var text = keywords[i];
                if (string.IsNullOrEmpty(text))
                    continue;
                if (text.StartsWith("Toll"))
                {
                    set.Add(Keyword.Toll);
                    continue;
                }

                if (System.Enum.TryParse<Keyword>(text, true, out var kw))
                    set.Add(kw);
            }

            return set;
        }
    }
}
