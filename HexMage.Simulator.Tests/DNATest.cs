using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Benchmarks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests
{
    [TestClass]
    public class DnaTest
    {
        [TestMethod]
        public void DnaSerializationTest() {
            var dna = new DNA(2, 2, new List<float> {
                30, 20, 5, 4, 3, 9, 1, 8,
                31, 21, 51, 41, 31, 91, 11, 81
            });

            var team = GenomeLoader.FromDna(dna);

            Assert.AreEqual(30 * Constants.HpMax, team.mobs[0].hp);
            Assert.AreEqual(20 * Constants.ApMax, team.mobs[0].ap);

            Assert.AreEqual(5 * Constants.DmgMax, team.mobs[0].abilities[0].dmg);
            Assert.AreEqual(4 * Constants.CostMax, team.mobs[0].abilities[0].ap);
            Assert.AreEqual(3 * Constants.RangeMax, team.mobs[0].abilities[0].range);

            Assert.AreEqual(9 * Constants.DmgMax, team.mobs[0].abilities[1].dmg);
            Assert.AreEqual(1 * Constants.CostMax, team.mobs[0].abilities[1].ap);
            Assert.AreEqual(8 * Constants.RangeMax, team.mobs[0].abilities[1].range);

            Assert.AreEqual(31 * Constants.HpMax, team.mobs[1].hp);
            Assert.AreEqual(21 * Constants.ApMax, team.mobs[1].ap);

            Assert.AreEqual(51 * Constants.DmgMax, team.mobs[1].abilities[0].dmg);
            Assert.AreEqual(41 * Constants.CostMax, team.mobs[1].abilities[0].ap);
            Assert.AreEqual(31 * Constants.RangeMax, team.mobs[1].abilities[0].range);

            Assert.AreEqual(91 * Constants.DmgMax, team.mobs[1].abilities[1].dmg);
            Assert.AreEqual(11 * Constants.CostMax, team.mobs[1].abilities[1].ap);
            Assert.AreEqual(81 * Constants.RangeMax, team.mobs[1].abilities[1].range);

            var dna2 = GenomeLoader.FromTeam(team);

            CollectionAssert.AreEqual(dna2.Data, dna.Data);
        }
    }
}
