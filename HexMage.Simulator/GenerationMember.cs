using System;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Single;

namespace HexMage.Benchmarks {
    public class GenerationMember {
        public DNA dna;
        public PlayoutResult result;
        public int failCount = 0;

        public GenerationMember() { }

        public GenerationMember(DNA dna, PlayoutResult result) {
            this.dna = dna;
            this.result = result;
        }

        public float CombinedFitness(DNA initialDna) {
            float fitA = 1 - result.HpPercentage;
            float fitB = (float) PlayoutResult.LengthSample(result.TotalTurns);

            float fitness = (fitA + fitB) / 2;

            if (!result.AllPlayed) {
                fitness = 0.0001f;
            }

            var low = DenseVector.Build.Dense(dna.Data.Count, 0);
            var high = DenseVector.Build.Dense(dna.Data.Count, 1);

            double maxDistance = Distance.Euclidean(low, high);


            double distance = Distance.Euclidean(dna.Data, initialDna.Data);
            double relativeDistance = distance / maxDistance;
            double distanceFitness = 1 / (1 + Math.Exp(-20 * relativeDistance + 4));

            return (fitness + (float) distanceFitness) / 2;
        }

        public override string ToString() {
            return result.ToFitnessString(dna, result.SimpleFitness());
        }
    }
}