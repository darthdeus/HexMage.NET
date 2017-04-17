using System;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Single;

namespace HexMage.Simulator.AI {
    public class Individual {
        public DNA Team1;
        public DNA Team2;
        public PlayoutResult Result;

        public Individual(DNA team1, DNA team2, PlayoutResult result) {
            Team1 = team1;
            Team2 = team2;
            Result = result;
        }

        public float CombinedFitness() {
            float fitA = 1 - Result.HpPercentage;
            float fitB = (float) PlayoutResult.LengthSample(Result.TotalTurns);
            float fitC = Team1.DistanceFitness(Team2);
            float fitness = (fitA + fitB + fitC) / 3;

            if (!Result.AllPlayed) {
                fitness = 0.0001f;
            }

            return fitness;
        }
    }
}