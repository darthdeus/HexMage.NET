using HexMage.Simulator;
using HexMage.Simulator.AI;

namespace HexMage.Benchmarks {
    public class GenerationMember {
        public DNA dna;
        public EvaluationResult result;

        public override string ToString() {
            return result.ToFitnessString(dna);
        }
    }
}