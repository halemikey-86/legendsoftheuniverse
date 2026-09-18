using System;
using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class SeededRng : IRng
    {
        readonly Random random;

        public SeededRng(int seed) => random = new Random(seed);

        public int NextInt(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);

        public void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = NextInt(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
