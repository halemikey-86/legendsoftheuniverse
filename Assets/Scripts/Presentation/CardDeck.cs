using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public static class CardDeck
    {
        static readonly List<Texture2D> setupPile = new();
        static readonly List<Texture2D> drawPile = new();

        public static int Remaining => drawPile.Count;
        public static int SetupRemaining => setupPile.Count;
        public static Texture2D SelectedLegendaryIcon { get; private set; }
        public static Texture2D SelectedCardBack { get; private set; }
        public static string SelectedLegendaryName => SelectedLegendaryIcon != null ? SelectedLegendaryIcon.name : null;

        public static event System.Action<int> CountChanged;

        public static void SetSelectedLegendary(Texture2D iconFront)
        {
            SelectedLegendaryIcon = iconFront;
        }

        public static void SetSelectedCardBack(Texture2D backTexture)
        {
            SelectedCardBack = backTexture;
        }

        /// <summary>
        /// Build the opening pool: non-Icon cards shuffled, capped to setup size (40 = 7 hand + 33 supply).
        /// </summary>
        public static void PrepareSetupPool()
        {
            setupPile.Clear();
            drawPile.Clear();
            SelectedLegendaryIcon = null;

            setupPile.AddRange(CardCatalog.LoadAllCardFronts());
            Shuffle(setupPile);

            if (setupPile.Count > GameSetupConstants.SetupPoolSize)
                setupPile.RemoveRange(GameSetupConstants.SetupPoolSize, setupPile.Count - GameSetupConstants.SetupPoolSize);

            if (setupPile.Count == 0)
                Debug.LogWarning("CardDeck: No card fronts found in Assets/Cards.");

            NotifyCountChanged();
        }

        /// <summary>
        /// Move undrawn setup cards into the shuffled supply pile.
        /// </summary>
        public static void CommitRemainderToSupply()
        {
            if (setupPile.Count == 0)
            {
                NotifyCountChanged();
                return;
            }

            drawPile.AddRange(setupPile);
            setupPile.Clear();
            Shuffle(drawPile);
            NotifyCountChanged();
        }

        public static void ClearSession()
        {
            setupPile.Clear();
            drawPile.Clear();
            SelectedLegendaryIcon = null;
            SelectedCardBack = null;
            NotifyCountChanged();
        }

        /// <summary>
        /// Return a card to the supply pile (e.g. opening-hand mulligan).
        /// </summary>
        public static void ReturnToSupply(Texture2D front)
        {
            if (front == null)
                return;

            drawPile.Add(front);
            NotifyCountChanged();
        }

        /// <summary>
        /// Add unpicked legendary icons (or any fronts) into the supply and reshuffle.
        /// </summary>
        public static void ReturnManyToSupplyAndShuffle(IReadOnlyList<Texture2D> fronts)
        {
            if (fronts == null || fronts.Count == 0)
                return;

            for (var i = 0; i < fronts.Count; i++)
            {
                if (fronts[i] != null)
                    drawPile.Add(fronts[i]);
            }

            Shuffle(drawPile);
            NotifyCountChanged();
        }

        public static List<Texture2D> Draw(int count)
        {
            if (setupPile.Count > 0)
                return DrawFromList(setupPile, count);

            if (drawPile.Count == 0)
            {
                Debug.LogWarning($"CardDeck: Supply empty — requested {count} card(s).");
                return new List<Texture2D>();
            }

            return DrawFromList(drawPile, count);
        }

        static List<Texture2D> DrawFromList(List<Texture2D> pile, int count)
        {
            var drawn = new List<Texture2D>(count);
            var toDraw = Mathf.Min(count, pile.Count);

            for (var i = 0; i < toDraw; i++)
            {
                drawn.Add(pile[0]);
                pile.RemoveAt(0);
            }

            if (drawn.Count < count)
                Debug.LogWarning($"CardDeck: Requested {count} cards but only {drawn.Count} remain.");

            NotifyCountChanged();
            return drawn;
        }

        static void NotifyCountChanged()
        {
            CountChanged?.Invoke(drawPile.Count);
        }

        static void Shuffle(List<Texture2D> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
