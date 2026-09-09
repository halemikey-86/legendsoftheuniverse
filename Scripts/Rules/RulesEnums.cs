namespace LegendsOfTheUniverse.Rules
{
    public enum CardType
    {
        Icon,
        Companion,
        Relic,
        Bond,
        Surge,
        Will,
        Token,
    }

    public enum ZoneType
    {
        Deck,
        Hand,
        Willwell,
        Field,
        Store,
        Supply,
        OutOfPlay,
        Banished,
        TokenPile,
        UniverseLock,
    }

    public enum TurnStep
    {
        Start,
        WillSite,
        Main,
        Clash,
        End,
    }

    public enum TokenKind
    {
        Wall,
        Hugacef,
    }

    public enum StoreActionKind
    {
        Buy,
        Trade,
        Dump,
    }

    public enum PriorityActionKind
    {
        PlaySurge,
        ActivateSpell,
        Pass,
    }

    public enum UniverseSetKind
    {
        Bands,
        RagonOrbs,
        FinintyGems,
    }
}
