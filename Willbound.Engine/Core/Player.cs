using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class Player
    {
        public int Id;
        public string Name;
        public int Seat;
        public int Will;
        public int Worth;
        public int Honor;
        public bool Lost;
        public List<CardInstance> Deck = new List<CardInstance>();
        public List<CardInstance> Hand = new List<CardInstance>();
        public List<CardInstance> Field = new List<CardInstance>();
        public List<CardInstance> Willwell = new List<CardInstance>();
        public CardInstance Icon;
        public int StoreActionsThisTurn;
        public int SitesPlayedThisTurn;
        public bool DeclaredClashThisTurn;
        public HashSet<string> OncePerGameFlags = new HashSet<string>();

        public bool IsAlive => !Lost && Icon != null && Icon.CurrentHealth > 0;
    }
}
