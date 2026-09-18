using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public static class StoreCardPricing
    {
        public const int DefaultStoreWorth = 1;

        public static int GetStoreWorth(CardView card, StoreView storeView = null, TableMatchBridge bridge = null)
        {
            if (card == null)
                return DefaultStoreWorth;

            if (bridge != null && bridge.IsActive && storeView != null)
            {
                var slot = storeView.IndexOf(card);
                if (slot >= 0)
                {
                    var storeCard = bridge.Runner.Match.Store[slot];
                    if (storeCard?.Printing != null)
                        return storeCard.Printing.StoreWorth;
                }
            }

            return DefaultStoreWorth;
        }

        public static string GetCardLabel(CardView card)
        {
            if (card == null || card.FrontTexture == null)
                return "Card";

            return CardCatalog.FormatCardBackName(card.FrontTexture.name);
        }
    }
}
