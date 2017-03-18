using HexMage.Simulator.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class StateTest {
        [TestMethod]
        public void IsFinishedWithSlowUpdateTest() {
            var game = new GameInstance(3);

            var a1 = game.AddAbilityWithInfo(new Ability(5, 1, 5, 0, AbilityElement.Fire));

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 1, 10, 0, new[] {a1}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 1, 10, 0, new[] {a1}));

            game.PrepareEverything();

            game.State.SlowUpdateIsFinished(game.MobManager);
            Assert.IsFalse(game.IsFinished);

            game.FastUse(a1, m1, m2);

            game.State.SlowUpdateIsFinished(game.MobManager);
            Assert.IsTrue(game.IsFinished);
        }

        [TestMethod]
        public void IsFinishedFastAutoUpdateTest() {
            var game = new GameInstance(3);

            var a1 = game.AddAbilityWithInfo(new Ability(5, 1, 5, 0, AbilityElement.Fire));

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 1, 10, 0, new[] {a1}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 1, 10, 0, new[] {a1}));

            game.PrepareEverything();

            Assert.IsFalse(game.IsFinished);

            game.FastUse(a1, m1, m2);
            Assert.IsTrue(game.IsFinished);
        }


        [TestMethod]
        public void IsFinishedFastAutoUpdateWithDotsTest() {
            var game = new GameInstance(3);

            var a1 = game.AddAbilityWithInfo(new Ability(0, 1, 5, 0, AbilityElement.Fire));

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 1, 10, 0, new[] {a1}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 1, 10, 0, new[] {a1}));

            game.PrepareEverything();
            Assert.IsFalse(game.IsFinished);

            game.FastUse(a1, m1, m2);
            Assert.IsFalse(game.IsFinished);

            game.NextMobOrNewTurn();
            game.NextMobOrNewTurn();

            Assert.AreEqual(0, game.State.MobInstances[m2].Hp);
            Assert.AreEqual(0, game.State.BlueAlive);
            Assert.IsTrue(game.IsFinished);
        }
    }
}