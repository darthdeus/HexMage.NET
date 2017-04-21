using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Benchmarks;
using HexMage.Simulator.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class DnaTest {
        [TestMethod]
        public void UniformPickTest() {
            var items = new List<int> {0, 1, 2, 3, 4};
            var probabilities = new [] { .05, .15, .3, .25, .25 };
            var counts = new double[] {0, 0, 0, 0, 0};

            const int max = 10000;
            for (int i = 0; i < max; i++) {
                var item = Probability.UniformPick(items, probabilities);
                counts[item]++;
            }

            Assert.AreEqual(counts[0]/max, probabilities[0], .02);
            Assert.AreEqual(counts[1]/max, probabilities[1], .02);
            Assert.AreEqual(counts[2]/max, probabilities[2], .02);
            Assert.AreEqual(counts[3]/max, probabilities[3], .02);
            Assert.AreEqual(counts[4]/max, probabilities[4], .02);
        }

        [TestMethod]
        public void DnaSerializationTest() {
            for (int i = 0; i < 50; i++) {
                var dna = new DNA(1, 1);
                dna.Randomize();
                //dna.Data = new List<float> {0.17f, 0.74f, .26f, .51f, .23f, .25f, 0, 0, 0, 0, 0, 0, 0};

                var team = dna.ToTeam();
                var converted = team.ToDna();

                for (int j = 0; j < dna.Data.Count; j++) {
                    float a = dna.Data[j];
                    float b = converted.Data[j];
                    double delta = 0.10;
                    if (Math.Abs(a - b) > delta) {
                        Debugger.Break();
                    }
                    Assert.AreEqual(a, b, delta, $"Expected {a} and {b} to be within {delta} at index {j}");
                }
            }
            //var dna = new DNA(2, 2, new List<float> {
            //    30, 20,  5, 4, 3, 0,   9, 1, 8, 3,
            //    31, 21,  51, 41, 31, 1,   91, 11, 81, 2
            //});

            //var team = GenomeLoader.FromDna(dna);

            //Assert.AreEqual(30 * Constants.HpMax, team.mobs[0].hp);
            //Assert.AreEqual(20 * Constants.ApMax, team.mobs[0].ap);

            //Assert.AreEqual(5 * Constants.DmgMax, team.mobs[0].abilities[0].dmg);
            //Assert.AreEqual(4 * Constants.CostMax, team.mobs[0].abilities[0].ap);
            //Assert.AreEqual(3 * Constants.RangeMax, team.mobs[0].abilities[0].range);
            //Assert.AreEqual(AbilityElement.Fire, team.mobs[0].abilities[0].element);

            //Assert.AreEqual(9 * Constants.DmgMax, team.mobs[0].abilities[1].dmg);
            //Assert.AreEqual(1 * Constants.CostMax, team.mobs[0].abilities[1].ap);
            //Assert.AreEqual(8 * Constants.RangeMax, team.mobs[0].abilities[1].range);
            //Assert.AreEqual(AbilityElement.Water, team.mobs[0].abilities[1].element);

            //Assert.AreEqual(31 * Constants.HpMax, team.mobs[1].hp);
            //Assert.AreEqual(21 * Constants.ApMax, team.mobs[1].ap);

            //Assert.AreEqual(51 * Constants.DmgMax, team.mobs[1].abilities[0].dmg);
            //Assert.AreEqual(41 * Constants.CostMax, team.mobs[1].abilities[0].ap);
            //Assert.AreEqual(31 * Constants.RangeMax, team.mobs[1].abilities[0].range);
            //Assert.AreEqual(AbilityElement.Earth, team.mobs[1].abilities[0].element);

            //Assert.AreEqual(91 * Constants.DmgMax, team.mobs[1].abilities[1].dmg);
            //Assert.AreEqual(11 * Constants.CostMax, team.mobs[1].abilities[1].ap);
            //Assert.AreEqual(81 * Constants.RangeMax, team.mobs[1].abilities[1].range);
            //Assert.AreEqual(AbilityElement.Air, team.mobs[1].abilities[1].element);

            //var dna2 = GenomeLoader.FromTeam(team);

            //CollectionAssert.AreEqual(dna2.Data, dna.Data);
        }
    }
}