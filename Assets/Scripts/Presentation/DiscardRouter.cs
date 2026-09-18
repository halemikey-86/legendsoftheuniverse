using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Routes played one-shot cards (Surges, Algorithms) to the discard pile on the mat.
    /// </summary>
    public static class DiscardRouter
    {
        public static bool IsDiscardCard(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
                return false;

            var lower = textureName.ToLowerInvariant();
            return lower.Contains("surge") || lower.Contains("algorithm");
        }

        public static void RouteToDiscard(CardView card)
        {
            if (card == null)
                return;

            PlaymatZonesView.Instance?.AddToDiscard(card);
        }
    }
}
