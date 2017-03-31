using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class UctTest {
        [TestMethod]
        public void StateDeepCopyTest() {
            var game = new GameInstance(3);

            var ability = new Ability(3, 1, 1, 0, AbilityElement.Fire);
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

            var copy = game.DeepCopy();

            TestHelpers.GameInstancesEqual(game, copy);

            var copy2 = copy.DeepCopy();
            copy2.FastUse(abilityId, m1, m2);

            TestHelpers.GameInstancesEqual(game, copy);

            TestHelpers.MobManagersEqual(game.MobManager, copy2.MobManager);
            TestHelpers.MapsEqual(game.Map, copy2.Map);
        }

        [TestMethod]
        public void DefaultPolicyTest() {
            var game = new GameInstance(3);

            var ability1 = new Ability(1, 1, 1, 0, AbilityElement.Fire);
            var a1 = game.AddAbilityWithInfo(ability1);

            var ability2 = new Ability(3, 1, 1, 0, AbilityElement.Fire);
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

            var uct = new UctAlgorithm(10);
            var result = UctAlgorithm.DefaultPolicy(game, TeamColor.Red);

            Assert.AreEqual(-1, result);
            game.NextMobOrNewTurn();

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

            var ability = new Ability(3, 1, 1, 0, AbilityElement.Fire);
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

            Assert.IsTrue(game.TurnManager.CurrentMob.HasValue);
            Assert.AreEqual(m1, game.TurnManager.CurrentMob.Value);

            var firstNode = new UctNode(0, 0, UctAction.NullAction(), game);
            firstNode.PrecomputePossibleActions(true, true);

            Assert.AreEqual(7, firstNode.PossibleActions.Count);

            var moveActions = firstNode.PossibleActions.Where(x => x.Type != UctActionType.EndTurn).ToList();
            Assert.AreEqual(6, moveActions.Count);

            foreach (var moveAction in moveActions) {
                Assert.AreEqual(UctActionType.Move, moveAction.Type);
            }

            game.TurnManager.NextMobOrNewTurn(game.Pathfinder, game.State);

            Assert.IsTrue(game.TurnManager.CurrentMob.HasValue);
            Assert.AreEqual(m2, game.TurnManager.CurrentMob.Value);

            var secondNode = new UctNode(0, 0, UctAction.NullAction(), game);
            secondNode.PrecomputePossibleActions(true, true);

            Assert.AreEqual(8, secondNode.PossibleActions.Count);
            var useAction = secondNode.PossibleActions.First(x => x.Type == UctActionType.AbilityUse);
            Assert.AreEqual(m2, useAction.MobId);
            Assert.AreEqual(m1, useAction.TargetId);
            Assert.AreEqual(abilityId, useAction.AbilityId);

            var updatedState = UctAlgorithm.F(game, useAction);

            var m1i = updatedState.State.MobInstances[m1];
            var m2i = updatedState.State.MobInstances[m2];

            Assert.AreEqual(2, m1i.Hp);
            Assert.AreEqual(1, m1i.Ap);
            Assert.AreEqual(5, m2i.Hp);
            Assert.AreEqual(0, m2i.Ap);
        }
    }
}