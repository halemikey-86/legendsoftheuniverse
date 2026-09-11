using System.Collections.Generic;

namespace Willbound.Engine
{
    public interface IMatchView
    {
        void OnEvent(GameEvent e);
    }

    public interface ICardDatabase
    {
        bool TryGet(string printingId, out CardPrinting printing);
        IReadOnlyList<CardPrinting> All { get; }
    }

    public interface IRng
    {
        int NextInt(int minInclusive, int maxExclusive);
        void Shuffle<T>(IList<T> list);
    }
}
