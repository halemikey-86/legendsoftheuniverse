using System.Collections.Generic;

namespace Willbound.Engine
{
    public sealed class StackObject
    {
        public int StackId;
        public int Timestamp;
        public StackObjectType Type;
        public int ControllerId;
        public int? SourceInstanceId;
        public string PrintingId;
        public int? AbilityIndex;
        public List<int> Targets = new List<int>();
        public int PaidWill;
        public int PaidWorth;
        public int Wave;
        public HashSet<Keyword> Keywords = new HashSet<Keyword>();
        public int StrikeSnapshot;
        public int DoubleteamStrike;
        public int? AnsweredToStackId;
        public StoreActionKind? StoreKind;
        public int? StoreSlotIndex;
        public int? HandCardInstanceId;
        public List<EffectPrinting> Effects = new List<EffectPrinting>();
        public bool Fizzled;
    }

    public sealed class QueuedPress
    {
        public int StackId;
        public int Timestamp;
        public int ControllerId;
        public int SourceInstanceId;
        public int TargetInstanceId;
        public int Wave;
        public HashSet<Keyword> Keywords = new HashSet<Keyword>();
        public int StrikeSnapshot;
        public int DoubleteamGuard;
        public bool Skipped;
        public int DamageDealt;
        public bool RemovedTarget;
    }
}
