using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class AbilityBuffTest {
        [TestMethod]
        public void AbilityElementTest() {
            var game = new GameInstance(3);

            var ability = new Ability(0, 1, 1, 0, AbilityElement.Fire);
            var abilityId = game.AddAbilityWithInfo(ability);

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 10, 10, 0, new List<int> {abilityId}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 10, 10, 1, new List<int> {}));

            game.PrepareEverything();

            game.FastUse(abilityId, m1, m2);

            Assert.AreEqual(9, game.State.MobInstances[m1].Ap);
            // No damage has been done yet
            Assert.AreEqual(10, game.State.MobInstances[m2].Hp);

            game.NextMobOrNewTurn();

            Assert.AreEqual(1, game.TurnManager.TurnNumber);
            Assert.AreEqual(m2, game.TurnManager.CurrentMob);

            // Not the fire debuff should be applied
            game.NextMobOrNewTurn();

            Assert.AreEqual(2, game.TurnManager.TurnNumber);
            Assert.AreEqual(m1, game.TurnManager.CurrentMob);

            Assert.AreEqual(9, game.State.MobInstances[m2].Hp);
            Assert.AreEqual(10, game.State.MobInstances[m1].Ap);
            Assert.AreEqual(10, game.State.MobInstances[m2].Ap);

            game.NextMobOrNewTurn();
            game.NextMobOrNewTurn();

            Assert.AreEqual(3, game.TurnManager.TurnNumber);
            Assert.AreEqual(m1, game.TurnManager.CurrentMob);

            Assert.AreEqual(8, game.State.MobInstances[m2].Hp);
            Assert.IsTrue(game.State.MobInstances[m2].Buff.IsZero);
        }
    }
}