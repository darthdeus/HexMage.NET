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
        public void StateDeepCopyTest() {
            var game = new GameInstance(3);

            var ability = new AbilityInfo(3, 1, 1, 0);
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

            var ability1 = new AbilityInfo(1, 1, 1, 0);
            var a1 = game.AddAbilityWithInfo(ability1);

            var ability2 = new AbilityInfo(3, 1, 1, 0);
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
        }


        [TestMethod]
        public void NodeActionComputeTest() {
            var game = new GameInstance(3);

            var ability = new AbilityInfo(3, 1, 1, 0);
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
        }
    }
}