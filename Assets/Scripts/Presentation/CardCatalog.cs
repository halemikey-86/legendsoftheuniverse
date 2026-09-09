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
        const string CardBacksFolder = "Assets/Cards/CardBacks";
        public const string LegendaryIconPrefix = "Legendary Icon";
        const string CardBackPrefix = "CardBack";

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
                if (AssetDatabase.IsValidFolder(path) || IsCardBackAssetPath(path))
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

        public static IReadOnlyList<Texture2D> LoadLegendaryIconChoices(
            int count = GameSetupConstants.LegendaryIconChoices)
        {
            var pool = new List<Texture2D>(LoadLegendaryIcons());
            if (pool.Count <= count)
                return pool;

            Shuffle(pool);

            return pool.GetRange(0, count);
        }

        public static IReadOnlyList<Texture2D> LoadCardBacks()
        {
            var results = new List<Texture2D>();
            var seen = new HashSet<Texture2D>();

#if UNITY_EDITOR
            foreach (var guid in AssetDatabase.FindAssets("", new[] { CardBacksFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension is not (".png" or ".jpg" or ".jpeg"))
                    continue;

                var texture = LoadTextureFromPath(path);
                if (texture != null && seen.Add(texture))
                    results.Add(texture);
            }
#else
            foreach (var texture in Resources.LoadAll<Texture2D>("CardBacks"))
            {
                if (texture != null && seen.Add(texture))
                    results.Add(texture);
            }
#endif

            results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return results;
        }

        public static Texture2D GetDefaultCardBack()
        {
            var backs = LoadCardBacks();
            return backs.Count > 0 ? backs[0] : null;
        }

        public static string FormatCardBackName(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
                return "Card Back";

            if (textureName.StartsWith(CardBackPrefix + "_", System.StringComparison.Ordinal))
                return textureName.Substring(CardBackPrefix.Length + 1).Replace('_', ' ');

            if (textureName.StartsWith(CardBackPrefix, System.StringComparison.Ordinal))
                return textureName.Substring(CardBackPrefix.Length).TrimStart('_', ' ').Replace('_', ' ');

            return textureName;
        }

        static bool ShouldExcludeFromDeck(string cardName)
        {
            return ExcludedNames.Contains(cardName)
                || IsLegendaryIcon(cardName)
                || cardName.StartsWith(CardBackPrefix, System.StringComparison.Ordinal);
        }

        static bool IsCardBackAssetPath(string path)
        {
            return path.Replace('\\', '/').Contains("/CardBacks/", System.StringComparison.OrdinalIgnoreCase);
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
