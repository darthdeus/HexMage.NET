using System;
using System.Linq;
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
    public class GameInstanceTest {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestDuplicateColorFails() {
            var game = new GameInstance(10);

            var ctrl = new AiRandomController(game);
            game.MobManager.AddTeam(TeamColor.Red, ctrl);
            game.MobManager.AddTeam(TeamColor.Red, ctrl);
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

            var pc1 = new AiRandomController(game);

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

    [TestClass]
    public class DeepCopyTest {
        [TestMethod]
        public void TestTopLevelDeepCopy() {
            int size = 10;
            var game = new GameInstance(size);

            var ctrl = new AiRandomController(game);
            var t1 = game.MobManager.AddTeam(TeamColor.Red, ctrl);
            var t2 = game.MobManager.AddTeam(TeamColor.Blue, ctrl);

            var m1 = Generator.RandomMob(t1, size);
            var m2 = Generator.RandomMob(t2, size, c => !m1.Coord.Equals(c));

            game.MobManager.AddMob(m1);
            game.MobManager.AddMob(m2);

            var gameCopy = game.DeepCopy();

            var mobList = gameCopy.MobManager.Mobs.ToList();
            CollectionAssert.DoesNotContain(mobList, m1);
            CollectionAssert.DoesNotContain(mobList, m2);

            m1.Hp = 0;
            m2.Hp = 0;

            Assert.IsTrue(gameCopy.MobManager.Mobs.All(m => m.Hp != 0));
        }
    }
}
