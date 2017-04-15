using HexMage.Simulator;
using HexMage.Simulator.AI;

namespace HexMage.Benchmarks {
    public class GenerationMember {
        public DNA dna;
        public PlayoutResult result;
        public int failCount = 0 ;

        public GenerationMember() {
        }

        public GenerationMember(DNA dna, PlayoutResult result) {
            this.dna = dna;
            this.result = result;
        }

        public override string ToString() {
            return result.ToFitnessString(dna);
        }
    }
}