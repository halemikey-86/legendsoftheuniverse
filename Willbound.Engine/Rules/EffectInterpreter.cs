using System;
using System.Collections.Generic;

namespace Willbound.Engine
{
    /// <summary>Who an effect is resolving for, and what it may reference by name (5.2/5.3).
    /// Source is the card whose ability is resolving; ChosenTarget is whatever the player
    /// picked when the ability was put on the Stack (a Press target, a Bond host, an
    /// Activate target, ...).</summary>
    public sealed class EffectContext
    {
        public CardInstance Source;
        public Player Controller;
        public CardInstance ChosenTarget;
        public int DamageAmount;

        public EffectContext(CardInstance source, Player controller, CardInstance chosenTarget = null, int damageAmount = 0)
        {
            Source = source;
            Controller = controller;
            ChosenTarget = chosenTarget;
            DamageAmount = damageAmount;
        }
    }

    /// <summary>The one place abilities[].effects[] actually run. Every ability resolution and
    /// every hook (OnEnter, OnRemoved, OnClashBegin/End, OnPress*, OnHold, OnAnswer,
    /// OnStoreAction) funnels through Execute — no card is special-cased by printing id here.</summary>
    public static class EffectInterpreter
    {
        /// <summary>Runs every effect on a specific ability, in order. Pass <paramref name="onlyWhen"/>
        /// to restrict to effects tagged for a particular hook; leave null to run the ability's
        /// direct resolution (all of its Immediate effects).</summary>
        public static void RunAbility(MatchRunner runner, AbilityPrinting ability, EffectContext ctx, EffectWhen? onlyWhen = null)
        {
            var when = onlyWhen ?? EffectWhen.Immediate;
            for (var i = 0; i < ability.Effects.Count; i++)
            {
                var effect = ability.Effects[i];
                if (effect.When != when)
                    continue;
                Execute(runner, effect, ctx);
            }
        }

        /// <summary>Runs a card's own Immediate effects across all of its abilities — how a resolving
        /// Surge/Algorithm/Activate ability plays out.</summary>
        public static void RunImmediate(MatchRunner runner, CardInstance source, EffectContext ctx)
        {
            for (var i = 0; i < source.Printing.Abilities.Count; i++)
                RunAbility(runner, source.Printing.Abilities[i], ctx, EffectWhen.Immediate);
        }

        /// <summary>Fires a hook on a single card's own abilities (OnEnter, OnRemoved, OnBanished,
        /// OnHold, OnAnswer, OnPressDeclared/DealtDamage/RemovedBody, OnClashBegin/End).</summary>
        public static void FireOwnHook(MatchRunner runner, CardInstance card, EffectWhen hook, EffectContext ctx)
        {
            if (card?.Printing == null)
                return;
            for (var i = 0; i < card.Printing.Abilities.Count; i++)
                RunAbility(runner, card.Printing.Abilities[i], ctx, hook);
        }

        /// <summary>OnStoreAction fires on every permanent the acting player controls, filtered by
        /// a pipe-delimited list of StoreActionKind names (e.g. "List|Buy"); an empty filter
        /// matches every kind.</summary>
        public static void FireOnStoreAction(MatchRunner runner, Player player, StoreActionKind kind)
        {
            for (var i = 0; i < player.Field.Count; i++)
            {
                var card = player.Field[i];
                var ctx = new EffectContext(card, player);
                for (var a = 0; a < card.Printing.Abilities.Count; a++)
                {
                    var ability = card.Printing.Abilities[a];
                    for (var e = 0; e < ability.Effects.Count; e++)
                    {
                        var effect = ability.Effects[e];
                        if (effect.When != EffectWhen.OnStoreAction)
                            continue;
                        if (!MatchesStoreFilter(effect.Filter, kind))
                            continue;
                        Execute(runner, effect, ctx);
                    }
                }
            }
        }

        static bool MatchesStoreFilter(string filter, StoreActionKind kind)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;
            var parts = filter.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (Enum.TryParse<StoreActionKind>(parts[i].Trim(), true, out var parsed) && parsed == kind)
                    return true;
            }
            return false;
        }

        /// <summary>Evaluates a GrantKeywordOnDeclare filter at Press-declare time (4.8.50). The
        /// only filter Engine B's sample cards use is "FirstPressYouDeclareThisClash" — since only
        /// the active player declares Presses in C1, that's exactly "no Press declared yet this
        /// Clash", independent of which card carries the ability.</summary>
        public static bool EvaluateDeclareFilter(string filter, Match match)
        {
            if (string.IsNullOrWhiteSpace(filter) || filter == "FirstPressYouDeclareThisClash")
                return match.PressesDeclaredThisClash == 0;
            return false;
        }

        static CardInstance ResolveCardTarget(string targetSpec, EffectContext ctx)
        {
            if (string.IsNullOrEmpty(targetSpec) || targetSpec == "Self")
                return ctx.Source;
            if (targetSpec == "ChosenTarget")
                return ctx.ChosenTarget;
            return ctx.Source;
        }

        static void Execute(MatchRunner runner, EffectPrinting effect, EffectContext ctx)
        {
            var match = runner.Match;

            switch (effect.Op)
            {
                case EffectOp.DealDamage:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null)
                        break;
                    var damage = Math.Max(0, effect.Amount);
                    target.DamageMarked += damage;
                    runner.Emit(EventKind.DamageDealt, "source", ctx.Source.InstanceId, "target", target.InstanceId, "amount", damage, "wave", -1, "absolute", false);
                    runner.Emit(EventKind.HealthChanged, "instanceId", target.InstanceId, "current", target.CurrentHealth, "printed", target.Health);
                    break;
                }
                case EffectOp.Mend:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null)
                        break;
                    target.DamageMarked = Math.Max(0, target.DamageMarked - Math.Max(0, effect.Amount));
                    runner.Emit(EventKind.HealthChanged, "instanceId", target.InstanceId, "current", target.CurrentHealth, "printed", target.Health);
                    break;
                }
                case EffectOp.Draw:
                {
                    var count = effect.Amount > 0 ? effect.Amount : 1;
                    runner.DrawCards(ctx.Controller, count);
                    break;
                }
                case EffectOp.GainWill:
                {
                    ctx.Controller.Will += Math.Max(0, effect.Amount);
                    runner.Emit(EventKind.WillSet, "player", ctx.Controller.Id, "amount", ctx.Controller.Will);
                    break;
                }
                case EffectOp.WillPay:
                {
                    ctx.Controller.Will = Math.Max(0, ctx.Controller.Will - Math.Max(0, effect.Amount));
                    runner.Emit(EventKind.WillPaid, "player", ctx.Controller.Id, "amount", effect.Amount);
                    break;
                }
                case EffectOp.GainWorth:
                    runner.GainWorth(ctx.Controller, Math.Max(0, effect.Amount));
                    break;
                case EffectOp.WorthPay:
                    runner.PayWorth(ctx.Controller, Math.Max(0, effect.Amount));
                    break;
                case EffectOp.GainHonor:
                    runner.GainHonor(ctx.Controller, effect.Amount);
                    break;
                case EffectOp.PutCounter:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null || string.IsNullOrEmpty(effect.Counter))
                        break;
                    target.PutCounter(effect.Counter, effect.Amount);
                    runner.Emit(EventKind.CounterPut, "instanceId", target.InstanceId, "name", effect.Counter, "amount", effect.Amount);
                    break;
                }
                case EffectOp.RemoveCounter:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null || string.IsNullOrEmpty(effect.Counter))
                        break;
                    target.PutCounter(effect.Counter, -Math.Max(0, effect.Amount));
                    runner.Emit(EventKind.CounterPut, "instanceId", target.InstanceId, "name", effect.Counter, "amount", -effect.Amount);
                    break;
                }
                case EffectOp.CreateToken:
                {
                    if (!effect.Params.TryGetValue("printingId", out var tokenId) || string.IsNullOrEmpty(tokenId))
                        break;
                    var token = MatchSetup.CreateInstance(match, runner.Database, tokenId, ctx.Controller.Id, Zone.Field);
                    token.Ready = true;
                    token.Exhausted = false;
                    ctx.Controller.Field.Add(token);
                    runner.Emit(EventKind.PermanentEntered, "instanceId", token.InstanceId, "zone", Zone.Field.ToString());
                    break;
                }
                case EffectOp.MoveZone:
                {
                    ExecuteMoveZone(runner, effect, ctx);
                    break;
                }
                case EffectOp.AttachBond:
                {
                    var host = effect.Target == "ChosenTarget" ? ctx.ChosenTarget : ctx.Controller.Icon;
                    if (ctx.Source.Printing.Type == CardType.Bond && host != null)
                        runner.AttachBond(ctx.Source, host);
                    break;
                }
                case EffectOp.DetachBond:
                {
                    if (ctx.Source.Printing.Type == CardType.Bond)
                        ctx.Source.HostInstanceId = null;
                    break;
                }
                case EffectOp.Exhaust:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null)
                        break;
                    target.Ready = false;
                    target.Exhausted = true;
                    runner.Emit(EventKind.Exhausted, "instanceId", target.InstanceId);
                    break;
                }
                case EffectOp.Ready:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null)
                        break;
                    target.Ready = true;
                    target.Exhausted = false;
                    runner.Emit(EventKind.Readied, "instanceId", target.InstanceId);
                    break;
                }
                case EffectOp.GuardAdd:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null)
                        break;
                    match.GuardBonusThisClash.TryGetValue(target.InstanceId, out var existing);
                    match.GuardBonusThisClash[target.InstanceId] = existing + effect.Amount;
                    break;
                }
                case EffectOp.StrikeAdd:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null)
                        break;
                    match.StrikeBonusThisClash.TryGetValue(target.InstanceId, out var existing);
                    match.StrikeBonusThisClash[target.InstanceId] = existing + effect.Amount;
                    break;
                }
                case EffectOp.GrantKeyword:
                case EffectOp.GrantKeywordOnDeclare:
                {
                    // GrantKeywordOnDeclare's declare-time gating (4.8.50) is evaluated by the
                    // caller (DeclarePress) before this ever runs; by the time Execute sees it,
                    // granting is unconditional. Direct GrantKeyword duration/expiry isn't tracked
                    // (Engine B rule 26's "strip this-turn/this-Clash flags" sweep doesn't exist
                    // yet in this kernel) — see ENGINE_INVENTORY.md.
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null || string.IsNullOrEmpty(effect.Keyword))
                        break;
                    if (!Enum.TryParse<Keyword>(effect.Keyword, true, out var keyword))
                        break;
                    target.KeywordsNow.Add(keyword);
                    runner.Emit(EventKind.KeywordGranted, "instanceId", target.InstanceId, "keyword", keyword.ToString(), "duration", effect.Duration ?? "");
                    break;
                }
                case EffectOp.GiveFlag:
                {
                    var target = ResolveCardTarget(effect.Target, ctx);
                    if (target == null || string.IsNullOrEmpty(effect.Params.GetValueOrDefault("flag")))
                        break;
                    var flag = effect.Params["flag"];
                    if (string.Equals(effect.Duration, "ThisTurn", StringComparison.OrdinalIgnoreCase))
                        target.FlagsThisTurn.Add(flag);
                    else
                        target.FlagsThisClash.Add(flag);
                    break;
                }
                case EffectOp.Silence:
                {
                    var stackId = effect.Params.TryGetValue("stackId", out var raw) && int.TryParse(raw, out var parsed) ? parsed : (int?)null;
                    if (stackId.HasValue)
                        runner.SilenceStackObject(stackId.Value);
                    break;
                }
                case EffectOp.Sweep:
                {
                    ExecuteSweep(runner, effect, ctx);
                    break;
                }
                case EffectOp.LookStore:
                {
                    var count = effect.Amount > 0 ? effect.Amount : 1;
                    runner.PeekSupplyIntoStore(count);
                    break;
                }

                // Declared but not yet wired to runtime behavior — a card using these loads fine
                // (they're recognized EffectOp names) but the effect is a no-op until implemented:
                // PreventDamage, ReplaceRemove, SearchSupply, StoreBuy, StoreSell, StoreTrade,
                // StoreList, StoreRow, ChooseTarget, ForEach, If, Unless.
                default:
                    break;
            }
        }

        static void ExecuteMoveZone(MatchRunner runner, EffectPrinting effect, EffectContext ctx)
        {
            var match = runner.Match;
            var target = ResolveCardTarget(effect.Target, ctx);
            if (target == null || !effect.Params.TryGetValue("zone", out var zoneName))
                return;

            if (string.Equals(zoneName, "Removed", StringComparison.OrdinalIgnoreCase))
            {
                if (target.HasKeyword(Keyword.Sealed))
                    return; // 4.11.88 — Sealed stops Remove, not Banish
                var owner = match.GetPlayer(target.ControllerId);
                if (owner == null || !owner.Field.Contains(target))
                    return;
                var events = new List<GameEvent>();
                StateChecks.RemoveCard(match, target, owner, events, match.NextTs(), "EffectRemove");
                runner.RecordEvents(events);
                FireOwnHook(runner, target, EffectWhen.OnRemoved, new EffectContext(target, owner));
            }
            else if (string.Equals(zoneName, "Banished", StringComparison.OrdinalIgnoreCase))
            {
                var owner = match.GetPlayer(target.ControllerId);
                owner?.Field.Remove(target);
                target.Zone = Zone.Banished;
                target.Ready = false;
                match.Banished.Add(target);
                runner.Emit(EventKind.PermanentLeft, "instanceId", target.InstanceId, "zone", Zone.Field.ToString(), "reason", "EffectBanish");
                if (owner != null)
                    FireOwnHook(runner, target, EffectWhen.OnBanished, new EffectContext(target, owner));
            }
        }

        static void ExecuteSweep(MatchRunner runner, EffectPrinting effect, EffectContext ctx)
        {
            var match = runner.Match;
            var count = effect.Amount > 0 ? effect.Amount : 1;
            var filter = string.IsNullOrWhiteSpace(effect.Filter) ? "OpponentField" : effect.Filter;

            var candidates = new List<CardInstance>();
            for (var p = 0; p < match.Players.Count; p++)
            {
                var player = match.Players[p];
                var include = filter switch
                {
                    "OwnField" => player.Id == ctx.Controller.Id,
                    "AllField" => true,
                    _ => player.Id != ctx.Controller.Id, // "OpponentField" and any unrecognized filter
                };
                if (!include)
                    continue;
                for (var i = 0; i < player.Field.Count; i++)
                {
                    var card = player.Field[i];
                    if (card.Printing.Type == CardType.Companion || card.Printing.Type == CardType.Token)
                        candidates.Add(card);
                }
            }

            var removed = 0;
            for (var i = 0; i < candidates.Count && removed < count; i++)
            {
                var body = candidates[i];
                if (body.HasKeyword(Keyword.Sealed))
                    continue;
                var owner = match.GetPlayer(body.ControllerId);
                var events = new List<GameEvent>();
                StateChecks.RemoveCard(match, body, owner, events, match.NextTs(), "Sweep");
                runner.RecordEvents(events);
                FireOwnHook(runner, body, EffectWhen.OnRemoved, new EffectContext(body, owner));
                removed++;
            }
        }
    }
}
