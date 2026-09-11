using System;
using System.Collections.Generic;
using System.Linq;

namespace LegendsOfTheUniverse.Rules
{
    public static class WillboundCardCatalog
    {
        static readonly Dictionary<string, CardDef> ByName = new(StringComparer.OrdinalIgnoreCase);
        static bool loaded;

        public static IReadOnlyCollection<CardDef> All => EnsureLoaded().Values.ToList();

        public static bool TryGet(string name, out CardDef def)
        {
            EnsureLoaded();
            return ByName.TryGetValue(name?.Trim() ?? string.Empty, out def);
        }

        public static CardDef GetOrUnknown(string name)
        {
            if (TryGet(name, out var def))
                return def;

            return new CardDef
            {
                Name = name ?? "UNKNOWN CARD",
                DisplayType = "UNKNOWN",
                Pack = "UNKNOWN",
            };
        }

        public static bool IsKnown(string name)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(name) && ByName.ContainsKey(name.Trim());
        }

        static Dictionary<string, CardDef> EnsureLoaded()
        {
            if (!loaded)
                Load();

            return ByName;
        }

        public static void Load()
        {
            ByName.Clear();

            var lines = WillboundCatalogLines.Data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var def = CardCatalogParser.ParseLine(line);
                if (def == null || string.IsNullOrWhiteSpace(def.Name))
                    continue;

                ByName[def.Name] = def;
            }

            EndlessCardCatalog.RegisterInto(ByName);

            // Postgres catalog export overrides embedded lines when present.
            CatalogJsonLoader.RegisterInto(ByName);

            loaded = true;
        }
    }
}
