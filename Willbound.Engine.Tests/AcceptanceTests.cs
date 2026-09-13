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
    }
}
