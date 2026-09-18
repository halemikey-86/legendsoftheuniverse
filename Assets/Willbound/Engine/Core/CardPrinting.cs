using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class CardPrinting
    {
        public string SchemaVersion;
        public string Id;
        public string Set;
        public string Number;
        public string Name;
        public CardType Type;
        public string Subtype;
        public string Role;
        public int WillCost;
        public int StoreWorth;
        public int Strike;
        public int Guard;
        public int Health;
        public bool StartsInPlay;
        public List<string> Keywords = new List<string>();
        public List<AbilityPrinting> Abilities = new List<AbilityPrinting>();
    }

    public sealed class AbilityPrinting
    {
        public string Name;
        public int CostWill;
        public int CostHonor;
        public Timing Timing;
        public bool OncePerTurn;
        public bool OncePerGame;
        public bool OncePerClash;
        public string Text;
        public List<EffectPrinting> Effects = new List<EffectPrinting>();
    }

    public sealed class EffectPrinting
    {
        public EffectOp Op;
        public EffectWhen When;
        public string Keyword;
        public string Counter;
        public int Amount;
        public string Target;
        public string Filter;
        public string Duration;
        public List<EffectPrinting> Then = new List<EffectPrinting>();
        public Dictionary<string, string> Params = new Dictionary<string, string>();
    }
}
