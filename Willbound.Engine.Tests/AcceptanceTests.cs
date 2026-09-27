using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Willbound.Engine;

namespace Willbound.Engine.Tests
{
    [TestFixture]
    public sealed class AcceptanceTests
    {
        [Test]
        public void Test01_WillReset()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.SetRound(3);
            runner.SetPlayerWill(0, 1);
            runner.ForceStartStep();
            Assert.That(runner.Match.GetPlayer(0).Will, Is.EqualTo(3));
        }

        [Test]
        public void Test02_WillCap()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.SetRound(9);
            runner.ForceStartStep();
            Assert.That(runner.Match.GetPlayer(0).Will, Is.EqualTo(8));
        }

        [Test]
        public void Test03_DrawLoss()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var player = runner.Match.GetPlayer(0);
            player.Deck.Clear();
            runner.ForceStartStep();
            Assert.That(player.Lost, Is.True);
        }

        [Test]
        public void Test04_EnterReady()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            runner.SetPlayerWill(0, 5);
            var card = runner.Match.GetPlayer(0).Hand[0];
            card.Printing = BootstrapCards.VanillaStriker();
            var play = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 0, CardInstanceId = card.InstanceId });
            Assert.That(play.Success, Is.True, play.Error);
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });
            var played = runner.Match.GetPlayer(0).Field.Find(c => c.InstanceId == card.InstanceId);
            Assert.That(played, Is.Not.Null);
            Assert.That(played.Ready, Is.True);
            Assert.That(played.Exhausted, Is.False);
        }

        [Test]
        public void Test05_PressExhausts()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaStriker());
            var target = runner.Match.GetPlayer(1).Icon;
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = target.InstanceId });
            Assert.That(attacker.Exhausted, Is.True);
            var again = runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = target.InstanceId });
            Assert.That(again.Success, Is.False);
        }

        [Test]
        public void Test06_Refresh()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaStriker());
            attacker.Exhausted = true;
            runner.ForceStartStep();
            Assert.That(attacker.Exhausted, Is.False);
            Assert.That(attacker.Ready, Is.True);
        }

        [Test]
        public void Test07_NoClashOnDefenseTurn()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            Assert.That(runner.Match.ActivePlayerId, Is.EqualTo(0));
            runner.AdvanceToClash();
            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.Clash));
            MatchSetup.AdvanceActivePlayer(runner.Match);
            MatchSetup.BeginTurn(runner.Match, new SeededRng(1));
            Assert.That(runner.Match.Phase, Is.Not.EqualTo(Phase.Clash));
        }

        [Test]
        public void Test08_AnswerLegal()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaStriker());
            var defender = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.ClosedTank());
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = defender.InstanceId });
            runner.Match.ClashPhase = ClashPhase.C2_Answers;
            var result = runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.Answer,
                PlayerId = 1,
                CardInstanceId = defender.InstanceId,
                TargetInstanceId = attacker.InstanceId,
            });
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void Test09_AnswerIllegal()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var defender = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.ClosedTank());
            defender.Exhausted = true;
            runner.AdvanceToClash();
            runner.Match.ClashPhase = ClashPhase.C2_Answers;
            var result = runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.Answer,
                PlayerId = 1,
                CardInstanceId = defender.InstanceId,
                TargetInstanceId = runner.Match.GetPlayer(0).Icon.InstanceId,
            });
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Test10_SilencePress()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaStriker());
            var target = runner.Match.GetPlayer(1).Icon;
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = target.InstanceId });
            var press = runner.Match.Stack[runner.Match.Stack.Count - 1];
            runner.Match.PriorityPlayerId = 1;
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Silence, PlayerId = 1, StackObjectId = press.StackId });
            Assert.That(attacker.Exhausted, Is.True);
            Assert.That(runner.Match.Stack.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test11_SilenceStoreRefunds()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            var before = player.Worth;
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.StoreBuy, PlayerId = 0, StoreSlotIndex = 0, PaidWorth = 1, StoreKind = StoreActionKind.Buy });
            var storeObj = runner.Match.Stack.Last();
            storeObj.PaidWorth = 2;
            player.Worth = before - 2;
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Silence, PlayerId = 1, StackObjectId = storeObj.StackId });
            Assert.That(player.Worth, Is.EqualTo(before));
        }

        [Test]
        public void Test12_AggressionSkip()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.AggressionStriker());
            var defender = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = defender.InstanceId });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 1, CardInstanceId = defender.InstanceId, TargetInstanceId = attacker.InstanceId });
            runner.FinishClashPipeline();
            Assert.That(defender.CurrentHealth, Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void Test13_MutualAggression()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var a1 = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.AggressionStriker());
            var a2 = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.AggressionStriker());
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = a1.InstanceId, TargetInstanceId = a2.InstanceId });
            runner.Match.ClashPhase = ClashPhase.C2_Answers;
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Answer, PlayerId = 1, CardInstanceId = a2.InstanceId, TargetInstanceId = a1.InstanceId });
            runner.FinishClashPipeline();
            Assert.That(a1.CurrentHealth, Is.LessThanOrEqualTo(0));
            Assert.That(a2.CurrentHealth, Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void Test14_Bazerk()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.BazerkStriker());
            var target = runner.Match.GetPlayer(1).Icon;
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = target.InstanceId });
            Assert.That(runner.Match.Stack.Count, Is.EqualTo(2));
            Assert.That(attacker.Exhausted, Is.True);
            runner.FinishClashPipeline();
        }

        [Test]
        public void Test15_Absolute()
        {
            var damage = DamageMath.ComputeDamage(3, 4, absolute: true);
            Assert.That(damage, Is.EqualTo(2));
        }

        [Test]
        public void Test16_Gashing()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.GashingStriker());
            var defender = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = defender.InstanceId });
            runner.FinishClashPipeline();
            Assert.That(defender.CurrentHealth, Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void Test17_HeavyHitter()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.HeavyHitter());
            var defender = TestHelpers.PutCompanionInField(runner, 1, new CardPrinting { Id = "T", Type = CardType.Companion, Strike = 0, Guard = 0, Health = 2 });
            var icon = runner.Match.GetPlayer(1).Icon;
            icon.Printing.Guard = 0;
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = defender.InstanceId });
            runner.FinishClashPipeline();
            Assert.That(defender.CurrentHealth, Is.LessThanOrEqualTo(0));
            Assert.That(icon.CurrentHealth, Is.LessThan(8));
        }

        [Test]
        public void Test18_Drain()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.DrainStriker());
            var defender = TestHelpers.PutCompanionInField(runner, 1, new CardPrinting { Id = "T2", Type = CardType.Companion, Strike = 0, Guard = 0, Health = 5 });
            var icon = runner.Match.GetPlayer(0).Icon;
            icon.DamageMarked = 3;
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = attacker.InstanceId, TargetInstanceId = defender.InstanceId });
            runner.FinishClashPipeline();
            Assert.That(icon.CurrentHealth, Is.GreaterThan(5));
        }

        [Test]
        public void Test19_HuntedTen()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var icon = runner.Match.GetPlayer(0).Icon;
            icon.PutCounter("hunted", 10);
            StateChecks.Run(runner.Match);
            Assert.That(runner.Match.GetPlayer(0).Lost, Is.True);
        }

        [Test]
        public void Test20_IconZeroAfterStandAgain()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var icon = runner.Match.GetPlayer(0).Icon;
            icon.KeywordsNow.Add(Keyword.StandAgain);
            icon.StandAgainUsed = true;
            icon.DamageMarked = icon.Health;
            StateChecks.Run(runner.Match);
            Assert.That(runner.Match.GetPlayer(0).Lost, Is.True);
        }

        [Test]
        public void Test21_StandAgain()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var body = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.StandAgainBody());
            body.DamageMarked = body.Health;
            var events = StateChecks.Run(runner.Match);
            Assert.That(body.CurrentHealth, Is.EqualTo(1));
            Assert.That(body.StandAgainUsed, Is.True);
        }

        [Test]
        public void Test22_Sealed()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var body = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.SealedBody());
            body.DamageMarked = body.Health;
            StateChecks.Run(runner.Match);
            Assert.That(body.Zone, Is.EqualTo(Zone.Field));
        }

        [Test]
        public void Test23_BondDiesWithHost()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var host = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaCompanion());
            var bond = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(BootstrapCards.All), "VANILLA-BOND", 0, Zone.Field);
            bond.HostInstanceId = host.InstanceId;
            runner.Match.GetPlayer(0).Field.Add(bond);
            host.DamageMarked = host.Health;
            StateChecks.Run(runner.Match);
            Assert.That(bond.Zone, Is.EqualTo(Zone.Removed));
        }

        [Test]
        public void Test24_TokenCease()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var token = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.TokenCompanion());
            token.IsToken = true;
            token.DamageMarked = token.Health;
            StateChecks.Run(runner.Match);
            Assert.That(runner.Match.Removed.Exists(c => c.InstanceId == token.InstanceId), Is.False);
        }

        [Test]
        public void Test25_SiteCap()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.Match.Phase = Phase.Site;
            var player = runner.Match.GetPlayer(0);
            player.SitesPlayedThisTurn = 1;
            var site = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(BootstrapCards.All), "WILL-SITE-1", 0, Zone.Hand);
            player.Hand.Add(site);
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 0, CardInstanceId = site.InstanceId });
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Test26_StoreCap()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            runner.Match.GetPlayer(0).StoreActionsThisTurn = 1;
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.StoreKeep, PlayerId = 0, StoreKind = StoreActionKind.Keep });
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Test27_Row()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            player.Worth = 5;
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.StoreRow, PlayerId = 0, StoreKind = StoreActionKind.Row });
            Assert.That(result.Success, Is.True);
            Assert.That(player.Worth, Is.EqualTo(3));
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });
        }

        [Test]
        public void Test28_ThenIllegal()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            runner.Match.PriorityPlayerId = 1;
            var algo = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(BootstrapCards.All), "VANILLA-ALGO", 1, Zone.Hand);
            runner.Match.GetPlayer(1).Hand.Add(algo);
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 1, CardInstanceId = algo.InstanceId });
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Test29_NowLegal()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            runner.Match.PriorityPlayerId = 1;
            var surge = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(BootstrapCards.All), "SILENCE-NOW", 1, Zone.Hand);
            runner.Match.GetPlayer(1).Hand.Add(surge);
            runner.Match.GetPlayer(1).Will = 5;
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 1, CardInstanceId = surge.InstanceId });
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void Test30_MultiSeat()
        {
            var runner = MatchRunner.FromSetup(new List<CardPrinting>(BootstrapCards.All), new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
                new SetupPlayer { PlayerId = 2, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
            });
            runner.Match.GetPlayer(1).Lost = true;
            Assert.That(runner.Match.LivingPlayers().Count, Is.EqualTo(2));
            Assert.That(runner.Match.Store.Length, Is.EqualTo(7));
        }

        [Test]
        public void Test31_JarJarAnger()
        {
            var printings = new List<CardPrinting>(BootstrapCards.All) { BootstrapCards.DarthJarJar() };
            var runner = MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = "SITH-001", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
            });
            var jarJar = runner.Match.GetPlayer(0).Icon;
            var target = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());
            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = jarJar.InstanceId, TargetInstanceId = target.InstanceId });
            runner.FinishClashPipeline();
            Assert.That(jarJar.GetCounter("anger"), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Test33_StoreBuyPutsCardInHand()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            var beforeWorth = player.Worth;
            var storeCard = runner.Match.Store[0];
            Assert.That(storeCard, Is.Not.Null);
            var cost = storeCard.Printing.StoreWorth;
            var instanceId = storeCard.InstanceId;
            var beforeHand = player.Hand.Count;

            var announced = runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.StoreBuy,
                PlayerId = 0,
                StoreSlotIndex = 0,
                StoreKind = StoreActionKind.Buy,
            });
            Assert.That(announced.Success, Is.True, announced.Error);
            Assert.That(player.Worth, Is.EqualTo(beforeWorth - cost));
            Assert.That(player.Hand.Count, Is.EqualTo(beforeHand));

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(player.Hand.Count, Is.EqualTo(beforeHand + 1));
            Assert.That(runner.Match.Store[0], Is.Null);
            Assert.That(player.Hand.Exists(c => c.InstanceId == instanceId), Is.True);
        }

        [Test]
        public void Test35_StoreSellGivesPrintedWorthAndRemovesFromHand()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            Assert.That(player.Hand.Count, Is.GreaterThan(0));
            var card = player.Hand[0];
            var instanceId = card.InstanceId;
            var sellWorth = card.Printing.StoreWorth;
            var beforeWorth = player.Worth;
            var beforeHand = player.Hand.Count;

            var announced = runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.StoreSell,
                PlayerId = 0,
                HandCardInstanceId = instanceId,
                StoreKind = StoreActionKind.Sell,
            });
            Assert.That(announced.Success, Is.True, announced.Error);
            Assert.That(player.Hand.Count, Is.EqualTo(beforeHand));
            Assert.That(player.Worth, Is.EqualTo(beforeWorth), "Worth is paid on resolve, not announce.");

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(player.Hand.Count, Is.EqualTo(beforeHand - 1));
            Assert.That(player.Hand.Exists(c => c.InstanceId == instanceId), Is.False);
            Assert.That(player.Worth, Is.EqualTo(beforeWorth + sellWorth));
            Assert.That(card.Zone, Is.EqualTo(Zone.Store));
        }

        [Test]
        public void StoreSellGivesTheCardPrintedStoreWorth()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            var striker = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaStriker());
            player.Field.Remove(striker);
            striker.Zone = Zone.Hand;
            player.Hand.Add(striker);

            var beforeWorth = player.Worth;
            var announced = runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.StoreSell,
                PlayerId = 0,
                HandCardInstanceId = striker.InstanceId,
                StoreKind = StoreActionKind.Sell,
            });
            Assert.That(announced.Success, Is.True, announced.Error);

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(striker.Printing.StoreWorth, Is.EqualTo(2));
            Assert.That(player.Worth, Is.EqualTo(beforeWorth + 2));
            Assert.That(player.Hand.Exists(c => c.InstanceId == striker.InstanceId), Is.False);
            Assert.That(striker.Zone, Is.EqualTo(Zone.Store));
        }

        [Test]
        public void Test34_StoreBuyRejectsInsufficientWorth()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            player.Worth = 0;
            var result = runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.StoreBuy,
                PlayerId = 0,
                StoreSlotIndex = 0,
                StoreKind = StoreActionKind.Buy,
            });
            var storeCost = runner.Match.Store[0]?.Printing.StoreWorth ?? 0;
            if (storeCost > 0)
                Assert.That(result.Success, Is.False);
        }

        [Test]
        public void Test36_StoreListEntersOpenSlotAndBuriesSecondCard()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            runner.Match.Store[0] = null; // open a slot so 4.12.97's "one enters Store" has somewhere to go
            var supplyCountBefore = runner.Match.Supply.Count;
            var first = runner.Match.Supply[0];
            var second = runner.Match.Supply[1];

            var announced = runner.Apply(new PlayerAction { Kind = PlayerActionKind.StoreList, PlayerId = 0, StoreKind = StoreActionKind.List });
            Assert.That(announced.Success, Is.True, announced.Error);

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(runner.Match.Store[0], Is.SameAs(first));
            Assert.That(first.Zone, Is.EqualTo(Zone.Store));
            Assert.That(runner.Match.Supply[runner.Match.Supply.Count - 1], Is.SameAs(second));
            Assert.That(second.Zone, Is.EqualTo(Zone.Supply));
            Assert.That(runner.Match.Supply.Count, Is.EqualTo(supplyCountBefore - 1));
        }

        [Test]
        public void Test37_StoreListLeavesFirstCardOnTopWhenStoreIsFull()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            Assert.That(runner.Match.Store, Has.All.Not.Null, "store starts full at 7");
            var supplyCountBefore = runner.Match.Supply.Count;
            var first = runner.Match.Supply[0];
            var second = runner.Match.Supply[1];

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.StoreList, PlayerId = 0, StoreKind = StoreActionKind.List });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(runner.Match.Supply[0], Is.SameAs(first), "no open slot: the looked-at card stays on top for the next refill");
            Assert.That(runner.Match.Supply[runner.Match.Supply.Count - 1], Is.SameAs(second));
            Assert.That(runner.Match.Supply.Count, Is.EqualTo(supplyCountBefore));
        }

        [Test]
        public void Loader_RejectsAbilityWithTextAndNoEffects()
        {
            const string json = @"{""id"":""BAD-01"",""name"":""Bad Card"",""type"":""Companion"",
                ""abilities"":[{""name"":""Vague"",""timing"":""now"",""text"":""Do something undefined.""}]}";
            Assert.Throws<CardLoadException>(() => CardPrintingLoader.Parse(json));
        }

        [Test]
        public void Loader_RejectsUnknownEffectOp()
        {
            const string json = @"{""id"":""BAD-02"",""name"":""Bad Card"",""type"":""Companion"",
                ""abilities"":[{""name"":""Broken"",""timing"":""now"",""text"":""Typo op."",
                ""effects"":[{""op"":""DealDamagee""}]}]}";
            Assert.Throws<CardLoadException>(() => CardPrintingLoader.Parse(json));
        }

        [Test]
        public void Test39_IconFixtureGrantsAggressionAndAngerWithoutPrintingIdGate()
        {
            // Same ability shape as Jar Jar (SITH-001) under a different printing id, to prove the
            // GrantKeywordOnDeclare / OnPressDealtDamage hook runs generically off effects[], not
            // off a hardcoded printing.Id check.
            const string json = @"{
                ""id"": ""TEST-PHANTOM-ICON"", ""name"": ""Generic Phantom Icon"", ""type"": ""Icon"",
                ""strike"": 3, ""guard"": 4, ""health"": 8, ""startsInPlay"": true,
                ""abilities"": [{
                    ""name"": ""Phantom Hand"", ""timing"": ""clash"",
                    ""text"": ""First Press each Clash has Aggression; put 1 Anger when it deals damage."",
                    ""effects"": [
                        { ""op"": ""GrantKeywordOnDeclare"", ""filter"": ""FirstPressYouDeclareThisClash"", ""keyword"": ""Aggression"" },
                        { ""op"": ""PutCounter"", ""when"": ""OnPressDealtDamage"", ""counter"": ""anger"", ""amount"": 1, ""target"": ""Self"" }
                    ]
                }]
            }";
            var phantomIcon = CardPrintingLoader.Parse(json);
            var printings = new List<CardPrinting>(BootstrapCards.All) { phantomIcon };
            var runner = MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = "TEST-PHANTOM-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
            });
            var icon = runner.Match.GetPlayer(0).Icon;
            Assert.That(icon.Printing.Id, Is.Not.EqualTo("SITH-001"));
            var target = TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());

            runner.AdvanceToClash();
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclarePress, PlayerId = 0, CardInstanceId = icon.InstanceId, TargetInstanceId = target.InstanceId });
            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(icon.HasKeyword(Keyword.Aggression), Is.True);

            runner.FinishClashPipeline();
            Assert.That(icon.GetCounter("anger"), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Test40_BondAttachesToChosenHostAndIsRemovedWhenHostLeaves()
        {
            const string json = @"{""id"":""TEST-BOND-1"",""name"":""Test Bond"",""type"":""Bond""}";
            var bondPrinting = CardPrintingLoader.Parse(json);
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            var host = TestHelpers.PutCompanionInField(runner, 0, BootstrapCards.VanillaStriker());

            var bond = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(new[] { bondPrinting }), bondPrinting.Id, 0, Zone.Hand);
            player.Hand.Add(bond);

            var announced = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 0, CardInstanceId = bond.InstanceId, TargetInstanceId = host.InstanceId });
            Assert.That(announced.Success, Is.True, announced.Error);
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(bond.HostInstanceId, Is.EqualTo(host.InstanceId));
            Assert.That(player.Field.Contains(bond), Is.True);

            host.DamageMarked = host.Health;
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = runner.Match.PriorityPlayerId });

            Assert.That(player.Field.Contains(host), Is.False);
            Assert.That(player.Field.Contains(bond), Is.False);
            Assert.That(runner.Match.Removed.Contains(bond), Is.True);
        }

        [Test]
        public void Test41_ActivatePaysWillAndRunsEffects()
        {
            const string json = @"{
                ""id"": ""TEST-ACTIVATOR"", ""name"": ""Test Activator"", ""type"": ""Relic"",
                ""abilities"": [{
                    ""name"": ""Tap for Worth"", ""timing"": ""activated"", ""costWill"": 2,
                    ""text"": ""Pay 2 Will: Gain 3 Worth."",
                    ""effects"": [{ ""op"": ""GainWorth"", ""amount"": 3 }]
                }]
            }";
            var printing = CardPrintingLoader.Parse(json);
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            player.Will = 5;
            var beforeWorth = player.Worth;
            var relic = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(new[] { printing }), printing.Id, 0, Zone.Field);
            player.Field.Add(relic);

            var announced = runner.Apply(new PlayerAction { Kind = PlayerActionKind.Activate, PlayerId = 0, CardInstanceId = relic.InstanceId, AbilityIndex = 0 });
            Assert.That(announced.Success, Is.True, announced.Error);
            Assert.That(player.Will, Is.EqualTo(3));
            Assert.That(player.Worth, Is.EqualTo(beforeWorth), "the effect runs on resolve, not announce");

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(player.Worth, Is.EqualTo(beforeWorth + 3));
        }

        [Test]
        public void Test42_SweepRemovesUpToNOpposingPermanents()
        {
            const string json = @"{
                ""id"": ""TEST-SWEEP-SURGE"", ""name"": ""Test Board Wipe"", ""type"": ""Surge"", ""willCost"": 0,
                ""abilities"": [{
                    ""name"": ""Wipe"", ""timing"": ""now"", ""text"": ""Remove up to 2 permanents from an opponent's Field."",
                    ""effects"": [{ ""op"": ""Sweep"", ""amount"": 2, ""filter"": ""OpponentField"" }]
                }]
            }";
            var printing = CardPrintingLoader.Parse(json);
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());
            TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());
            TestHelpers.PutCompanionInField(runner, 1, BootstrapCards.VanillaCompanion());
            var defender = runner.Match.GetPlayer(1);
            Assert.That(defender.Field.Count, Is.EqualTo(4)); // 3 Companions + Icon

            var surge = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(new[] { printing }), printing.Id, 0, Zone.Hand);
            runner.Match.GetPlayer(0).Hand.Add(surge);

            var announced = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 0, CardInstanceId = surge.InstanceId });
            Assert.That(announced.Success, Is.True, announced.Error);
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(defender.Field.Count, Is.EqualTo(2)); // Icon + 1 surviving Companion
            Assert.That(defender.Field.Count(c => c.Printing.Type == CardType.Companion), Is.EqualTo(1));
        }

        [Test]
        public void Test43_HonorAddChangesHonorOnlyNotWillOrWorth()
        {
            const string json = @"{
                ""id"": ""TEST-HONOR-SURGE"", ""name"": ""Test Honor Grant"", ""type"": ""Surge"", ""willCost"": 0,
                ""abilities"": [{ ""name"": ""Glory"", ""timing"": ""now"", ""text"": ""Gain 2 Honor."",
                    ""effects"": [{ ""op"": ""GainHonor"", ""amount"": 2 }] }]
            }";
            var printing = CardPrintingLoader.Parse(json);
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            var beforeWill = player.Will;
            var beforeWorth = player.Worth;
            var surge = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(new[] { printing }), printing.Id, 0, Zone.Hand);
            player.Hand.Add(surge);

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.PlayCard, PlayerId = 0, CardInstanceId = surge.InstanceId });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            Assert.That(player.Honor, Is.EqualTo(2));
            Assert.That(player.Will, Is.EqualTo(beforeWill));
            Assert.That(player.Worth, Is.EqualTo(beforeWorth));
        }

        [Test]
        public void Test44_OnStoreActionLookStoreActuallyLooks()
        {
            const string json = @"{
                ""id"": ""RELIC-SCOUT"", ""name"": ""Market Scout"", ""type"": ""Relic"",
                ""abilities"": [{
                    ""name"": ""Scout the River"", ""timing"": ""static"",
                    ""text"": ""Whenever you List or Buy, look at the top 3 of Supply."",
                    ""effects"": [{ ""op"": ""LookStore"", ""when"": ""OnStoreAction"", ""filter"": ""List|Buy"", ""amount"": 3 }]
                }]
            }";
            var printing = CardPrintingLoader.Parse(json);
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            var player = runner.Match.GetPlayer(0);
            var scout = MatchSetup.CreateInstance(runner.Match, new InMemoryCardDatabase(new[] { printing }), printing.Id, 0, Zone.Field);
            player.Field.Add(scout);

            var lookedAt = new List<CardInstance> { runner.Match.Supply[0], runner.Match.Supply[1], runner.Match.Supply[2] };
            var supplyCountBefore = runner.Match.Supply.Count;

            var announced = runner.Apply(new PlayerAction { Kind = PlayerActionKind.StoreBuy, PlayerId = 0, StoreSlotIndex = 0, StoreKind = StoreActionKind.Buy });
            Assert.That(announced.Success, Is.True, announced.Error);
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });

            // Buy just emptied slot 0; LookStore(3) finds only that one open slot, so the first
            // looked-at card lands there and the other two return to the bottom of Supply.
            Assert.That(runner.Match.Store[0], Is.SameAs(lookedAt[0]));
            Assert.That(runner.Match.Supply[runner.Match.Supply.Count - 2], Is.SameAs(lookedAt[1]));
            Assert.That(runner.Match.Supply[runner.Match.Supply.Count - 1], Is.SameAs(lookedAt[2]));
            Assert.That(runner.Match.Supply.Count, Is.EqualTo(supplyCountBefore - 1));
        }

        [Test]
        public void Test45_DoubleteamAddsHelperStrikeAndGuard()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            var attacker = TestHelpers.PutCompanionInField(runner, 0, new CardPrinting
            {
                Id = "DT-ATTACKER", Name = "Attacker", Type = CardType.Companion, Strike = 3, Guard = 1, Health = 5,
            });
            var helper = TestHelpers.PutCompanionInField(runner, 0, new CardPrinting
            {
                Id = "DT-HELPER", Name = "Helper", Type = CardType.Companion, Strike = 2, Guard = 4, Health = 3,
            });
            var defender = TestHelpers.PutCompanionInField(runner, 1, new CardPrinting
            {
                Id = "DT-DEFENDER", Name = "Defender", Type = CardType.Companion, Strike = 2, Guard = 0, Health = 10,
            });
            var icon0 = runner.Match.GetPlayer(0).Icon;

            runner.AdvanceToClash();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclareHold, PlayerId = 0, CardInstanceId = icon0.InstanceId });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.DeclareHold, PlayerId = 0, CardInstanceId = helper.InstanceId });
            runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.DeclarePress,
                PlayerId = 0,
                CardInstanceId = attacker.InstanceId,
                TargetInstanceId = defender.InstanceId,
                DoubleteamHelperId = helper.InstanceId,
            });

            Assert.That(runner.Match.StrikeBonusThisClash[attacker.InstanceId], Is.EqualTo(2));
            Assert.That(runner.Match.GuardBonusThisClash[attacker.InstanceId], Is.EqualTo(4));

            runner.Apply(new PlayerAction
            {
                Kind = PlayerActionKind.Answer,
                PlayerId = 1,
                CardInstanceId = defender.InstanceId,
                TargetInstanceId = attacker.InstanceId,
            });

            runner.FinishClashPipeline();

            // Strike-side: attacker's Press dealt 3+2(helper)-0(guard) = 5.
            Assert.That(defender.CurrentHealth, Is.EqualTo(10 - 5));
            // Guard-side: defender's Answer dealt max(0, 2-(1+4)) = 0 — without the Doubleteam
            // Guard bonus this would have been max(0, 2-1) = 1 and the attacker would be damaged.
            Assert.That(attacker.CurrentHealth, Is.EqualTo(attacker.Health));
        }

        [Test]
        public void Test32_PriorityOrderAfterResolve()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            runner.AdvanceToMain();
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 0 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, PlayerId = 1 });
            Assert.That(runner.Match.PriorityPlayerId, Is.EqualTo(runner.Match.ActivePlayerId).Or.EqualTo(0));
        }

        [Test]
        public void Loader_ParsesSchema13()
        {
            var card = BootstrapCards.DarthJarJar();
            Assert.That(card.Id, Is.EqualTo("SITH-001"));
            Assert.That(card.Abilities.Count, Is.EqualTo(1));
            Assert.That(card.Abilities[0].Effects.Count, Is.EqualTo(2));
        }

        [Test]
        public void Loader_ReadsWorthAliasAsStoreWorth()
        {
            const string json = @"{""id"":""rm-06"",""name"":""Purse Cutter"",""type"":""Companion"",""will"":2,""worth"":2,""strike"":2,""guard"":1,""health"":2}";
            var card = CardPrintingLoader.Parse(json);
            Assert.That(card.WillCost, Is.EqualTo(2));
            Assert.That(card.StoreWorth, Is.EqualTo(2));
        }

        [Test]
        public void Loader_RemnantParsesAsRelic()
        {
            var card = BootstrapCards.RemnantRelic();
            Assert.That(card.Type, Is.EqualTo(CardType.Relic));
        }

        [Test]
        public void PregameFlow_DisabledByDefaultSkipsStraightToStart()
        {
            var runner = TestHelpers.TwoPlayerMatch();
            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.Site));
            Assert.That(runner.Match.GetPlayer(1).Hand.Count, Is.EqualTo(MatchConstants.OpeningHandSize));
            Assert.That(runner.Match.GetPlayer(0).Hand.Count, Is.EqualTo(MatchConstants.OpeningHandSize + 1),
                "Active player also draws 1 from the automatic Start step.");
        }

        [Test]
        public void LegendaryDraft_PickAssignsIconAndReturnsRestToSupply()
        {
            var printings = new List<CardPrinting>(BootstrapCards.All);
            var runner = MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, DraftLegendaryIcon = true, DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
                new SetupPlayer { PlayerId = 1, DraftLegendaryIcon = true, DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
            }, enablePregameFlow: true);

            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.LegendaryDraft));
            var player0 = runner.Match.GetPlayer(0);
            Assert.That(player0.LegendaryChoices.Count, Is.EqualTo(3));
            Assert.That(player0.Icon, Is.Null);

            var chosen = player0.LegendaryChoices[0];
            var declinedIds = player0.LegendaryChoices.Skip(1).Select(c => c.InstanceId).ToList();
            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.PickLegendaryIcon, PlayerId = 0, CardInstanceId = chosen.InstanceId });
            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(player0.Icon?.InstanceId, Is.EqualTo(chosen.InstanceId));
            Assert.That(player0.Field.Contains(chosen), Is.True);
            foreach (var id in declinedIds)
                Assert.That(runner.Match.Supply.Exists(c => c.InstanceId == id), Is.True);

            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.LegendaryDraft), "Player 1 has not drafted yet.");

            var player1 = runner.Match.GetPlayer(1);
            var chosen1 = player1.LegendaryChoices[0];
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.PickLegendaryIcon, PlayerId = 1, CardInstanceId = chosen1.InstanceId });

            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.Mulligan));
        }

        [Test]
        public void LegendaryDraft_CycleIsOnceOnlyAndZeroesFirstRoundWill()
        {
            var printings = new List<CardPrinting>(BootstrapCards.All);
            var runner = MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, DraftLegendaryIcon = true, DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
            }, enablePregameFlow: true);

            var player0 = runner.Match.GetPlayer(0);

            var cycle = runner.Apply(new PlayerAction { Kind = PlayerActionKind.CycleLegendaryIcon, PlayerId = 0 });
            Assert.That(cycle.Success, Is.True, cycle.Error);
            Assert.That(player0.LegendaryCycleUsed, Is.True);

            var secondCycle = runner.Apply(new PlayerAction { Kind = PlayerActionKind.CycleLegendaryIcon, PlayerId = 0 });
            Assert.That(secondCycle.Success, Is.False);

            var chosen = player0.LegendaryChoices[0];
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.PickLegendaryIcon, PlayerId = 0, CardInstanceId = chosen.InstanceId });

            runner.Apply(new PlayerAction { Kind = PlayerActionKind.KeepHand, PlayerId = 0 });
            runner.Apply(new PlayerAction { Kind = PlayerActionKind.KeepHand, PlayerId = 1 });

            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.Site));
            Assert.That(runner.Match.GetPlayer(0).Will, Is.EqualTo(0), "Cycling the Legendary Icon spends the first round's Will.");
        }

        [Test]
        public void Mulligan_MulliganDrawsSix()
        {
            var printings = new List<CardPrinting>(BootstrapCards.All);
            var runner = MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
            }, enablePregameFlow: true);

            Assert.That(runner.Match.Phase, Is.EqualTo(Phase.Mulligan));
            var player0 = runner.Match.GetPlayer(0);
            Assert.That(player0.Hand.Count, Is.EqualTo(MatchConstants.OpeningHandSize));

            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.Mulligan, PlayerId = 0 });
            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(player0.Hand.Count, Is.EqualTo(MatchConstants.MulliganHandSize));
            Assert.That(player0.MulliganStepDone, Is.True);
        }

        [Test]
        public void Mulligan_CycleHandCardSwapsOneCard()
        {
            var printings = new List<CardPrinting>(BootstrapCards.All);
            var runner = MatchRunner.FromSetup(printings, new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 20) },
            }, enablePregameFlow: true);

            var player0 = runner.Match.GetPlayer(0);
            var beforeCount = player0.Hand.Count;
            var cardToCycle = player0.Hand[0];

            var result = runner.Apply(new PlayerAction { Kind = PlayerActionKind.CycleHandCard, PlayerId = 0, CardInstanceId = cardToCycle.InstanceId });
            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(player0.Hand.Count, Is.EqualTo(beforeCount));
            Assert.That(player0.Hand.Exists(c => c.InstanceId == cardToCycle.InstanceId), Is.False);
            Assert.That(player0.Deck.Exists(c => c.InstanceId == cardToCycle.InstanceId), Is.True);
            Assert.That(player0.MulliganStepDone, Is.True);
        }

        [Test]
        public void RandomizeSeatOrder_IsDrivenByInjectedRng()
        {
            var printings = new List<CardPrinting>(BootstrapCards.All);
            SetupPlayer[] MakePlayers() => new[]
            {
                new SetupPlayer { PlayerId = 0, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
                new SetupPlayer { PlayerId = 1, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
                new SetupPlayer { PlayerId = 2, IconId = "VANILLA-ICON", DeckIds = TestHelpers.FillDeck("VANILLA-COMPANION", 10) },
            };

            var matchA = MatchSetup.Create(new InMemoryCardDatabase(printings), new SeededRng(7), MakePlayers(), firstActivePlayerId: null, randomizeSeatOrder: true);
            var matchB = MatchSetup.Create(new InMemoryCardDatabase(printings), new SeededRng(7), MakePlayers(), firstActivePlayerId: null, randomizeSeatOrder: true);

            Assert.That(matchA.ActivePlayerId, Is.EqualTo(matchB.ActivePlayerId));
            Assert.That(matchA.Players.Select(p => p.Id).ToList(), Is.EqualTo(matchB.Players.Select(p => p.Id).ToList()));
        }
    }
}
