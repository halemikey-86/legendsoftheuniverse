using Willbound.Engine;

namespace LegendsOfTheUniverse.Presentation.EngineBridge
{
    /// <summary>
    /// Legal-play checks against public <see cref="Match"/> state.
    /// Mirrors engine ValidatePlayCard without calling plugin APIs that are not on the shipped DLL.
    /// </summary>
    public static class EnginePlayRules
    {
        public static bool CanPlayFromHand(Match match, int playerId, int cardInstanceId)
        {
            if (match == null || match.WinnerId.HasValue)
                return false;
            if (playerId != match.PriorityPlayerId)
                return false;

            var player = match.GetPlayer(playerId);
            if (player == null || player.Lost)
                return false;

            var card = match.GetCard(cardInstanceId);
            if (card?.Printing == null || !player.Hand.Contains(card))
                return false;

            var printing = card.Printing;
            if (printing.Type == CardType.WillSite)
            {
                if (match.Phase != Phase.Site || player.Id != match.ActivePlayerId)
                    return false;
                if (player.SitesPlayedThisTurn >= 1)
                    return false;
            }
            else if (printing.Type == CardType.Algorithm)
            {
                if (player.Id != match.ActivePlayerId)
                    return false;
            }
            else if (printing.Type == CardType.Surge || HasTiming(card, Timing.Now))
            {
                // Now is legal with priority.
            }
            else if (player.Id != match.ActivePlayerId || match.Phase != Phase.Main)
            {
                return false;
            }

            return player.Will >= printing.WillCost;
        }

        public static bool HasTiming(CardInstance card, Timing timing)
        {
            var abilities = card.Printing?.Abilities;
            if (abilities == null)
                return false;

            for (var i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].Timing == timing)
                    return true;
            }

            return false;
        }
    }
}
