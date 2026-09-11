namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Opening setup: player hand is dealt from a shared pool first, then the remainder
    /// becomes the supply pile. The store opens at round start; unpicked legendary icons
    /// are shuffled back into the supply after the icon pick.
    /// </summary>
    public static class GameSetupConstants
    {
        public const int OpeningHandSize = 7;
        public const int SupplyDeckSize = 33;
        public const int StoreSlotCount = 7;
        public const int PlayerCardTotal = OpeningHandSize + SupplyDeckSize;
        public const int SetupPoolSize = PlayerCardTotal;
        public const int LegendaryIconChoices = 5;
        public const int OpeningHandRedrawsPerCard = 1;
    }
}
