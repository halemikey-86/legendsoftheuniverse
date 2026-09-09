using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    public static class CardCatalog
    {
        const string CardsFolder = "Assets/Cards";
        public const string LegendaryIconPrefix = "Legendary Icon";

        static readonly HashSet<string> ExcludedNames = new()
        {
            "TableBackground",
            "Card Back",
            "BackOfCard",
        };

        public static bool IsLegendaryIcon(string cardName)
        {
            return !string.IsNullOrEmpty(cardName)
                && cardName.StartsWith(LegendaryIconPrefix, System.StringComparison.Ordinal);
        }

        public static IReadOnlyList<Texture2D> LoadAllCardFronts()
        {
            var results = new List<Texture2D>();
            var seen = new HashSet<Texture2D>();

#if UNITY_EDITOR
            foreach (var guid in AssetDatabase.FindAssets("", new[] { CardsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension is not (".png" or ".jpg" or ".jpeg"))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (ShouldExcludeFromDeck(fileName))
                    continue;

                var texture = LoadTextureFromPath(path);
                if (texture != null && seen.Add(texture))
                    results.Add(texture);
            }
#else
            foreach (var texture in Resources.LoadAll<Texture2D>("Cards"))
            {
                if (texture == null || ShouldExcludeFromDeck(texture.name))
                    continue;

                if (seen.Add(texture))
                    results.Add(texture);
            }
#endif

            results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return results;
        }

        public static IReadOnlyList<Texture2D> LoadLegendaryIcons()
        {
            var results = new List<Texture2D>();
            var seen = new HashSet<Texture2D>();

#if UNITY_EDITOR
            foreach (var guid in AssetDatabase.FindAssets("", new[] { CardsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension is not (".png" or ".jpg" or ".jpeg"))
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (!IsLegendaryIcon(fileName))
                    continue;

                var texture = LoadTextureFromPath(path);
                if (texture != null && seen.Add(texture))
                    results.Add(texture);
            }
#else
            foreach (var texture in Resources.LoadAll<Texture2D>("Cards"))
            {
                if (texture == null || !IsLegendaryIcon(texture.name))
                    continue;

                if (seen.Add(texture))
                    results.Add(texture);
            }
#endif

            results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return results;
        }

        public static IReadOnlyList<Texture2D> LoadDiscoveryCards()
        {
            var results = new List<Texture2D>(LoadAllCardFronts());
            var seen = new HashSet<Texture2D>(results);

            foreach (var icon in LoadLegendaryIcons())
            {
                if (icon != null && seen.Add(icon))
                    results.Add(icon);
            }

            results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return results;
        }

        public static IReadOnlyList<Texture2D> LoadLegendaryIconChoices(int count = 3)
        {
            var pool = new List<Texture2D>(LoadLegendaryIcons());
            if (pool.Count <= count)
                return pool;

            Shuffle(pool);

            return pool.GetRange(0, count);
        }

        static bool ShouldExcludeFromDeck(string cardName)
        {
            return ExcludedNames.Contains(cardName) || IsLegendaryIcon(cardName);
        }

        static void Shuffle(List<Texture2D> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

#if UNITY_EDITOR
        public static Texture2D LoadTextureFromPath(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Texture2D textureAsset)
                    return textureAsset;

                if (asset is Sprite sprite && sprite.texture != null)
                    return sprite.texture;
            }

            return null;
        }
#endif
    }
}
