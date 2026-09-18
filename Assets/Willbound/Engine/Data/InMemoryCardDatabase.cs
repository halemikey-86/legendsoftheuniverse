using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class InMemoryCardDatabase : ICardDatabase
    {
        readonly Dictionary<string, CardPrinting> byId = new Dictionary<string, CardPrinting>();
        readonly List<CardPrinting> all = new List<CardPrinting>();

        public IReadOnlyList<CardPrinting> All => all;

        public InMemoryCardDatabase(IEnumerable<CardPrinting> printings)
        {
            foreach (var printing in printings)
            {
                byId[printing.Id] = printing;
                all.Add(printing);
            }
        }

        public bool TryGet(string printingId, out CardPrinting printing) => byId.TryGetValue(printingId, out printing);

        public static InMemoryCardDatabase From(params CardPrinting[] printings) => new InMemoryCardDatabase(printings);
    }
}
