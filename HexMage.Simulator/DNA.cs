using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using HexMage.Simulator.PCG;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace HexMage.Simulator {
    public class DNA {
        public Vector<float> Data;

        //public List<float> Data = new List<float>();
        public int AbilityCount;

        public int MobCount;

        public const int MobAttributeCount = 2;
        public const int AbilityAttributeCount = 11;
        public int MobSize => MobAttributeCount + AbilityCount * AbilityAttributeCount;

        // TODO - area buff

        public DNA() { }

        public DNA(DNA dna) {
            MobCount = dna.MobCount;
            AbilityCount = dna.AbilityCount;
            Data = (DenseVector) dna.Data.Clone();
        }

        public DNA(int mobCount, int abilityCount) {
            MobCount = mobCount;
            AbilityCount = abilityCount;
            Data = new DenseVector(mobCount * MobSize);
            for (int i = 0; i < Data.Count; i++) {
                Data[i] = .5f;
            }
        }

        public DNA(int mobCount, int abilityCount, List<float> data) {
            AbilityCount = abilityCount;
            MobCount = mobCount;
            Data = DenseVector.OfEnumerable(data);
        }

        public DNA Clone() {
            return new DNA(this);
        }

        public void Randomize() {
            do {
                for (int i = 0; i < Data.Count; i++) {
                    Data[i] = (float) Generator.Random.NextDouble();
                }
            } while (!ToTeam().IsValid());
        }

        public string ToDnaString() {
            var dnaString = new StringBuilder();
            foreach (var num in Data) {
                dnaString.Append(num.ToString(".00"));
                dnaString.Append(" ");
            }

            return dnaString.ToString();
        }

        public string ToSerializableString() {
            return $"{MobCount},{AbilityCount},{string.Join(",", Data.Select(n => n.ToString("0.00")))}";
        }

        public static DNA FromSerializableString(string str) {
            Console.WriteLine($"Processing****\n\n'{str}'\n\n*****");
            var split = str.Split(',');
            var dna = new DNA();
            dna.MobCount = int.Parse(split[0]);
            dna.AbilityCount = int.Parse(split[1]);

            dna.Data = DenseVector.OfEnumerable(split.Skip(2).Select(x => {
                Console.WriteLine($"Parsing {x}");
                return float.Parse(x, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            }));

            return dna;
        }

        public override string ToString() {
            return ToDnaString();
        }

        public Team ToTeam() {
            return GenomeLoader.FromDna(this);
        }

        public float DistanceFitness(DNA team2) {
            var low = DenseVector.Build.Dense(Data.Count, 0);
            var high = DenseVector.Build.Dense(Data.Count, 1);

            double maxDistance = Distance.Euclidean(low, high);

            double distance = Distance.Euclidean(Data, team2.Data);
            double relativeDistance = distance / maxDistance;

            double distanceFitness = 1 / (1 + Math.Exp(-60 * relativeDistance + 1.5));

            return (float) distanceFitness;
        }
    }
}