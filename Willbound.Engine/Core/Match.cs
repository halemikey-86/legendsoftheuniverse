using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class Match
    {
        public string MatchId;
        public int Round = 1;
        public Phase Phase = Phase.Setup;
        public ClashPhase ClashPhase = ClashPhase.None;
        public int ActivePlayerId;
        public int PriorityPlayerId;
        public HashSet<int> Passed = new HashSet<int>();
        public List<StackObject> Stack = new List<StackObject>();
        public List<QueuedPress> ClashQueue = new List<QueuedPress>();
        public CardInstance[] Store = new CardInstance[7];
        public List<CardInstance> Supply = new List<CardInstance>();
        public List<CardInstance> Removed = new List<CardInstance>();
        public List<CardInstance> Banished = new List<CardInstance>();
        public List<Player> Players = new List<Player>();
        public int NextTimestamp;
        public int? WinnerId;
        public List<GameEvent> EventLog = new List<GameEvent>();
        public int NextInstanceId = 1;
        public int NextStackId = 1;

        // Clash transient state
        public List<int> BodiesAwaitingDeclare = new List<int>();
        public int AnswerOfferIndex;
        public bool ClashLocked;
        public int PressesDeclaredThisClash;
        public bool FirstPressAggressionGranted;
        public Dictionary<int, int> DoubleteamStrikeBonus = new Dictionary<int, int>();
        public Dictionary<int, int> DoubleteamGuardBonus = new Dictionary<int, int>();
        public Dictionary<int, int> ClashKeywordGrants = new Dictionary<int, int>();
        public bool StartStepComplete;
        public bool DrawStepComplete;
        public bool SiteStepComplete;
        public bool EndStepComplete;

        public int NextInstance() => NextInstanceId++;
        public int NextStack() => NextStackId++;
        public int NextTs() => ++NextTimestamp;

        public Player GetPlayer(int id)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                if (Players[i].Id == id)
                    return Players[i];
            }

            return null;
        }

        public CardInstance GetCard(int instanceId)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p.Icon != null && p.Icon.InstanceId == instanceId)
                    return p.Icon;
                var found = FindInList(p.Deck, instanceId) ?? FindInList(p.Hand, instanceId)
                    ?? FindInList(p.Field, instanceId) ?? FindInList(p.Willwell, instanceId);
                if (found != null)
                    return found;
            }

            for (var i = 0; i < Store.Length; i++)
            {
                if (Store[i] != null && Store[i].InstanceId == instanceId)
                    return Store[i];
            }

            return FindInList(Supply, instanceId) ?? FindInList(Removed, instanceId) ?? FindInList(Banished, instanceId);
        }

        static CardInstance FindInList(List<CardInstance> list, int instanceId)
        {
            if (list == null)
                return null;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].InstanceId == instanceId)
                    return list[i];
            }

            return null;
        }

        public List<Player> LivingPlayers()
        {
            var result = new List<Player>();
            for (var i = 0; i < Players.Count; i++)
            {
                if (Players[i].IsAlive)
                    result.Add(Players[i]);
            }

            return result;
        }
    }
}
