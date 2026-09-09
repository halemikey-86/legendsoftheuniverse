using System.Collections.Generic;

namespace LegendsOfTheUniverse.Rules
{
    public sealed class PlayerState
    {
        public PlayerState(int playerId, string displayName)
        {
            PlayerId = playerId;
            DisplayName = displayName;
        }

        public int PlayerId { get; }
        public string DisplayName { get; }
        public bool IsEliminated { get; set; }

        public int WillPool { get; set; }
        public int Worth { get; set; } = GameConstants.StartingWorth;
        public int Honor { get; set; } = GameConstants.StartingHonor;

        public bool HasTakenStoreActionThisTurn { get; set; }
        public bool HasPlayedWillSiteThisTurn { get; set; }
        public bool HasPressedOrHeldThisTurn { get; set; }

        public List<CardInstance> Deck { get; } = new();
        public List<CardInstance> Hand { get; } = new();
        public List<CardInstance> Field { get; } = new();
        public List<CardInstance> Willwell { get; } = new();
        public List<CardInstance> OutOfPlay { get; } = new();

        public CardInstance Icon { get; set; }

        public int DeckCount => Deck.Count;

        public bool CanAddToDeck => DeckCount < GameConstants.MaxDeckSize;
    }
}
