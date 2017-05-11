using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class StateTest {
        [TestMethod]
        public void IsFinishedWithSlowUpdateTest() {
            var game = new GameInstance(3);

            var a1 = game.AddAbilityWithInfo(new AbilityInfo(5, 1, 5, 0));

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 1, 10, 0, new[] {a1}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 1, 10, 0, new[] {a1}));

            game.PrepareEverything();

            game.State.SlowUpdateIsFinished(game.MobManager);
            Assert.IsFalse(game.IsFinished);

            ActionEvaluator.FNoCopy(game, UctAction.AbilityUseAction(a1, m1, m2));

            game.State.SlowUpdateIsFinished(game.MobManager);
            Assert.IsTrue(game.IsFinished);
        }

        [TestMethod]
        public void IsFinishedFastAutoUpdateTest() {
            var game = new GameInstance(3);

            var a1 = game.AddAbilityWithInfo(new AbilityInfo(5, 1, 5, 0));

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 1, 10, 0, new[] {a1}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 1, 10, 0, new[] {a1}));

            game.PrepareEverything();

            Assert.IsFalse(game.IsFinished);

            ActionEvaluator.FNoCopy(game, UctAction.AbilityUseAction(a1, m1, m2));
            Assert.IsTrue(game.IsFinished);
        }
    }
}