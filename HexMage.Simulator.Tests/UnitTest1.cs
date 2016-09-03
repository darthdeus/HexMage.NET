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
            var m = new HexMap<int>(5);

            m[3, 4] = 6;
            Assert.AreEqual(6, m[new AxialCoord(3, 4)]);
        }
    }

    [TestClass]
    public class PathfinderTest
    {
        [TestMethod]
        public void TestAll() {
            int size = 10;
            var game = new GameInstance(size);

            var mobManager = game.MobManager;

            var pc1 = new AiRandomController();

            var t1 = mobManager.AddTeam(TeamColor.Red, pc1);
            var t2 = mobManager.AddTeam(TeamColor.Blue, pc1);

            var m1 = Generator.RandomMob(t1, size, _ => true);
            var m2 = Generator.RandomMob(t2, size, c => !m1.Coord.Equals(c));

            Assert.AreNotEqual(m1.Coord, m2.Coord);

            mobManager.AddMob(m1);
            mobManager.AddMob(m2);

            var pathfinder = new Pathfinder(game.Map, game.MobManager);
            pathfinder.PathfindFrom(m1.Coord);

            Assert.AreNotEqual(0, pathfinder.Distance(m2.Coord));

            var path = pathfinder.PathTo(m2.Coord);

            Assert.IsTrue(path.Count >= 1);
        }
    }
}
