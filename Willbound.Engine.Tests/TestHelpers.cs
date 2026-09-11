using System.Collections.Generic;
using Willbound.Engine;

namespace Willbound.Engine.Tests
{
    public static class TestHelpers
    {
        public static MatchRunner TwoPlayerMatch(int seed = 42, string iconA = "VANILLA-ICON", string iconB = "VANILLA-ICON")
        {
            var printings = new List<CardPrinting>(BootstrapCards.All);
            return MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = iconA, DeckIds = FillDeck("VANILLA-COMPANION", 20) },
                new SetupPlayer { PlayerId = 1, IconId = iconB, DeckIds = FillDeck("VANILLA-COMPANION", 20) },
            }, seed);
        }

        public static List<string> FillDeck(string id, int count)
        {
            var list = new List<string>();
            for (var i = 0; i < count; i++)
                list.Add(id);
            return list;
        }

        public static CardInstance PutCompanionInField(MatchRunner runner, int playerId, CardPrinting printing)
        {
            var match = runner.Match;
            var player = match.GetPlayer(playerId);
            var card = MatchSetup.CreateInstance(match, new InMemoryCardDatabase(new[] { printing }), printing.Id, playerId, Zone.Field);
            card.Ready = true;
            card.Exhausted = false;
            player.Field.Add(card);
            return card;
        }

        public static void PassAll(MatchRunner runner)
        {
            var living = runner.Match.LivingPlayers();
            for (var i = 0; i < living.Count; i++)
                runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = living[i].Id });
        }
    }
}
