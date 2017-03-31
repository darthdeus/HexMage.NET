using System.Collections.Generic;
using System.Text;

namespace HexMage.Simulator {
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

        public DNA Copy() {
            var dna = new DNA(MobCount, AbilityCount, new List<float>(Data.Count));

            for (int i = 0; i < Data.Count; i++) {
                dna.Data.Add(Data[i]);
            }

            return dna;
        }

        public string ToDNAString() {
            var dnaString = new StringBuilder();
            foreach (var num in Data) {
                dnaString.Append(num.ToString(".00"));
                dnaString.Append(",");
            }

            return dnaString.ToString();
        }
    }
}