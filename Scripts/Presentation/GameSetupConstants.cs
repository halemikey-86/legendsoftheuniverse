namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Opening setup: store row and player hand are dealt from a shared pool first,
    /// then the remainder becomes the supply pile. Legendary Icon pick comes last.
    /// </summary>
    public static class GameSetupConstants
    {
        public const int OpeningHandSize = 7;
        public const int SupplyDeckSize = 33;
        public const int StoreSlotCount = 7;
        public const int PlayerCardTotal = OpeningHandSize + SupplyDeckSize;
        public const int SetupPoolSize = PlayerCardTotal + StoreSlotCount;
        public const int LegendaryIconChoices = 5;
    }
}
