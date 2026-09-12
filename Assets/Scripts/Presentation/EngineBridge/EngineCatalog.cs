using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using Willbound.Engine;

namespace LegendsOfTheUniverse.Presentation.EngineBridge
{
    /// <summary>
    /// Loads real card data from Assets/StreamingAssets/Cards/*.json into the rules kernel.
    /// Two JSON shapes are supported: the canonical Engine B shape (sets[].cards[] with
    /// willCost/storeWorth/abilities[]) handled by Willbound.Engine.CardPrintingLoader, and a
    /// compact shape (top-level cards[] with will/worth/text) used by the single-set exports.
    /// </summary>
    public static class EngineCatalog
    {
        const string CardsFolder = "Cards";
        const string DefaultDeckFile = "WILLBOUND_Goblin_King_cards.json"; // Fallback when the chosen Icon has no deck file of its own.

        /// <summary>Maps a hero Icon's printing id to the deck file its companions/relics/etc. live in.</summary>
        static readonly Dictionary<string, string> DeckFileByIconId = new(StringComparer.OrdinalIgnoreCase)
        {
            { "gk-01", "WILLBOUND_Goblin_King_cards.json" },
            { "rp-01", "WILLBOUND_Rise_of_Pride_cards.json" },
            { "rm-01", "WILLBOUND_River_Merchant_cards.json" },
        };

        /// <summary>Maps a compact-schema source file to the Assets/Cards art subfolder that matches it by name.</summary>
        static readonly Dictionary<string, string> ArtFolderBySourceFile = new()
        {
            { "WILLBOUND_Goblin_King_cards.json", "Goblin King Set" },
            { "WILLBOUND_Rise_of_Pride_cards.json", "RiseOfPride" },
            { "WILLBOUND_River_Merchant_cards.json", "RiverMerchant" },
        };

        /// <summary>
        /// Sets whose art folder uses a "&lt;two-digit card number&gt;_..." filename convention instead of matching
        /// the JSON's name/fileKey (its art was authored with different — often trademarked — character names).
        /// Only Mystery Kitchen/Scooby-Doo has been verified to align 1:1 by position across all 60 cards; the
        /// other AllSets sets (Phantom Mesa, Mostorno Cycle, Politics of Time, Figures of History) do not have a
        /// reliable corresponding art folder and are intentionally left unmapped.
        /// </summary>
        static readonly Dictionary<string, string> NumberedArtFolderBySet = new()
        {
            { "Mystery Kitchen", "Scooby-Doo" },
        };

        static List<CardPrinting> cachedPrintings;
        static readonly Dictionary<string, string> cardArtFolder = new();
        static readonly Dictionary<string, string> cardArtFileKey = new();
        static readonly Dictionary<string, Texture2D> cardArtCache = new();

        public static IReadOnlyList<CardPrinting> LoadPrintings()
        {
            if (cachedPrintings != null)
                return cachedPrintings;

            var printings = new List<CardPrinting>();
            var cardsDir = Path.Combine(Application.streamingAssetsPath, CardsFolder);

            if (Directory.Exists(cardsDir))
            {
                foreach (var path in Directory.GetFiles(cardsDir, "*.json"))
                {
                    try
                    {
                        printings.AddRange(ParseCardFile(File.ReadAllText(path), Path.GetFileName(path)));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"EngineCatalog: failed to parse {path}: {ex.Message}");
                    }
                }
            }

            if (printings.Count == 0)
            {
                Debug.LogWarning($"EngineCatalog: no printings found in {cardsDir}. Falling back to bootstrap printings.");
                printings = BootstrapPrintings();
            }

            cachedPrintings = printings;
            return printings;
        }

        /// <summary>Resolves the real card art for a loaded printing, if its source set has a matching Assets/Cards folder.</summary>
        public static Texture2D GetCardArt(CardPrinting printing)
        {
            if (printing == null || string.IsNullOrEmpty(printing.Id))
                return null;

            if (cardArtCache.TryGetValue(printing.Id, out var cached))
                return cached;

            var texture = ResolveCardArt(printing);
            cardArtCache[printing.Id] = texture;
            return texture;
        }

        static Texture2D ResolveCardArt(CardPrinting printing)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(printing.Set) && NumberedArtFolderBySet.TryGetValue(printing.Set, out var numberedFolder))
                return ResolveArtByNumberPrefix(numberedFolder, printing.Number);

            if (!cardArtFolder.TryGetValue(printing.Id, out var folder))
                return null;

            var folderPath = $"Assets/Cards/{folder}";
            var wantedKey = Normalize(cardArtFileKey.TryGetValue(printing.Id, out var fileKey) ? fileKey : null);
            var wantedName = Normalize(printing.Name);

            string bestPath = null;
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var normalizedFileName = Normalize(Path.GetFileNameWithoutExtension(path));

                if (!string.IsNullOrEmpty(wantedKey) && normalizedFileName == wantedKey)
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (bestPath == null && !string.IsNullOrEmpty(wantedName) && normalizedFileName.Contains(wantedName))
                    bestPath = path;
            }

            return bestPath != null ? UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(bestPath) : null;
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        /// <summary>Matches art by the leading "NN_" filename prefix against a "NN/total" CardPrinting.Number.</summary>
        static Texture2D ResolveArtByNumberPrefix(string folder, string number)
        {
            if (string.IsNullOrEmpty(number))
                return null;

            var slashIndex = number.IndexOf('/');
            var numberPart = slashIndex >= 0 ? number.Substring(0, slashIndex) : number;
            if (!int.TryParse(numberPart, out var parsed))
                return null;

            var prefix = parsed.ToString("00") + "_";
            var folderPath = $"Assets/Cards/{folder}";

            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return null;
        }
#endif

        static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var builder = new System.Text.StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (char.IsLetterOrDigit(c))
                    builder.Append(char.ToLowerInvariant(c));
            }

            return builder.Length > 0 ? builder.ToString() : null;
        }

        static List<CardPrinting> ParseCardFile(string json, string sourceFileName)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("sets", out var sets))
                return ParseNestedSets(sets);

            if (root.TryGetProperty("cards", out var cards) && cards.GetArrayLength() > 0 && IsCompactCard(cards[0]))
                return ParseCompactCards(root, cards, sourceFileName);

            return CardPrintingLoader.ParseMany(json);
        }

        static bool IsCompactCard(JsonElement card) =>
            card.TryGetProperty("will", out _) || card.TryGetProperty("worth", out _);

        static List<CardPrinting> ParseNestedSets(JsonElement sets)
        {
            var rawCards = new List<string>();
            foreach (var set in sets.EnumerateArray())
            {
                AppendRawElements(set, "cards", rawCards);
                AppendRawElements(set, "tokens", rawCards);
                AppendRawElements(set, "extras", rawCards);
            }

            if (rawCards.Count == 0)
                return new List<CardPrinting>();

            var combined = "[" + string.Join(",", rawCards) + "]";
            return CardPrintingLoader.ParseMany(combined);
        }

        static void AppendRawElements(JsonElement parent, string propertyName, List<string> into)
        {
            if (!parent.TryGetProperty(propertyName, out var array))
                return;

            foreach (var element in array.EnumerateArray())
                into.Add(element.GetRawText());
        }

        static List<CardPrinting> ParseCompactCards(JsonElement root, JsonElement cards, string sourceFileName)
        {
            var setName = root.TryGetProperty("set", out var setEl) ? GetString(setEl, "name") : null;
            ArtFolderBySourceFile.TryGetValue(sourceFileName, out var artFolder);

            var result = new List<CardPrinting>();
            foreach (var card in cards.EnumerateArray())
                result.Add(ParseCompactCard(card, setName, artFolder));

            if (root.TryGetProperty("token", out var tokenEl))
                result.Add(ParseCompactCard(tokenEl, setName, artFolder));

            return result;
        }

        static CardPrinting ParseCompactCard(JsonElement card, string setName, string artFolder)
        {
            var type = ParseCompactType(GetString(card, "type"));
            var printing = new CardPrinting
            {
                Id = GetString(card, "id") ?? GetString(card, "fileKey"),
                Set = setName,
                Name = GetString(card, "name"),
                Type = type,
                WillCost = GetNullableInt(card, "will") ?? 0,
                StoreWorth = GetNullableInt(card, "worth") ?? 0,
                Strike = GetNullableInt(card, "strike") ?? 0,
                Guard = GetNullableInt(card, "guard") ?? 0,
                Health = GetNullableInt(card, "health") ?? 0,
                StartsInPlay = type == CardType.Icon,
            };

            if (!string.IsNullOrEmpty(artFolder) && !string.IsNullOrEmpty(printing.Id))
            {
                cardArtFolder[printing.Id] = artFolder;
                var fileKey = GetString(card, "fileKey");
                if (!string.IsNullOrEmpty(fileKey))
                    cardArtFileKey[printing.Id] = fileKey;
            }

            if (card.TryGetProperty("keywords", out var keywords))
            {
                foreach (var kw in keywords.EnumerateArray())
                {
                    var text = kw.GetString();
                    if (!string.IsNullOrEmpty(text))
                        printing.Keywords.Add(text);
                }
            }

            var abilityText = GetString(card, "text");
            if (!string.IsNullOrEmpty(abilityText))
            {
                printing.Abilities.Add(new AbilityPrinting
                {
                    Name = printing.Name,
                    Timing = type == CardType.Surge ? Timing.Now : Timing.Static,
                    Text = abilityText,
                });
            }

            return printing;
        }

        static CardType ParseCompactType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return CardType.Companion;

            switch (value.Trim().ToLowerInvariant())
            {
                case "icon": return CardType.Icon;
                case "companion": return CardType.Companion;
                case "relic": return CardType.Relic;
                case "bond": return CardType.Bond;
                case "surge": return CardType.Surge;
                case "algorithm": return CardType.Algorithm;
                case "will site":
                case "willsite": return CardType.WillSite;
                case "token": return CardType.Token;
                default: return CardType.Companion;
            }
        }

        static string GetString(JsonElement el, string name) =>
            el.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null ? prop.GetString() : null;

        static int? GetNullableInt(JsonElement el, string name) =>
            el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : (int?)null;

        public static List<string> DefaultDeck(string iconId, int count = 30, string fallbackCardId = "VANILLA-COMPANION")
        {
            var pool = BuildDefaultDeckPool(iconId);
            if (pool.Count == 0)
            {
                var legacy = new List<string>(count);
                for (var i = 0; i < count; i++)
                    legacy.Add(fallbackCardId);
                return legacy;
            }

            Shuffle(pool);

            var result = new List<string>(count);
            for (var i = 0; i < count; i++)
                result.Add(pool[i % pool.Count]);
            return result;
        }

        static List<string> BuildDefaultDeckPool(string iconId)
        {
            var fileName = !string.IsNullOrEmpty(iconId) && DeckFileByIconId.TryGetValue(iconId, out var mapped)
                ? mapped
                : DefaultDeckFile;
            var path = Path.Combine(Application.streamingAssetsPath, CardsFolder, fileName);
            if (!File.Exists(path))
                return new List<string>();

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (!root.TryGetProperty("cards", out var cards))
                return new List<string>();

            var pool = new List<string>();
            foreach (var card in cards.EnumerateArray())
            {
                if (string.Equals(GetString(card, "type"), "Icon", StringComparison.OrdinalIgnoreCase))
                    continue;

                var id = GetString(card, "id");
                if (string.IsNullOrEmpty(id))
                    continue;

                var qty = GetNullableInt(card, "qty") ?? 1;
                for (var i = 0; i < qty; i++)
                    pool.Add(id);
            }

            return pool;
        }

        static void Shuffle(List<string> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Last-resort placeholder data, used only if StreamingAssets/Cards is missing or empty.</summary>
        static List<CardPrinting> BootstrapPrintings()
        {
            return new List<CardPrinting>
            {
                VanillaIcon(),
                VanillaCompanion(),
                VanillaStriker(),
                WillSite(),
                SilenceSurge(),
            };
        }

        static CardPrinting VanillaIcon() => new CardPrinting
        {
            Id = "VANILLA-ICON",
            Name = "Vanilla Icon",
            Type = CardType.Icon,
            Strike = 3,
            Guard = 4,
            Health = 8,
            StartsInPlay = true,
        };

        static CardPrinting VanillaCompanion() => new CardPrinting
        {
            Id = "VANILLA-COMPANION",
            Name = "Vanilla Companion",
            Type = CardType.Companion,
            WillCost = 1,
            StoreWorth = 1,
            Strike = 2,
            Guard = 2,
            Health = 2,
        };

        static CardPrinting VanillaStriker() => new CardPrinting
        {
            Id = "VANILLA-STRIKER",
            Name = "Vanilla Striker",
            Type = CardType.Companion,
            WillCost = 2,
            StoreWorth = 2,
            Strike = 4,
            Guard = 2,
            Health = 3,
        };

        static CardPrinting WillSite() => new CardPrinting
        {
            Id = "WILL-SITE-1",
            Name = "Training Ground",
            Type = CardType.WillSite,
            WillCost = 0,
            StoreWorth = 1,
        };

        static CardPrinting SilenceSurge() => new CardPrinting
        {
            Id = "SILENCE-NOW",
            Name = "Silence",
            Type = CardType.Surge,
            WillCost = 1,
            StoreWorth = 1,
            Keywords = new List<string> { "Now", "Silence" },
        };
    }
}
