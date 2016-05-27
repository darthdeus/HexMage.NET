using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests
{
    [TestClass]
    public class MatrixTest
    {
        [TestMethod]
        public void TestSubscriptOperator()
        {
            var m = new Matrix<int>(5, 5);

            m[3, 4] = 6;
            Assert.AreEqual(6, m[new Coord(3, 4)]);
        }
    }

    [TestClass]
    public class PathfinderTest
    {
        [TestMethod]
        public void TestAll() {
            int size = 10;
            var game = new Game(size);

            var t1 = new Team();
            var t2 = new Team();
            game.MobManager.Teams.Add(t1);
            game.MobManager.Teams.Add(t2);

            var m1 = Generator.RandomMob(t1, size, _ => true);
            var m2 = Generator.RandomMob(t2, size, c => !m1.Coord.Equals(c));

            Assert.AreNotEqual(m1.Coord, m2.Coord);

            game.MobManager.Mobs.Add(m1);
            game.MobManager.Mobs.Add(m2);

            var pathfinder = new Pathfinder(size);
            pathfinder.PathfindFrom(m1.Coord, game.Map, game.MobManager);

            Assert.AreNotEqual(0, pathfinder.Distance(m2.Coord));

            var path = pathfinder.PathTo(m2.Coord);

            Assert.IsTrue(path.Count >= 1);
        }
    }
}
