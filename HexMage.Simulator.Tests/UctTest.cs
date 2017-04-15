using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class UctTest {
        [TestMethod]
        public void AttackMoveEquivalentToSeparateActionsTest() {
            var game = GameSetup.GenerateForDnaSettings(1, 1);

            GameSetup.ResetPositions(game);
            game.PrepareEverything();

            var mobId = game.MobManager.Mobs[0];
            var mobInfo = game.MobManager.MobInfos[mobId];
            var abilityId = mobInfo.Abilities[0];
            var targetId = game.MobManager.Mobs.First(m => game.MobManager.MobInfos[m].Team != mobInfo.Team);

            var c = new AxialCoord(0, 1);

            var attackMoveAction = UctAction.AttackMoveAction(mobId, c, abilityId, targetId);
            GameInvariants.AssertValidAction(game, attackMoveAction);

            var afterAttackMove = ActionEvaluator.F(game, attackMoveAction);

            var moveAction = UctAction.MoveAction(mobId, c);
            GameInvariants.AssertValidAction(game, moveAction);
            var afterMove = ActionEvaluator.F(game, moveAction);

            var abilityUseAction = UctAction.AbilityUseAction(abilityId, mobId, targetId);
            GameInvariants.AssertValidAction(afterMove, abilityUseAction);
            var afterMoveAndAttack = ActionEvaluator.F(afterMove, abilityUseAction);

            TestHelpers.GameInstancesEqual(afterAttackMove, afterMoveAndAttack);
        }

        [TestMethod]
        public void BasicUctTest() {
            var dna = new DNA(1, 1);
            dna.Randomize();

            var game = GameSetup.GenerateFromDna(dna, dna);

            var root = new UctNode(UctAction.NullAction(), game);

            var result = new UctAlgorithm(100).UctSearch(game);

            foreach (var action in result.Actions) {
                Console.WriteLine(action);
            }
        }

        [TestMethod]
        public void FlatMonteCarloTest() {
            var dna = new DNA(1, 1);
            dna.Randomize();

            var game = GameSetup.GenerateFromDna(dna, dna);
            Assert.IsTrue(game.CurrentTeam.HasValue);

            var startingTeam = game.CurrentTeam.Value;

            var root = new UctNode(UctAction.NullAction(), game);

            var uct = new UctAlgorithm(100);

            for (int i = 0; i < 30; i++) {
                uct.OneIteration(root, startingTeam);
                uct.OneIteration(root, startingTeam);
                uct.OneIteration(root, startingTeam);
                uct.OneIteration(root, startingTeam);
                uct.OneIteration(root, startingTeam);
                uct.OneIteration(root, startingTeam);
                uct.OneIteration(root, startingTeam);
            }

            UctDebug.PrintDotgraph(root, () => 0);
            //var endTurnChild = UctAlgorithm.Expand(root);
            //var nullChild = UctAlgorithm.Expand(root);

            //float endTurnReward = UctAlgorithm.DefaultPolicy(endTurnChild.State, startingTeam);
            //float nullReward = UctAlgorithm.DefaultPolicy(nullChild.State, startingTeam);

            //UctAlgorithm.Backup(endTurnChild, endTurnReward);
            //UctAlgorithm.Backup(nullChild, nullReward);

            //UctDebug.PrintDotgraph(root);

            //var mc = new FlatMonteCarlo();

            //var endNode = new UctNode(UctAction.EndTurnAction(), game.DeepCopy());

            //var ability = game.MobManager.MobInfos[0].Abilities[0];
            //var abilityAction = UctAction.AbilityUseAction(ability,
            //                                               game.MobManager.Mobs[0],
            //                                               game.MobManager.Mobs[1]);
            //var abilityNode = new UctNode(abilityAction, game.DeepCopy());

            //var result = FlatMonteCarlo.Search(game);

            //Console.WriteLine(result);
        }

        [TestMethod]
        public void StateDeepCopyTest() {
            var game = new GameInstance(3);

            var ability = new AbilityInfo(3, 1, 1, 0, AbilityElement.Fire);
            var abilityId = game.AddAbilityWithInfo(ability);

            var abilities1 = new List<int>();
            var abilities2 = new List<int> {
                abilityId
            };

            var info1 = new MobInfo(TeamColor.Red, 5, 1, 0, abilities1);
            var info2 = new MobInfo(TeamColor.Blue, 5, 1, 1, abilities2);

            var m1 = game.AddMobWithInfo(info1);
            var m2 = game.AddMobWithInfo(info2);
            game.PrepareEverything();

            var copy = game.CopyStateOnly();

            TestHelpers.GameInstancesEqual(game, copy);

            var copy2 = copy.CopyStateOnly();
            ActionEvaluator.F(copy2, UctAction.AbilityUseAction(abilityId, m1, m2));

            TestHelpers.GameInstancesEqual(game, copy);

            TestHelpers.MobManagersEqual(game.MobManager, copy2.MobManager);
            TestHelpers.MapsEqual(game.Map, copy2.Map);
        }

        [TestMethod]
        public void DefaultPolicyTest() {
            var game = new GameInstance(3);

            var ability1 = new AbilityInfo(1, 1, 1, 0, AbilityElement.Fire);
            var a1 = game.AddAbilityWithInfo(ability1);

            var ability2 = new AbilityInfo(3, 1, 1, 0, AbilityElement.Fire);
            var a2 = game.AddAbilityWithInfo(ability2);

            var abilities1 = new List<int>();
            var abilities2 = new List<int> {
                a1,
                a2
            };

            var info1 = new MobInfo(TeamColor.Red, 5, 1, 0, abilities1);
            var info2 = new MobInfo(TeamColor.Blue, 5, 1, 1, abilities2);

            game.AddMobWithInfo(info1);
            game.AddMobWithInfo(info2);
            game.PrepareEverything();

            Assert.IsFalse(game.IsFinished);

            var uct = new UctAlgorithm(100);
            var result = UctAlgorithm.DefaultPolicy(game, TeamColor.Red);

            Assert.AreEqual(0, result);
            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());

            Assert.AreEqual(TeamColor.Blue, game.CurrentTeam);

            var bestAction = ActionGenerator.DefaultPolicyAction(game);
            Console.WriteLine($"Best: {bestAction}");

            var node = uct.UctSearch(game);
            Console.WriteLine(node);
            //node.Parent.Print(0);
        }


        [TestMethod]
        public void NodeActionComputeTest() {
            var game = new GameInstance(3);

            var ability = new AbilityInfo(3, 1, 1, 0, AbilityElement.Fire);
            var abilityId = game.AddAbilityWithInfo(ability);

            var abilities1 = new List<int>();
            var abilities2 = new List<int> {
                abilityId
            };

            var info1 = new MobInfo(TeamColor.Red, 5, 1, 0, abilities1);
            var info2 = new MobInfo(TeamColor.Blue, 5, 1, 1, abilities2);

            var m1 = game.AddMobWithInfo(info1);
            var m2 = game.AddMobWithInfo(info2);

            game.PlaceMob(m1, new AxialCoord(1, 1));
            game.PlaceMob(m2, new AxialCoord(-1, -1));

            game.PrepareEverything();

            Assert.IsTrue(game.CurrentMob.HasValue);
            Assert.AreEqual(m1, game.CurrentMob.Value);

            var firstNode = new UctNode(0, 0, UctAction.NullAction(), game);
            firstNode.PrecomputePossibleActions(true, true);

            Assert.AreEqual(3, firstNode.PossibleActions.Count);

            //var moveActions = firstNode.PossibleActions.Where(x => x.Type != UctActionType.EndTurn).ToList();
            //Assert.AreEqual(6, moveActions.Count);

            //foreach (var moveAction in moveActions) {
            //    Assert.AreEqual(UctActionType.Move, moveAction.Type);
            //}

            //ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());

            //Assert.IsTrue(game.CurrentMob.HasValue);
            //Assert.AreEqual(m2, game.CurrentMob.Value);

            //var secondNode = new UctNode(0, 0, UctAction.NullAction(), game);
            //secondNode.PrecomputePossibleActions(true, true);

            //Assert.AreEqual(8, secondNode.PossibleActions.Count);
            //var useAction = secondNode.PossibleActions.First(x => x.Type == UctActionType.AbilityUse);
            //Assert.AreEqual(m2, useAction.MobId);
            //Assert.AreEqual(m1, useAction.TargetId);
            //Assert.AreEqual(abilityId, useAction.AbilityId);

            //var updatedState = ActionEvaluator.F(game, useAction);

            //var m1i = updatedState.State.MobInstances[m1];
            //var m2i = updatedState.State.MobInstances[m2];

            //Assert.AreEqual(2, m1i.Hp);
            //Assert.AreEqual(1, m1i.Ap);
            //Assert.AreEqual(5, m2i.Hp);
            //Assert.AreEqual(0, m2i.Ap);
        }
    }
}