using System;
using System.Collections.Generic;
using System.Text;

namespace LegendsOfTheUniverse.Rules
{
    public static class CardEngine
    {
        public const string UnknownCardMessage = "UNKNOWN CARD";

        public static string LookupJson(string name)
        {
            if (!WillboundCardCatalog.TryGet(name, out var def))
                return BuildUnknownJson(name);

            return ToJson(def);
        }

        public static CardDef Lookup(string name)
        {
            return WillboundCardCatalog.GetOrUnknown(name);
        }

        public static PlayResolution PreparePlay(RulesEngine engine, int actingPlayerId, string cardName)
        {
            if (engine?.State == null)
                return PlayResolution.Reject("Game not started.");

            if (!WillboundCardCatalog.TryGet(cardName, out var def))
                return PlayResolution.Reject(UnknownCardMessage);

            var player = engine.State.GetPlayer(actingPlayerId);
            if (player == null)
                return PlayResolution.Reject("Unknown player.");

            var playCost = GetEffectivePlayCost(def, player);
            if (playCost > player.WillPool)
                return PlayResolution.Reject($"Insufficient Will. Need {playCost}, have {player.WillPool}.");

            var zone = GetPlayDestination(def);
            return PlayResolution.Ok(def, playCost, zone);
        }

        public static int GetEffectivePlayCost(CardDef def, PlayerState player)
        {
            if (def == null)
                return int.MaxValue;

            var baseCost = def.PlayCost ?? 0;

            if (def.Name.Equals("Ptainac Plenet", StringComparison.OrdinalIgnoreCase)
                && CardCatalogParser.HasAllBands(player.Field))
                return 0;

            if (def.Name.Equals("Hugacef", StringComparison.OrdinalIgnoreCase)
                && CardCatalogParser.ControlsNamedCard(player.Field, "Neeuq Morphoxen"))
                return 0;

            return baseCost;
        }

        public static ZoneType GetPlayDestination(CardDef def)
        {
            return def.Type switch
            {
                CardType.Icon => ZoneType.Field,
                CardType.Companion => ZoneType.Field,
                CardType.Relic => ZoneType.Field,
                CardType.Bond => ZoneType.Field,
                CardType.Will => ZoneType.Willwell,
                CardType.Surge => ZoneType.Field,
                CardType.Token => ZoneType.Field,
                _ => ZoneType.Field,
            };
        }

        public static CardInstance CreateInstance(int instanceId, CardDef def, int controllerId, ZoneType zone)
        {
            var card = new CardInstance(instanceId, def.Name, def.Type, def.Universe)
            {
                ControllerId = controllerId,
                Zone = zone,
            };

            if (def.Strike.HasValue)
                card.Strike = def.Strike.Value;
            if (def.Guard.HasValue)
                card.Guard = def.Guard.Value;
            if (def.Health.HasValue)
            {
                card.Health = def.Health.Value;
                card.MaxHealth = def.Health.Value;
            }

            return card;
        }

        public static string ValidateDeck(IReadOnlyList<string> cardNames)
        {
            if (cardNames == null)
                return "Deck is empty.";

            if (cardNames.Count > GameConstants.MaxDeckSize)
                return $"Deck exceeds {GameConstants.MaxDeckSize} cards.";

            var iconCount = 0;
            foreach (var name in cardNames)
            {
                if (!WillboundCardCatalog.TryGet(name, out var def))
                    return UnknownCardMessage + ": " + name;

                if (def.Type == CardType.Icon)
                    iconCount++;
            }

            if (iconCount > GameConstants.IconsPerPack)
                return $"Deck has {iconCount} Icons; max {GameConstants.IconsPerPack}.";

            return null;
        }

        public static string ListPackDeck(IReadOnlyList<(string name, int copies)> entries)
        {
            var sb = new StringBuilder();
            var total = 0;
            var icons = 0;

            foreach (var (name, copies) in entries)
            {
                if (!WillboundCardCatalog.TryGet(name, out var def))
                {
                    sb.AppendLine($"{name} x{copies} — {UnknownCardMessage}");
                    continue;
                }

                total += copies;
                if (def.Type == CardType.Icon)
                    icons += copies;

                sb.AppendLine($"{name} x{copies} | {def.DisplayType} | Play {def.PlayCost?.ToString() ?? "—"} | Store {def.StoreCost?.ToString() ?? "—"}");
            }

            sb.AppendLine($"Total: {total}/{GameConstants.MaxDeckSize}. Icons: {icons}/{GameConstants.IconsPerPack}.");
            return sb.ToString();
        }

        static string BuildUnknownJson(string name)
        {
            return "{\n  \"name\": \"" + EscapeJson(name ?? UnknownCardMessage) + "\",\n  \"error\": \"" + UnknownCardMessage + "\"\n}";
        }

        public static string ToJson(CardDef def)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"id\": {NullableStringJson(def.Id)},");
            sb.AppendLine($"  \"name\": \"{EscapeJson(def.Name)}\",");
            sb.AppendLine($"  \"set\": {NullableStringJson(def.SetName)},");
            sb.AppendLine($"  \"number\": {NullableStringJson(def.Number)},");
            sb.AppendLine($"  \"type\": \"{EscapeJson(def.DisplayType)}\",");
            sb.AppendLine($"  \"subtype\": {NullableStringJson(def.Subtype)},");
            sb.AppendLine($"  \"role\": {RoleJson(def.Role)},");
            sb.AppendLine($"  \"pack\": \"{EscapeJson(def.Pack)}\",");
            sb.AppendLine($"  \"play\": {def.PlayCost ?? 0},");
            sb.AppendLine($"  \"store\": {def.StoreCost ?? 0},");
            sb.AppendLine($"  \"strike\": {NullableJson(def.Strike)},");
            sb.AppendLine($"  \"guard\": {NullableJson(def.Guard)},");
            sb.AppendLine($"  \"health\": {NullableJson(def.Health)},");
            sb.AppendLine($"  \"startsInPlay\": {(def.StartsInPlay ? "true" : "false")},");
            sb.AppendLine($"  \"universe\": {(def.Universe ? "true" : "false")},");
            sb.AppendLine("  \"abilities\": " + AbilitiesJson(def.Abilities) + ",");
            sb.AppendLine("  \"spells\": " + SpellsJson(def.Spells) + ",");
            sb.AppendLine("  \"static\": " + StringArrayJson(def.Static) + ",");
            sb.AppendLine("  \"triggers\": " + StringArrayJson(def.Triggers) + ",");
            sb.AppendLine("  \"tokens_made\": " + StringArrayJson(def.TokensMade));
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string NullableStringJson(string value) =>
            string.IsNullOrEmpty(value) ? "null" : $"\"{EscapeJson(value)}\"";

        static string AbilitiesJson(IReadOnlyList<CardAbilityDef> abilities)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            for (var i = 0; i < abilities.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                var a = abilities[i];
                sb.Append("{");
                sb.Append($"\"name\":\"{EscapeJson(a.Name)}\",");
                sb.Append($"\"timing\":\"{a.Timing.ToString().ToLowerInvariant()}\",");
                sb.Append($"\"oncePerGame\":{(a.OncePerGame ? "true" : "false")},");
                sb.Append($"\"oncePerTurn\":{(a.OncePerTurn ? "true" : "false")},");
                sb.Append($"\"optional\":{(a.Optional ? "true" : "false")},");
                sb.Append($"\"costWill\":{(a.CostWill.HasValue ? a.CostWill.Value.ToString() : "null")},");
                sb.Append($"\"text\":\"{EscapeJson(a.Text)}\"");
                sb.Append("}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        static string RoleJson(CardRole? role)
        {
            if (!role.HasValue)
                return "null";

            var label = role.Value switch
            {
                CardRole.PlayMaker => "Play Maker",
                _ => role.Value.ToString(),
            };

            return $"\"{label}\"";
        }

        static string NullableJson(int? value) => value.HasValue ? value.Value.ToString() : "null";

        static string SpellsJson(IReadOnlyList<CardSpellDef> spells)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            for (var i = 0; i < spells.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append("{");
                sb.Append($"\"name\":\"{EscapeJson(spells[i].Name)}\",");
                sb.Append($"\"cost\":{spells[i].Cost},");
                sb.Append($"\"text\":\"{EscapeJson(spells[i].Text)}\"");
                sb.Append("}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        static string StringArrayJson(IReadOnlyList<string> values)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append($"\"{EscapeJson(values[i])}\"");
            }

            sb.Append("]");
            return sb.ToString();
        }

        static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    public readonly struct PlayResolution
    {
        public PlayResolution(bool legal, string reason, CardDef def, int playCost, ZoneType destination)
        {
            Legal = legal;
            Reason = reason;
            Def = def;
            PlayCost = playCost;
            Destination = destination;
        }

        public bool Legal { get; }
        public string Reason { get; }
        public CardDef Def { get; }
        public int PlayCost { get; }
        public ZoneType Destination { get; }

        public static PlayResolution Ok(CardDef def, int playCost, ZoneType destination) =>
            new(true, null, def, playCost, destination);

        public static PlayResolution Reject(string reason) =>
            new(false, reason, null, 0, ZoneType.Hand);
    }
}
