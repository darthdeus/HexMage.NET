using HexMage.Simulator.Pathfinding;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HexMage.Simulator.Tests {
    public static class TestHelpers {
        public static void GameInstancesEqual(GameInstance instance1, GameInstance instance2) {
            Assert.AreEqual(instance1.Size, instance2.Size);
            MapsEqual(instance1.Map, instance2.Map);
            MobManagersEqual(instance1.MobManager, instance2.MobManager);
            GameStatesEqual(instance1.State, instance2.State);
        }

        public static void GameStatesEqual(GameState state1, GameState state2) {
            CollectionAssert.AreEqual(state1.MobInstances, state2.MobInstances);
            CollectionAssert.AreEqual(state1.Cooldowns, state2.Cooldowns);
            CollectionAssert.AreEqual(state1.AreaBuffs, state2.AreaBuffs);
            Assert.AreEqual(state1.CurrentMobIndex, state2.CurrentMobIndex);

            // TODO - tohle funguje?
            //CollectionAssert.AreEqual(state1.MobPositions, state2.MobPositions);

            Assert.AreEqual(state1.RedAlive, state2.RedAlive);
            Assert.AreEqual(state1.BlueAlive, state2.BlueAlive);

            Assert.AreEqual(state1.IsFinished, state2.IsFinished);
        }
        
        public static void MapsEqual(Map map1, Map map2) {
            Assert.AreEqual(map1.Size, map2.Size);
        }

        public static void MobManagersEqual(MobManager mobManager1, MobManager mobManager2) {
            CollectionAssert.AreEqual(mobManager1.Abilities, mobManager2.Abilities);
            CollectionAssert.AreEqual(mobManager1.Mobs, mobManager2.Mobs);
            CollectionAssert.AreEqual(mobManager1.MobInfos, mobManager2.MobInfos);
        }
    }
}