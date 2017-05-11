using System.Collections.Generic;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class TurnOrderTest {
        [TestMethod]
        public void BasicIniciativeTest() {
            var game = new GameInstance(3);
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 10, 10, 2, new List<int>()));
            var m3 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 10, 10, 3, new List<int>()));
            var m4 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 10, 10, 4, new List<int>()));
            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 10, 10, 1, new List<int>()));

            game.PrepareEverything();

            Assert.AreEqual(game.CurrentMob, m1);

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m2);

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m3);

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m4);

            // At this point a new turn should start

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m1);

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m2);

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m3);

            ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            Assert.AreEqual(game.CurrentMob, m4);
        }
    }
}