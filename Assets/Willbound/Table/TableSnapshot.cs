using System.Collections.Generic;
using Willbound.Engine;

namespace Willbound.Table
{
    /// <summary>
    /// Last known match frame for the local seat. Built from Engine B Match (offline)
    /// or JSON from Engine C (online). TableBinder is the sole reader of raw JSON.
    /// </summary>
    public sealed class TableSnapshot
    {
        public int LocalPlayerId;
        public Phase Phase;
        public ClashPhase ClashPhase;
        public int ActivePlayerId;
        public int PriorityPlayerId;
        public bool ClashLocked;
        public IReadOnlyList<int> DeclareQueue = System.Array.Empty<int>();
        public int LocalWill;
        public int LocalWorth;
        public int LocalHonor;
        public IReadOnlyList<CardSnapshot> LocalHand = System.Array.Empty<CardSnapshot>();
        public IReadOnlyList<CardSnapshot> LocalField = System.Array.Empty<CardSnapshot>();
        public IReadOnlyList<CardSnapshot> OpponentField = System.Array.Empty<CardSnapshot>();
        public IReadOnlyList<CardSnapshot> LocalWillwell = System.Array.Empty<CardSnapshot>();
        public IReadOnlyList<CardSnapshot> OpponentWillwell = System.Array.Empty<CardSnapshot>();
        public CardSnapshot[] Store = new CardSnapshot[7];
        public int StackCount;
    }

    public sealed class CardSnapshot
    {
        public int InstanceId;
        public string PrintingId;
        public string Name;
        public CardType Type;
        public int WillCost;
        public int StoreWorth;
        public int Strike;
        public int Guard;
        public int Health;
        public int CurrentHealth;
        public bool Exhausted;
        public bool Ready;
        public Zone Zone;
        public int ControllerId;
    }
}
