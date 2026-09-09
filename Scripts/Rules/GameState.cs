using System;
using System.Collections.Generic;
using System.Linq;

namespace LegendsOfTheUniverse.Rules
{
    public sealed class GameState
    {
        public GameState(IReadOnlyList<PlayerState> players, int storeSlotCount, int? rngSeed = null)
        {
            if (players == null || players.Count < 2 || players.Count > 10)
                throw new ArgumentException("WILLBOUND supports 2–10 players.");

            if (storeSlotCount < GameConstants.MinStoreSlots || storeSlotCount > GameConstants.MaxStoreSlots)
                throw new ArgumentException($"Store must have {GameConstants.MinStoreSlots}–{GameConstants.MaxStoreSlots} slots.");

            Players = players.ToList();
            StoreSlotCount = storeSlotCount;
            Store = new CardInstance[storeSlotCount];
            Rng = rngSeed.HasValue ? new Random(rngSeed.Value) : new Random();
        }

        public List<PlayerState> Players { get; }
        public List<CardInstance> Supply { get; } = new();
        public CardInstance[] Store { get; }
        public List<CardInstance> Sold { get; } = new();
        public List<CardInstance> Banished { get; } = new();
        public List<CardInstance> TokenPile { get; } = new();
        public List<CardInstance> UniverseLock { get; } = new();

        public GameStack Stack { get; } = new();
        public Random Rng { get; }

        public int Round { get; set; } = 1;
        public int ActivePlayerIndex { get; set; }
        public int PriorityPlayerIndex { get; set; }
        public TurnStep CurrentStep { get; set; } = TurnStep.Start;
        public int StoreSlotCount { get; }

        public bool GameOver { get; set; }
        public int? WinnerPlayerId { get; set; }

        public HashSet<string> OncePerGameLocks { get; } = new();
        public Dictionary<string, bool> OncePerTurnLocks { get; } = new();
        public Dictionary<int, EndlessPressModifierState> EndlessPressModifiers { get; } = new();
        public EndlessClashState EndlessClash { get; set; }

        public long Timestamp { get; private set; }

        public PlayerState ActivePlayer => Players[ActivePlayerIndex];
        public PlayerState PriorityPlayer => Players[PriorityPlayerIndex];

        public int LivingPlayerCount => Players.Count(p => !p.IsEliminated);

        public long NextTimestamp() => ++Timestamp;

        public PlayerState GetPlayer(int playerId)
        {
            return Players.FirstOrDefault(p => p.PlayerId == playerId);
        }

        public int WillGrantForRound()
        {
            return Math.Min(Round, GameConstants.MaxRoundWill);
        }
    }
}
