using System;

namespace LegendsOfTheUniverse.Rules
{
    public sealed class CardInstance
    {
        public CardInstance(int instanceId, string cardId, CardType printedType, bool isUniverse = false)
        {
            InstanceId = instanceId;
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            PrintedType = printedType;
            IsUniverse = isUniverse;
        }

        public int InstanceId { get; }
        public string CardId { get; }
        public CardType PrintedType { get; }
        public bool IsUniverse { get; }

        public int ControllerId { get; set; } = -1;
        public ZoneType Zone { get; set; }
        public int Damage { get; set; }
        public int? AttachedToInstanceId { get; set; }
        public bool Exhausted { get; set; }
        public bool Holding { get; set; }
        public bool Sealed { get; set; }
        public bool Closed { get; set; }
        public int Toll { get; set; }
        public TokenKind? TokenKind { get; set; }
        public long LastAppliedTimestamp { get; set; }

        public int Strike { get; set; }
        public int Guard { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }

        public int RemainingHealth => Math.Max(0, Health - Damage);

        public bool IsToken => PrintedType == CardType.Token;
        public bool IsIcon => PrintedType == CardType.Icon;

        public bool IsRemovedByDamage =>
            RemainingHealth <= 0 && !Sealed;
    }
}
