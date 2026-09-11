using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class CardInstance
    {
        public int InstanceId;
        public CardPrinting Printing;
        public int ControllerId;
        public int OwnerId;
        public Zone Zone;
        public int? HostInstanceId;
        public bool Ready = true;
        public bool Exhausted;
        public int DamageMarked;
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        public HashSet<string> FlagsThisTurn = new HashSet<string>();
        public HashSet<string> FlagsThisClash = new HashSet<string>();
        public HashSet<Keyword> KeywordsNow = new HashSet<Keyword>();
        public int Timestamp;
        public bool StandAgainUsed;
        public bool IsToken;

        public int CurrentHealth => Printing != null ? Printing.Health - DamageMarked : 0;
        public int Strike => Printing?.Strike ?? 0;
        public int Guard => Printing?.Guard ?? 0;
        public int Health => Printing?.Health ?? 0;
        public int Toll => HasKeyword(Keyword.Toll) ? GetCounter("toll", 1) : 0;

        public bool HasKeyword(Keyword keyword) => KeywordsNow.Contains(keyword);

        public int GetCounter(string name, int defaultValue = 0)
        {
            return Counters.TryGetValue(name, out var value) ? value : defaultValue;
        }

        public void PutCounter(string name, int amount)
        {
            if (!Counters.ContainsKey(name))
                Counters[name] = 0;
            Counters[name] += amount;
        }
    }
}
