namespace LegendsOfTheUniverse.Presentation.Menu
{
    public enum MatchMode
    {
        Solo,
        Bot,
    }

    /// <summary>Session launch flags set by the main menu before the table scene loads.</summary>
    public static class MatchLaunch
    {
        public static MatchMode Mode { get; set; } = MatchMode.Bot;
    }
}
