using System;
using System.Collections.Generic;
using System.Linq;

namespace LegendsOfTheUniverse.Rules
{
    /// <summary>
    /// Solo James The Endless setup and ability locks.
    /// </summary>
    public static class EndlessDeckSetup
    {
        public static GameState CreateJamesEndlessDemo(int? rngSeed = null, int storeSlots = 7)
        {
            var james = new PlayerState(1, "James");
            var opponent = new PlayerState(2, "Opponent");
            var state = new GameState(new[] { james, opponent }, storeSlots, rngSeed);
            var nextInstanceId = 1;
            ConfigureSoloJames(james, ref nextInstanceId);
            return state;
        }

        public static void ConfigureSoloJames(PlayerState player, ref int nextInstanceId)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (!WillboundCardCatalog.TryGet(EndlessCardCatalog.JamesIconName, out var jamesDef))
                throw new InvalidOperationException("James The Endless Icon not in catalog.");

            player.Deck.Clear();
            player.Hand.Clear();
            player.Field.Clear();
            player.Icon = null;

            foreach (var def in EndlessCardCatalog.Block01To10)
            {
                if (def.StartsInPlay)
                    continue;

                var card = CardEngine.CreateInstance(nextInstanceId++, def, player.PlayerId, ZoneType.Deck);
                player.Deck.Add(card);
            }

            if (jamesDef.StartsInPlay)
            {
                var icon = CardEngine.CreateInstance(nextInstanceId++, jamesDef, player.PlayerId, ZoneType.Field);
                player.Icon = icon;
                player.Field.Add(icon);
            }
        }
    }

    public static class EndlessAbilityKeys
    {
        public static string For(CardInstance card, CardAbilityDef ability) =>
            $"{card.InstanceId}:{ability.Name}";

        public static string EndlessLock(int playerId) => $"endless-01:Endless:p{playerId}";
    }

    public sealed class EndlessClashState
    {
        public int ClashesThisTurn { get; set; }
        public int PressesThisClash { get; set; }
        public CardInstance LastPressAttacker { get; set; }
        public CardInstance LastPressTarget { get; set; }
        public CardInstance PendingPressTarget { get; set; }
        public int LastPressDamage { get; set; }
        public string LastOpposingMoveName { get; set; }
        public string LastOpposingMoveCardId { get; set; }
    }

    public static class EndlessCombatEngine
    {
        public const string JamesCardId = EndlessCardCatalog.JamesIconName;

        public static bool Matches(CardInstance card, string endlessId)
        {
            if (card == null || string.IsNullOrEmpty(endlessId))
                return false;

            return WillboundCardCatalog.TryGet(card.CardId, out var def)
                   && string.Equals(def.Id, endlessId, StringComparison.OrdinalIgnoreCase);
        }

        public static EndlessClashState ClashState(GameState state)
        {
            state.EndlessClash ??= new EndlessClashState();
            return state.EndlessClash;
        }

        public static void OnClashStepStart(GameState state)
        {
            var clash = ClashState(state);
            clash.ClashesThisTurn++;
            clash.PressesThisClash = 0;
        }

        public static void OnTurnStart(GameState state)
        {
            state.EndlessClash = new EndlessClashState();
            state.OncePerTurnLocks.Clear();
            state.EndlessPressModifiers.Clear();
        }

        /// <summary>
        /// Endless replacement — fires before Remove/elimination, not as a health-check side effect.
        /// </summary>
        public static bool TryEndlessReplacement(RulesEngine engine, CardInstance icon, PlayerState owner)
        {
            if (icon == null || owner?.Icon != icon)
                return false;

            if (!icon.CardId.Equals(JamesCardId, StringComparison.OrdinalIgnoreCase))
                return false;

            var lockKey = EndlessAbilityKeys.EndlessLock(owner.PlayerId);
            if (engine.State.OncePerGameLocks.Contains(lockKey))
                return false;

            if (!WillboundCardCatalog.TryGet(icon.CardId, out var def))
                return false;

            var endless = def.Abilities.FirstOrDefault(a =>
                a.Name.Equals("Endless", StringComparison.OrdinalIgnoreCase));
            if (endless == null)
                return false;

            icon.Damage = Math.Max(0, icon.Health - 1);
            engine.State.OncePerGameLocks.Add(lockKey);
            return true;
        }

        public static int ModifyGuardForPress(GameState state, CardInstance attacker, CardInstance target,
            PlayerState attackerPlayer, PlayerState defenderPlayer, bool targetIsHolding, int strikeAmount)
        {
            var guard = targetIsHolding ? target.Guard : 0;

            if (Matches(attacker, "endless-03") && attackerPlayer.WillPool < defenderPlayer.WillPool)
                return 0;

            if (Matches(attacker, "endless-02")
                && state.EndlessPressModifiers.TryGetValue(attacker.InstanceId, out var mods)
                && mods.EddieGuardIgnored)
                guard = Math.Max(0, guard - 1);

            return guard;
        }

        public static bool TrySanjayBlackLeg(GameState state, CardInstance sanjay)
        {
            if (!Matches(sanjay, "endless-05"))
                return false;

            var abilityKey = $"{sanjay.InstanceId}:Black Leg";
            if (state.OncePerTurnLocks.ContainsKey(abilityKey))
                return false;

            state.OncePerTurnLocks[abilityKey] = true;
            return true;
        }

        public static bool CanMoveFromField(CardInstance card, string reasonCardName)
        {
            if (!Matches(card, "endless-07"))
                return true;

            if (string.IsNullOrEmpty(reasonCardName))
                return false;

            return reasonCardName.IndexOf("Ronan", StringComparison.OrdinalIgnoreCase) >= 0
                   || reasonCardName.IndexOf("The Anchor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static int ExtraPressesAllowedThisClash(GameState state, PlayerState player)
        {
            var hasRyu = player.Field.Any(c => Matches(c, "endless-08"));
            if (!hasRyu)
                return 0;

            return ClashState(state).ClashesThisTurn;
        }

        public static bool CanPressThisClash(GameState state, PlayerState player)
        {
            var allowed = 1 + ExtraPressesAllowedThisClash(state, player);
            return ClashState(state).PressesThisClash < allowed;
        }

        public static void RecordPress(GameState state, CardInstance attacker, CardInstance target, int damage)
        {
            var clash = ClashState(state);
            clash.PressesThisClash++;
            clash.LastPressAttacker = attacker;
            clash.LastPressTarget = target;
            clash.LastPressDamage = damage;
        }

        public static void RecordOpposingCompanionAbility(GameState state, CardInstance source, string abilityName)
        {
            if (source == null || source.PrintedType != CardType.Companion)
                return;

            ClashState(state).LastOpposingMoveName = abilityName;
            ClashState(state).LastOpposingMoveCardId = source.CardId;
        }

        public static bool TryPayEddieRubberGuard(RulesEngine engine, CardInstance eddie, PlayerState player)
        {
            if (eddie == null || player == null || !Matches(eddie, "endless-02"))
                return false;

            if (player.WillPool < 1)
                return false;

            player.WillPool -= 1;
            if (!engine.State.EndlessPressModifiers.ContainsKey(eddie.InstanceId))
                engine.State.EndlessPressModifiers[eddie.InstanceId] = new EndlessPressModifierState();

            engine.State.EndlessPressModifiers[eddie.InstanceId].EddieGuardIgnored = true;
            engine.State.EndlessPressModifiers[eddie.InstanceId].EddieRedirectDamage = true;
            return true;
        }

        public static void ClearPressGuardModifiers(GameState state, CardInstance attacker)
        {
            if (attacker == null)
                return;

            if (state.EndlessPressModifiers.TryGetValue(attacker.InstanceId, out var mods))
                mods.EddieGuardIgnored = false;
        }

        /// <summary>
        /// Resolves incoming Clash damage. When Eddie paid Rubber Guard on his Press, 1 damage redirects to his Press target.
        /// </summary>
        public static int ResolveIncomingClashDamage(GameState state, CardInstance victim, int amount,
            out CardInstance redirectTarget)
        {
            redirectTarget = null;
            if (victim == null || amount <= 0)
                return amount;

            if (!Matches(victim, "endless-02"))
                return amount;

            if (!state.EndlessPressModifiers.TryGetValue(victim.InstanceId, out var mods) || !mods.EddieRedirectDamage)
                return amount;

            var clash = ClashState(state);
            if (clash.LastPressAttacker != victim || clash.LastPressTarget == null)
                return amount;

            mods.EddieRedirectDamage = false;
            redirectTarget = clash.LastPressTarget;
            return Math.Max(0, amount - 1);
        }

        public static bool TryDempseyRollRetarget(RulesEngine engine, CardInstance damon, CardInstance newTarget)
        {
            if (damon == null || newTarget == null || !Matches(damon, "endless-04"))
                return false;

            var key = $"{damon.InstanceId}:Dempsey Roll";
            if (engine.State.OncePerTurnLocks.ContainsKey(key))
                return false;

            var clash = ClashState(engine.State);
            if (clash.LastPressAttacker != damon || clash.LastPressDamage <= 0)
                return false;

            if (newTarget.PrintedType != CardType.Icon && newTarget.PrintedType != CardType.Companion)
                return false;

            newTarget.Damage += clash.LastPressDamage;
            if (clash.LastPressTarget != null && clash.LastPressTarget != newTarget)
                clash.LastPressTarget.Damage = Math.Max(0, clash.LastPressTarget.Damage - clash.LastPressDamage);

            clash.LastPressTarget = newTarget;
            engine.State.OncePerTurnLocks[key] = true;
            engine.RunStateChecks();
            return true;
        }

        public static bool TryZaneSecondTarget(RulesEngine engine, CardInstance zane, CardInstance secondTarget)
        {
            if (zane == null || secondTarget == null || !Matches(zane, "endless-06"))
                return false;

            var clash = ClashState(engine.State);
            if (clash.LastPressAttacker != zane)
                return false;

            if (secondTarget.ControllerId == zane.ControllerId)
                return false;

            secondTarget.Damage += 1;
            engine.RunStateChecks();
            return true;
        }

        public static bool TryIronGripRetarget(GameState state, CardInstance kaito, CardInstance newTarget)
        {
            if (kaito == null || newTarget == null || !Matches(kaito, "endless-09"))
                return false;

            if (newTarget.PrintedType != CardType.Companion || newTarget.ControllerId == kaito.ControllerId)
                return false;

            ClashState(state).PendingPressTarget = newTarget;
            return true;
        }

        public static CardInstance ConsumePendingPressTarget(GameState state)
        {
            var clash = ClashState(state);
            var target = clash.PendingPressTarget;
            clash.PendingPressTarget = null;
            return target;
        }

        public static bool TryActivateCopycat(RulesEngine engine, CardInstance kiro)
        {
            if (kiro == null || !Matches(kiro, "endless-10"))
                return false;

            var key = $"{kiro.InstanceId}:Copycat";
            if (engine.State.OncePerTurnLocks.ContainsKey(key))
                return false;

            var clash = ClashState(engine.State);
            if (string.IsNullOrEmpty(clash.LastOpposingMoveName))
                return false;

            engine.State.OncePerTurnLocks[key] = true;
            return true;
        }

        public static string GetCopycatAbilityName(GameState state) =>
            ClashState(state).LastOpposingMoveName;

        public static string GetCopycatSourceCardId(GameState state) =>
            ClashState(state).LastOpposingMoveCardId;
    }

    public sealed class EndlessPressModifierState
    {
        public bool EddieGuardIgnored { get; set; }
        public bool EddieRedirectDamage { get; set; }
    }
}
