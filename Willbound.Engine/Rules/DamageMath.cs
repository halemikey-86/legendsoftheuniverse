using System;

namespace Willbound.Engine
{
    public static class DamageMath
    {
        // 4.9.73–76
        public static int ComputeDamage(
            int strikeEffective,
            int guardEffective,
            bool absolute)
        {
            var baseDamage = Math.Max(0, strikeEffective - guardEffective);
            if (!absolute)
                return baseDamage;

            var absoluteFloor = (strikeEffective + 1) / 2;
            return Math.Max(baseDamage, absoluteFloor);
        }

        public static int EffectiveStrike(CardInstance source, Match match, int strikeSnapshot = 0)
        {
            var strike = strikeSnapshot > 0 ? strikeSnapshot : source.Strike;
            if (match.DoubleteamStrikeBonus.TryGetValue(source.InstanceId, out var bonus))
                strike += bonus;
            return strike;
        }

        public static int EffectiveGuard(CardInstance target, Match match)
        {
            var guard = target.Guard;
            if (match.DoubleteamGuardBonus.TryGetValue(target.InstanceId, out var bonus))
                guard += bonus;
            return guard;
        }
    }
}
