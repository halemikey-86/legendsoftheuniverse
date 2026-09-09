namespace LegendsOfTheUniverse.Rules
{
    public sealed class CardAbilityDef
    {
        public string Name { get; set; }
        public AbilityTiming Timing { get; set; }
        public string Text { get; set; }
        public bool OncePerGame { get; set; }
        public bool OncePerTurn { get; set; }
        public bool Optional { get; set; }
        public int? CostWill { get; set; }
    }
}
