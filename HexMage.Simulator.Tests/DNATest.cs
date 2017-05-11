using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                var team = dna.ToTeam();
                var converted = team.ToDna();

                for (int j = 0; j < dna.Data.Count; j++) {
                    float a = dna.Data[j];
                    float b = converted.Data[j];
                    const double delta = 0.25;
                    if (Math.Abs(a - b) > delta) {
                        Debugger.Break();
                    }
                    Assert.AreEqual(a, b, delta, $"Expected {a} and {b} to be within {delta} at index {j}");
                }
            }
        }
    }
}