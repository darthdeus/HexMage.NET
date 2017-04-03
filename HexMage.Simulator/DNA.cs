using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    public class DNA {
        public List<float> Data = new List<float>();
        public int AbilityCount;
        public int MobCount;

        public const int MobAttributeCount = 2;
        public const int AbilityAttributeCount = 3;
        public int MobSize => MobAttributeCount + AbilityCount * AbilityAttributeCount;

        // TODO - element
        // TODO - buff
        // TODO - area buff

        public DNA() {}

        public DNA(int mobCount, int abilityCount) {
            MobCount = mobCount;
            AbilityCount = abilityCount;
            Data = new List<float>(mobCount * MobSize);
            for (int i = 0; i < Data.Capacity; i++) {
                Data.Add(0.5f);
            }
        }

        public DNA(int mobCount, int abilityCount, List<float> data) {
            AbilityCount = abilityCount;
            MobCount = mobCount;
            Data = data;
        }

        public DNA Clone() {
            var dna = new DNA(MobCount, AbilityCount, new List<float>(Data.Count));

            for (int i = 0; i < Data.Count; i++) {
                dna.Data.Add(Data[i]);
            }

            return dna;
        }

        public void Randomize() {
            do {
                for (int j = 0; j < Data.Count; j++) {
                    Data[j] = (float) Generator.Random.NextDouble();
                }
            } while (!ToTeam().IsValid());
        }

        public string ToDNAString() {
            var dnaString = new StringBuilder();
            foreach (var num in Data) {
                dnaString.Append(num.ToString(".00"));
                dnaString.Append(" ");
            }

            return dnaString.ToString();
        }

        public string ToSerializableString() {
            return $"{MobCount},{AbilityCount},{string.Join(",", Data)}";
        }

        public static DNA FromSerializableString(string str) {
            var split = str.Split(',');
            var dna = new DNA();
            dna.MobCount = int.Parse(split[0]);
            dna.AbilityCount = int.Parse(split[1]);

            dna.Data = split.Skip(2).Select(float.Parse).ToList();

            return dna;
        }

        public override string ToString() {
            return ToDNAString();
        }

        public Team ToTeam() {
            return GenomeLoader.FromDna(this);
        }
    }
}