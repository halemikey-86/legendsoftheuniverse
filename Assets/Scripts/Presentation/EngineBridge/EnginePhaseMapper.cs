using LegendsOfTheUniverse.Rules;
using Willbound.Engine;

namespace LegendsOfTheUniverse.Presentation.EngineBridge
{
    public static class EnginePhaseMapper
    {
        public static TurnStep ToTurnStep(Phase phase)
        {
            switch (phase)
            {
                case Phase.Start:
                case Phase.Site:
                    return TurnStep.WillSite;
                case Phase.Main:
                    return TurnStep.Main;
                case Phase.Clash:
                    return TurnStep.Clash;
                case Phase.End:
                    return TurnStep.Clash;
                default:
                    return TurnStep.Main;
            }
        }

        public static bool StoreAllowed(Phase phase, Match match, int localPlayerId)
        {
            if (phase != Phase.Main)
                return false;
            if (match.ActivePlayerId != localPlayerId)
                return false;
            var player = match.GetPlayer(localPlayerId);
            return player != null && player.StoreActionsThisTurn < 1;
        }
    }
}
