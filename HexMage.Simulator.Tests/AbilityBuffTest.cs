﻿using System;
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

            var ability = new AbilityInfo(1, 1, 1, 0, AbilityElement.Fire);
            var abilityId = game.AddAbilityWithInfo(ability);

            var m1 = game.AddMobWithInfo(new MobInfo(TeamColor.Red, 10, 10, 0, new List<int> {abilityId}));
            var m2 = game.AddMobWithInfo(new MobInfo(TeamColor.Blue, 10, 10, 1, new List<int> {}));

            game.PrepareEverything();

            ActionEvaluator.FastUse(game, abilityId, m1, m2);

            var fireDebuff = game.State.MobInstances[m2].Buff;
            Assert.AreEqual(2, fireDebuff.Lifetime);

            Assert.AreEqual(9, game.State.MobInstances[m1].Ap);
            // No damage has been done yet by the AOE
            Assert.AreEqual(9, game.State.MobInstances[m2].Hp);

            game.NextMobOrNewTurn();

            Assert.AreEqual(1, game.TurnManager.TurnNumber);
            Assert.AreEqual(m2, game.TurnManager.CurrentMob);

            // Not the fire debuff should be applied
            game.NextMobOrNewTurn();

            Assert.AreEqual(2, game.TurnManager.TurnNumber);
            Assert.AreEqual(m1, game.TurnManager.CurrentMob);

            var targetAfter = game.State.MobInstances[m2];
            Assert.AreEqual(8, targetAfter.Hp);
            Assert.AreEqual(10, targetAfter.Ap);
            Assert.AreEqual(1, targetAfter.Buff.Lifetime);
            Assert.AreEqual(10, game.State.MobInstances[m1].Ap);

            game.NextMobOrNewTurn();
            game.NextMobOrNewTurn();

            Assert.AreEqual(3, game.TurnManager.TurnNumber);
            Assert.AreEqual(m1, game.TurnManager.CurrentMob);

            Assert.AreEqual(7, game.State.MobInstances[m2].Hp);

            var buffAfter = game.State.MobInstances[m2].Buff;
            Assert.IsTrue(buffAfter.IsZero);
        }
    }
}