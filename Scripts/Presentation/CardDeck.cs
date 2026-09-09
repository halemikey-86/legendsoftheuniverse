using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public static class CardDeck
    {
        static readonly List<Texture2D> drawPile = new();

        public static int Remaining => drawPile.Count;
        public static Texture2D SelectedLegendaryIcon { get; private set; }
        public static string SelectedLegendaryName => SelectedLegendaryIcon != null ? SelectedLegendaryIcon.name : null;

        public static event System.Action<int> CountChanged;

        public static void SetSelectedLegendary(Texture2D iconFront)
        {
            SelectedLegendaryIcon = iconFront;
        }

        public static void ResetAndShuffle()
        {
            drawPile.Clear();
            drawPile.AddRange(CardCatalog.LoadAllCardFronts());
            Shuffle(drawPile);

            if (drawPile.Count == 0)
                Debug.LogWarning("CardDeck: No card fronts found in Assets/Cards.");

            NotifyCountChanged();
        }

        public static void ClearSession()
        {
            drawPile.Clear();
            SelectedLegendaryIcon = null;
            NotifyCountChanged();
        }

        public static List<Texture2D> Draw(int count)
        {
            if (drawPile.Count == 0)
                ResetAndShuffle();

            var drawn = new List<Texture2D>(count);
            var toDraw = Mathf.Min(count, drawPile.Count);

            for (var i = 0; i < toDraw; i++)
            {
                drawn.Add(drawPile[0]);
                drawPile.RemoveAt(0);
            }

            if (drawn.Count < count)
                Debug.LogWarning($"CardDeck: Requested {count} cards but only {drawn.Count} remain in the deck.");

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
