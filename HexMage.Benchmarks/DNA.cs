using System.Collections.Generic;

namespace HexMage.Benchmarks {
    public class DNA {
        public List<float> Data = new List<float>();
        public int AbilityCount;
        public int MobCount;

        public const int MobAttributeCount = 2;
        public const int AbilityAttributeCount = 3;
        public int MobSize => MobAttributeCount + AbilityCount * AbilityAttributeCount;

        public DNA() {}

        public DNA(int mobCount, int abilityCount, List<float> data) {
            AbilityCount = abilityCount;
            MobCount = mobCount;
            Data = data;
        }
    }
}