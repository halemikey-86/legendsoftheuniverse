using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Willbound.Engine
{
    public static class CardPrintingLoader
    {
        public static CardPrinting Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return ParseElement(doc.RootElement);
        }

        public static List<CardPrinting> ParseMany(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                var list = new List<CardPrinting>();
                foreach (var item in root.EnumerateArray())
                    list.Add(ParseElement(item));
                return list;
            }

            if (root.TryGetProperty("cards", out var cards))
            {
                var list = new List<CardPrinting>();
                foreach (var item in cards.EnumerateArray())
                    list.Add(ParseElement(item));
                return list;
            }

            return new List<CardPrinting> { ParseElement(root) };
        }

        static CardPrinting ParseElement(JsonElement el)
        {
            var printing = new CardPrinting
            {
                SchemaVersion = GetString(el, "schemaVersion"),
                Id = GetString(el, "id"),
                Set = GetString(el, "set"),
                Number = GetString(el, "number"),
                Name = GetString(el, "name"),
                Type = ParseType(GetString(el, "type")),
                Subtype = GetString(el, "subtype"),
                Role = GetString(el, "role"),
                WillCost = GetInt(el, "willCost"),
                StoreWorth = GetInt(el, "storeWorth"),
                Strike = GetInt(el, "strike"),
                Guard = GetInt(el, "guard"),
                Health = GetInt(el, "health"),
                StartsInPlay = GetBool(el, "startsInPlay"),
            };

            if (el.TryGetProperty("keywords", out var keywords))
            {
                foreach (var kw in keywords.EnumerateArray())
                    printing.Keywords.Add(kw.GetString());
            }

            if (el.TryGetProperty("abilities", out var abilities))
            {
                foreach (var abilityEl in abilities.EnumerateArray())
                {
                    var ability = new AbilityPrinting
                    {
                        Name = GetString(abilityEl, "name"),
                        CostWill = GetInt(abilityEl, "costWill"),
                        CostHonor = GetInt(abilityEl, "costHonor"),
                        Timing = ParseTiming(GetString(abilityEl, "timing")),
                        OncePerTurn = GetBool(abilityEl, "oncePerTurn"),
                        OncePerGame = GetBool(abilityEl, "oncePerGame"),
                        OncePerClash = GetBool(abilityEl, "oncePerClash"),
                        Text = GetString(abilityEl, "text"),
                    };

                    if (abilityEl.TryGetProperty("effects", out var effects))
                    {
                        foreach (var effectEl in effects.EnumerateArray())
                            ability.Effects.Add(ParseEffect(effectEl));
                    }

                    printing.Abilities.Add(ability);
                }
            }

            return printing;
        }

        static EffectPrinting ParseEffect(JsonElement el)
        {
            var effect = new EffectPrinting
            {
                Op = ParseOp(GetString(el, "op")),
                When = ParseWhen(GetString(el, "when")),
                Keyword = GetString(el, "keyword"),
                Counter = GetString(el, "counter"),
                Amount = GetInt(el, "amount"),
                Target = GetString(el, "target"),
                Filter = GetString(el, "filter"),
                Duration = GetString(el, "duration"),
            };

            if (el.TryGetProperty("then", out var then))
            {
                foreach (var child in then.EnumerateArray())
                    effect.Then.Add(ParseEffect(child));
            }

            foreach (var prop in el.EnumerateObject())
            {
                if (prop.Name is "op" or "when" or "keyword" or "counter" or "amount" or "target" or "filter" or "duration" or "then")
                    continue;
                effect.Params[prop.Name] = prop.Value.ToString();
            }

            return effect;
        }

        static CardType ParseType(string value)
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
                case "willsite":
                case "will": return CardType.WillSite;
                case "token": return CardType.Token;
                default: return CardType.Companion;
            }
        }

        static Timing ParseTiming(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Timing.Static;
            switch (value.Trim().ToLowerInvariant())
            {
                case "now": return Timing.Now;
                case "then": return Timing.Then;
                case "activated": return Timing.Activated;
                case "enter": return Timing.Enter;
                case "clash": return Timing.Clash;
                case "removed": return Timing.Removed;
                case "trigger": return Timing.Trigger;
                default: return Timing.Static;
            }
        }

        static EffectOp ParseOp(string value)
        {
            if (string.IsNullOrEmpty(value))
                return EffectOp.If;
            if (Enum.TryParse<EffectOp>(value, true, out var op))
                return op;
            return EffectOp.If;
        }

        static EffectWhen ParseWhen(string value)
        {
            if (string.IsNullOrEmpty(value))
                return EffectWhen.Immediate;
            switch (value.Trim())
            {
                case "OnEnter": return EffectWhen.OnEnter;
                case "OnStart": return EffectWhen.OnStart;
                case "OnEnd": return EffectWhen.OnEnd;
                case "OnClashBegin": return EffectWhen.OnClashBegin;
                case "OnClashEnd": return EffectWhen.OnClashEnd;
                case "OnHold": return EffectWhen.OnHold;
                case "OnPressDeclared": return EffectWhen.OnPressDeclared;
                case "OnPressDealtDamage": return EffectWhen.OnPressDealtDamage;
                case "OnPressRemovedBody": return EffectWhen.OnPressRemovedBody;
                case "OnRemoved": return EffectWhen.OnRemoved;
                case "OnBanished": return EffectWhen.OnBanished;
                default: return EffectWhen.Immediate;
            }
        }

        static string GetString(JsonElement el, string name) =>
            el.TryGetProperty(name, out var prop) ? prop.GetString() : null;

        static int GetInt(JsonElement el, string name) =>
            el.TryGetProperty(name, out var prop) && prop.TryGetInt32(out var value) ? value : 0;

        static bool GetBool(JsonElement el, string name) =>
            el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.True;
    }
}
