using System.Collections.Generic;

namespace LegendsOfTheUniverse.Rules
{
    public sealed class CardSpellDef
    {
        public string Name { get; set; }
        public int Cost { get; set; }
        public string Text { get; set; }
    }

    public sealed class CardDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SetName { get; set; }
        public string Number { get; set; }
        public string Series { get; set; }
        public CardType Type { get; set; }
        public string DisplayType { get; set; }
        public string Subtype { get; set; }
        public CardRole? Role { get; set; }
        public string Pack { get; set; }
        public int? PlayCost { get; set; }
        public int? StoreCost { get; set; }
        public int? Strike { get; set; }
        public int? Guard { get; set; }
        public int? Health { get; set; }
        public bool Universe { get; set; }
        public bool StartsInPlay { get; set; }
        public string FrontImagePath { get; set; }
        public string BackImagePath { get; set; }
        public List<CardAbilityDef> Abilities { get; } = new();
        public List<CardSpellDef> Spells { get; } = new();
        public List<string> Static { get; } = new();
        public List<string> Triggers { get; } = new();
        public List<string> TokensMade { get; } = new();
    }
}
