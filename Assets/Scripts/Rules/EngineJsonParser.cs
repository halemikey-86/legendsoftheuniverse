using System;
using System.Collections.Generic;

namespace LegendsOfTheUniverse.Rules
{
    /// <summary>
    /// Parses Engine B catalog JSON (schemaVersion 1.2) from the Postgres admin export.
    /// </summary>
    public static class EngineJsonParser
    {
        [Serializable]
        sealed class CatalogRoot
        {
            public string schemaVersion;
            public EngineCardJson[] cards;
        }

        [Serializable]
        sealed class EngineCardJson
        {
            public string schemaVersion;
            public string id;
            public string set;
            public string number;
            public string series;
            public string name;
            public string type;
            public string subtype;
            public string role;
            public string frame;
            public int willCost;
            public int storeWorth;
            public int honorCost;
            public int honorGain;
            public int strike;
            public int guard;
            public int health;
            public bool startsInPlay;
            public string frontImage;
            public string backImage;
            public string[] keywords;
            public EngineAbilityJson[] abilities;
            public EngineSpellJson[] spells;
        }

        [Serializable]
        sealed class EngineAbilityJson
        {
            public string name;
            public string timing;
            public int costWill;
            public int costHonor;
            public bool oncePerTurn;
            public bool oncePerGame;
            public string target;
            public string text;
        }

        [Serializable]
        sealed class EngineSpellJson
        {
            public string name;
            public int willCost;
            public string text;
        }

        public static IReadOnlyList<CardDef> ParseCatalogJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<CardDef>();

            var root = UnityEngine.JsonUtility.FromJson<CatalogRoot>(WrapArray(json));
            if (root?.cards == null || root.cards.Length == 0)
                return Array.Empty<CardDef>();

            var results = new List<CardDef>(root.cards.Length);
            foreach (var card in root.cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.name))
                    continue;

                results.Add(MapCard(card));
            }

            return results;
        }

        static string WrapArray(string json)
        {
            var trimmed = json.TrimStart();
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
                return json;

            return "{\"cards\":" + json + "}";
        }

        static CardDef MapCard(EngineCardJson card)
        {
            var def = new CardDef
            {
                Id = card.id,
                Name = card.name,
                SetName = card.set,
                Number = card.number,
                Series = card.series,
                Pack = card.set,
                DisplayType = card.type,
                Subtype = card.subtype ?? string.Empty,
                PlayCost = card.willCost,
                StoreCost = card.storeWorth,
                Strike = card.strike,
                Guard = card.guard,
                Health = card.health,
                StartsInPlay = card.startsInPlay,
                FrontImagePath = card.frontImage,
                BackImagePath = card.backImage,
                Universe = string.Equals(card.frame, "universe", StringComparison.OrdinalIgnoreCase),
            };

            def.Type = ParseType(card.type);
            def.Role = ParseRole(card.role);

            if (card.abilities != null)
            {
                foreach (var ability in card.abilities)
                {
                    if (ability == null || string.IsNullOrWhiteSpace(ability.text))
                        continue;

                    var abilityDef = new CardAbilityDef
                    {
                        Name = ability.name ?? string.Empty,
                        Timing = ParseTiming(ability.timing),
                        Text = ability.text,
                        OncePerTurn = ability.oncePerTurn,
                        OncePerGame = ability.oncePerGame,
                        CostWill = ability.costWill > 0 ? ability.costWill : null,
                    };
                    def.Abilities.Add(abilityDef);

                    if (abilityDef.Timing == AbilityTiming.Static)
                        def.Static.Add(abilityDef.Text);
                    else if (abilityDef.Timing == AbilityTiming.Replacement)
                        def.Triggers.Add(abilityDef.Text);
                }
            }

            if (card.spells != null)
            {
                foreach (var spell in card.spells)
                {
                    if (spell == null)
                        continue;

                    def.Spells.Add(new CardSpellDef
                    {
                        Name = spell.name ?? string.Empty,
                        Cost = spell.willCost,
                        Text = spell.text ?? string.Empty,
                    });
                }
            }

            if (card.keywords != null)
            {
                foreach (var keyword in card.keywords)
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                        def.Static.Add(keyword);
                }
            }

            return def;
        }

        static CardType ParseType(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return CardType.Companion;

            var normalized = raw.Replace(" ", string.Empty);
            return Enum.TryParse<CardType>(normalized, true, out var type)
                ? type
                : CardType.Companion;
        }

        static CardRole? ParseRole(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var key = raw.Replace(" ", string.Empty);
            if (Enum.TryParse<CardRole>(key, true, out var role))
                return role;

            if (key.Equals("PlayMaker", StringComparison.OrdinalIgnoreCase))
                return CardRole.PlayMaker;

            return null;
        }

        static AbilityTiming ParseTiming(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return AbilityTiming.Static;

            var key = raw.Trim().ToLowerInvariant();
            return key switch
            {
                "replacement" => AbilityTiming.Replacement,
                "onpress" => AbilityTiming.OnPress,
                "afterpress" => AbilityTiming.AfterPress,
                "activated" => AbilityTiming.Activated,
                _ => AbilityTiming.Static,
            };
        }
    }
}
