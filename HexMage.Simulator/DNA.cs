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
    /// <summary>
    /// Represents a single team in the encounter.
    /// </summary>
    public class DNA {
        public Vector<float> Data;

        public int AbilityCount;

        public int MobCount;

        public const int MobAttributeCount = 2;
        public const int AbilityAttributeCount = 11;
        public int MobSize => MobAttributeCount + AbilityCount * AbilityAttributeCount;

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

        /// <summary>
        /// Randomizes all of the attributes. Note that this can take a few iterations
        /// as the values are validated after generating.
        /// </summary>
        public void Randomize() {
            do {
                for (int i = 0; i < Data.Count; i++) {
                    Data[i] = (float) Generator.Random.NextDouble();
                }
            } while (!ToTeam().IsValid());
        }

        /// <summary>
        /// Converts to a printable string format, used mainly for debugging.
        /// </summary>
        public string ToDnaString() {
            var dnaString = new StringBuilder();
            foreach (var num in Data) {
                dnaString.Append(num.ToString(".00"));
                dnaString.Append(" ");
            }

            return dnaString.ToString();
        }

        /// <summary>
        /// Converts to a serializable DNA format as described in the programmer documentation.
        /// </summary>
        public string ToSerializableString() {
            return $"{MobCount},{AbilityCount},{string.Join(",", Data.Select(n => n.ToString("0.00")))}";
        }

        /// <summary>
        /// Deserializes the string format created by <code>ToSerializableString</code>
        /// </summary>
        public static DNA FromSerializableString(string str) {
            var split = str.Split(',');
            var dna = new DNA();
            dna.MobCount = int.Parse(split[0]);
            dna.AbilityCount = int.Parse(split[1]);

            dna.Data = DenseVector.OfEnumerable(split.Skip(2).Select(x => {
                return float.Parse(x, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }));

            return dna;
        }

        public override string ToString() {
            return ToDnaString();
        }

        public Team ToTeam() {
            return GenomeLoader.FromDna(this);
        }

        /// <summary>
        /// Calculates a distance fitness value between two DNAs.
        /// </summary>
        public float DistanceFitness(DNA team2) {
            var low = DenseVector.Build.Dense(Data.Count, 0);
            var high = DenseVector.Build.Dense(Data.Count, 1);

            double maxDistance = Distance.Euclidean(low, high);

            double distance = Distance.Euclidean(Data, team2.Data);
            double relativeDistance = distance / maxDistance;

            var distanceFitness = RelativeDistance(relativeDistance);

            return (float) distanceFitness;
        }

        public static double RelativeDistance(double relativeDistance) {
            return 1 / (1 + Math.Exp(-60 * relativeDistance + 1.5));
        }
    }
}