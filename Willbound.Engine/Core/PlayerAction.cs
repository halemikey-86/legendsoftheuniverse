namespace Willbound.Engine
{
    public sealed class PlayerAction
    {
        public PlayerActionKind Kind;
        public int PlayerId;
        public int? CardInstanceId;
        public int? TargetInstanceId;
        public int? StackObjectId;
        public int? StoreSlotIndex;
        public int? HandCardInstanceId;
        public int? AbilityIndex;
        public int? DoubleteamHelperId;
        public StoreActionKind? StoreKind;
        public int PaidWill;
        public int PaidWorth;
    }

    public sealed class ApplyResult
    {
        public bool Success;
        public string Error;
        public Match Match;
        public GameEvent[] Events;

        public static ApplyResult Ok(Match match, GameEvent[] events) =>
            new ApplyResult { Success = true, Match = match, Events = events ?? System.Array.Empty<GameEvent>() };

        public static ApplyResult Fail(string error, Match match) =>
            new ApplyResult { Success = false, Error = error, Match = match, Events = System.Array.Empty<GameEvent>() };
    }
}
