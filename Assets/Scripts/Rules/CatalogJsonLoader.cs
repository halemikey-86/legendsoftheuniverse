using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LegendsOfTheUniverse.Rules
{
    /// <summary>
    /// Loads exported catalog JSON into Engine B. Export from catalog: npm run export:unity
    /// </summary>
    public static class CatalogJsonLoader
    {
        public const string StreamingAssetFileName = "willbound-cards.json";
        public const string ResourcesFileName = "willbound-cards";

        public static bool TryLoadFromStreamingAssets(out IReadOnlyList<CardDef> cards)
        {
            cards = null;
            var path = Path.Combine(Application.streamingAssetsPath, StreamingAssetFileName);
            if (!File.Exists(path))
                return false;

            try
            {
                var json = File.ReadAllText(path);
                cards = EngineJsonParser.ParseCatalogJson(json);
                return cards.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CatalogJsonLoader: could not read {path}: {ex.Message}");
                return false;
            }
        }

        public static bool TryLoadFromResources(out IReadOnlyList<CardDef> cards)
        {
            cards = null;
            var asset = Resources.Load<TextAsset>(ResourcesFileName);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return false;

            cards = EngineJsonParser.ParseCatalogJson(asset.text);
            return cards.Count > 0;
        }

        public static void RegisterInto(Dictionary<string, CardDef> byName)
        {
            if (byName == null)
                throw new ArgumentNullException(nameof(byName));

            IReadOnlyList<CardDef> cards = null;
            if (!TryLoadFromStreamingAssets(out cards) && !TryLoadFromResources(out cards))
                return;

            foreach (var def in cards)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.Name))
                    continue;

                byName[def.Name] = def;
            }

            Debug.Log($"CatalogJsonLoader: registered {cards.Count} card(s) from export.");
        }
    }
}
