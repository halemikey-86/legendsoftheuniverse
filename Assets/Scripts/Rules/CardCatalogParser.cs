using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LegendsOfTheUniverse.Rules
{
    public static class CardCatalogParser
    {
        static readonly Regex SpellPattern = new(@"^(.+?) \((\d+)\): (.+)$", RegexOptions.Compiled);

        static readonly string[] BandNames =
        {
            "Band Ertha",
            "Band Rife",
            "Band Nwid",
            "Band Tawer",
            "Band Traeh",
        };

        public static CardDef ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var parts = SplitCatalogLine(line);
            if (parts.Count < 8)
                return null;

            var def = new CardDef
            {
                Name = parts[0].Trim(),
                DisplayType = parts[1].Trim(),
                Pack = parts[3].Trim(),
                PlayCost = ParseOptionalInt(parts[4]),
                StoreCost = ParseOptionalInt(parts[5]),
            };

            ParseType(def, parts[1].Trim());
            ParseRole(def, parts[2].Trim());
            ParseStats(def, parts[6].Trim());
            ParseText(def, parts[7].Trim());

            return def;
        }

        static List<string> SplitCatalogLine(string line)
        {
            var parts = new List<string>();
            var current = string.Empty;
            var segment = 0;

            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '|' && segment < 7)
                {
                    parts.Add(current);
                    current = string.Empty;
                    segment++;
                    continue;
                }

                current += line[i];
            }

            parts.Add(current);
            return parts;
        }

        static void ParseType(CardDef def, string rawType)
        {
            if (rawType.Equals("Universe Relic", StringComparison.OrdinalIgnoreCase))
            {
                def.Type = CardType.Relic;
                def.Universe = true;
                def.DisplayType = "Universe Relic";
                return;
            }

            if (Enum.TryParse<CardType>(rawType.Replace(" ", ""), true, out var type)
                || Enum.TryParse<CardType>(rawType, true, out type))
            {
                def.Type = type;
                def.DisplayType = rawType;
                return;
            }

            def.Type = CardType.Relic;
            def.DisplayType = rawType;
        }

        static void ParseRole(CardDef def, string rawRole)
        {
            if (IsDash(rawRole))
                return;

            var normalized = rawRole.Replace(" ", "");
            if (Enum.TryParse<CardRole>(normalized, true, out var role))
                def.Role = role;
        }

        static void ParseStats(CardDef def, string rawStats)
        {
            if (IsDash(rawStats))
                return;

            var split = rawStats.Split('/');
            if (split.Length != 3)
                return;

            def.Strike = int.Parse(split[0]);
            def.Guard = int.Parse(split[1]);
            def.Health = int.Parse(split[2]);
        }

        static void ParseText(CardDef def, string rawText)
        {
            var segments = rawText.Split(new[] { " || " }, StringSplitOptions.None);
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var spellMatch = SpellPattern.Match(trimmed);
                if (spellMatch.Success)
                {
                    def.Spells.Add(new CardSpellDef
                    {
                        Name = spellMatch.Groups[1].Value.Trim(),
                        Cost = int.Parse(spellMatch.Groups[2].Value),
                        Text = spellMatch.Groups[3].Value.Trim(),
                    });
                    ExtractTokens(def, spellMatch.Groups[3].Value);
                    continue;
                }

                if (trimmed.StartsWith("When ", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("Once per turn", StringComparison.OrdinalIgnoreCase))
                {
                    def.Triggers.Add(trimmed);
                    ExtractTokens(def, trimmed);
                    continue;
                }

                def.Static.Add(trimmed);
                ExtractTokens(def, trimmed);
            }
        }

        static void ExtractTokens(CardDef def, string text)
        {
            if (text.IndexOf("Wall token", StringComparison.OrdinalIgnoreCase) >= 0
                && !def.TokensMade.Contains("Wall"))
                def.TokensMade.Add("Wall");

            if (text.IndexOf("Bulkhead", StringComparison.OrdinalIgnoreCase) >= 0
                && !def.TokensMade.Contains("Bulkhead"))
                def.TokensMade.Add("Bulkhead");

            if (text.IndexOf("Riverman", StringComparison.OrdinalIgnoreCase) >= 0
                && !def.TokensMade.Contains("Riverman"))
                def.TokensMade.Add("Riverman");

            if (text.IndexOf("Hugacef", StringComparison.OrdinalIgnoreCase) >= 0
                && !def.TokensMade.Contains("Hugacef"))
                def.TokensMade.Add("Hugacef");
        }

        static int? ParseOptionalInt(string raw)
        {
            if (IsDash(raw))
                return null;

            return int.TryParse(raw.Trim(), out var value) ? value : null;
        }

        static bool IsDash(string value)
        {
            var trimmed = value.Trim();
            return trimmed == "—" || trimmed == "-" || trimmed == "--";
        }

        public static bool HasAllBands(IReadOnlyList<CardInstance> field)
        {
            if (field == null)
                return false;

            foreach (var bandName in BandNames)
            {
                var found = false;
                foreach (var card in field)
                {
                    if (card != null && card.CardId.Equals(bandName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        public static bool ControlsNamedCard(IReadOnlyList<CardInstance> field, string name)
        {
            if (field == null || string.IsNullOrEmpty(name))
                return false;

            foreach (var card in field)
            {
                if (card != null && card.CardId.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
