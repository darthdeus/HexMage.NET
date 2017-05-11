using System;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    [TestClass]
    public class HexMapTest {
        [TestMethod]
        public void PathFromSourceToTargetTest() {
            var game = new GameInstance(5);
            game.PrepareEverything();

            foreach (var from in game.Map.AllCoords) {
                foreach (var to in game.Map.AllCoords) {
                    if (from == to) continue;
                    var path1 = game.Pathfinder.PathFromSourceToTarget(from, to);
                    var path2 = game.Pathfinder.PathTo(from, to);
                    var origPath2 = path2.ToList();
                    path2.RemoveAt(0);
                    path2.Reverse();

                    if (path1.Count != path2.Count) {
                        Debugger.Break();
                    }

                    Assert.AreEqual(path1.Count, path2.Count, $"Expected same length {from} -> {to}");
                    CollectionAssert.AreEqual(path1.ToList(), path2);
                }
            }
        }

        [TestMethod]
        public void TestSubscriptOperator() {
            var m = new HexMap<int>(5);

            m[3, 4] = 6;
            Assert.AreEqual(6, m[new AxialCoord(3, 4)]);
        }
    }
}