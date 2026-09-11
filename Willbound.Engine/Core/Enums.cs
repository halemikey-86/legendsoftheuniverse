namespace Willbound.Engine
{
    public enum CardType
    {
        Icon,
        Companion,
        Relic,
        Bond,
        Surge,
        Algorithm,
        WillSite,
        Token,
    }

    public enum Zone
    {
        Deck,
        Hand,
        Field,
        Willwell,
        Store,
        Supply,
        Removed,
        Banished,
        Stack,
        ClashQueue,
    }

    public enum Phase
    {
        Setup,
        Start,
        Site,
        Main,
        Clash,
        End,
        Halt,
    }

    public enum ClashPhase
    {
        None,
        C0_Begin,
        C1_ActiveDeclare,
        C2_Answers,
        C3_Interact,
        C4_AggressionWave,
        C5_Skip,
        C6_NormalWave,
        C7_Aftereffects,
        C8_End,
    }

    public enum StackObjectType
    {
        PlayCard,
        Activate,
        Trigger,
        Press,
        StoreAction,
        Silence,
    }

    public enum StoreActionKind
    {
        Buy,
        Sell,
        Trade,
        Keep,
        List,
        Row,
    }

    public enum Timing
    {
        Static,
        Now,
        Then,
        Activated,
        Enter,
        Clash,
        Removed,
        Trigger,
    }

    public enum Keyword
    {
        Aggression,
        Bazerk,
        Gashing,
        HeavyHitter,
        Hunted,
        Aftereffect,
        Still,
        Drain,
        Doubleteam,
        Sealed,
        Closed,
        Toll,
        Silence,
        Now,
        Then,
        Absolute,
        StandAgain,
    }

    public enum EffectOp
    {
        DealDamage,
        Mend,
        Draw,
        GainWill,
        GainWorth,
        GainHonor,
        PutCounter,
        RemoveCounter,
        CreateToken,
        MoveZone,
        AttachBond,
        DetachBond,
        Exhaust,
        Ready,
        GrantKeyword,
        GrantKeywordOnDeclare,
        GiveFlag,
        Silence,
        PreventDamage,
        ReplaceRemove,
        SearchSupply,
        StoreBuy,
        StoreSell,
        StoreTrade,
        StoreList,
        StoreRow,
        ChooseTarget,
        ForEach,
        If,
        Unless,
    }

    public enum EffectWhen
    {
        Immediate,
        OnEnter,
        OnStart,
        OnEnd,
        OnClashBegin,
        OnClashEnd,
        OnHold,
        OnPressDeclared,
        OnPressDealtDamage,
        OnPressRemovedBody,
        OnRemoved,
        OnBanished,
    }

    public enum PlayerActionKind
    {
        Pass,
        PlayCard,
        Activate,
        DeclarePress,
        DeclareHold,
        Answer,
        StoreBuy,
        StoreSell,
        StoreTrade,
        StoreKeep,
        StoreList,
        StoreRow,
        Silence,
    }

    public enum LossReason
    {
        EmptyDeckDraw,
        IconZeroHealth,
        HuntedTen,
        CardEffect,
    }

    public enum EventKind
    {
        MatchStarted,
        RoundChanged,
        TurnStarted,
        PhaseChanged,
        PriorityChanged,
        WillSet,
        WillPaid,
        WorthChanged,
        HonorChanged,
        CardDrew,
        CardMoved,
        PermanentEntered,
        PermanentLeft,
        BondAttached,
        Exhausted,
        Readied,
        StackPushed,
        StackPopped,
        Silenced,
        PressDeclared,
        PressAnswered,
        HoldDeclared,
        ClashLocked,
        DamageDealt,
        CounterPut,
        HealthChanged,
        KeywordGranted,
        StoreRefilled,
        PlayerLost,
        PlayerWon,
        CheckRan,
    }
}
