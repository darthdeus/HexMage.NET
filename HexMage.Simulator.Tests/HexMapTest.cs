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

    //[TestClass]
    //public class PathfinderTest
    //{
    //    [TestMethod]
    //    public void TestAll() {
    //        int size = 10;
    //        var game = new GameInstance(size);

    //        var mobManager = game.MobManager;

    //        var pc1 = new AiRandomController(game);

    //        var t1 = TeamColor.Red;
    //        var t2 = TeamColor.Blue;

    //        var m1 = Generator.RandomMob(mobManager, t1, size, _ => true);
    //        var m2 = Generator.RandomMob(mobManager, t2, size, c => !m1.Coord.Equals(c));

    //        Assert.AreNotEqual(m1.Coord, m2.Coord);

    //        mobManager.AddMob(m1);
    //        mobManager.AddMob(m2);

    //        var pathfinder = new Pathfinder(game.Map, game.MobManager);
    //        pathfinder.PathfindFrom(m1.Coord);

    //        Assert.AreNotEqual(0, pathfinder.Distance(m2.Coord));

    //        Assert.IsTrue(pathfinder.Distance(m2.Coord) >= 1);
    //    }
    //}

    //[TestClass]
    //public class DeepCopyTest {
    //    [TestMethod]
    //    public void TestTopLevelDeepCopy() {
    //        int size = 10;
    //        var game = new GameInstance(size);

    //        var ctrl = new AiRandomController(game);
    //        var t1 = TeamColor.Red;
    //        var t2 = TeamColor.Blue;

    //        var m1 = Generator.RandomMob(game.MobManager, t1, size);
    //        var m2 = Generator.RandomMob(game.MobManager, t2, size, c => !m1.Coord.Equals(c));

    //        game.MobManager.AddMob(m1);
    //        game.MobManager.AddMob(m2);

    //        var gameCopy = game.DeepCopy();

    //        var mobList = gameCopy.MobManager.Mobs.ToList();
    //        CollectionAssert.DoesNotContain(mobList, m1);
    //        CollectionAssert.DoesNotContain(mobList, m2);

    //        m1.Hp = 0;
    //        m2.Hp = 0;

    //        Assert.IsTrue(gameCopy.MobManager.Mobs.All(m => m.Hp != 0));
    //    }
    //}
}