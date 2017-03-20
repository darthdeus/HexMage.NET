using System.Collections.Generic;

namespace HexMage.Simulator.AI {
    public class UctSearchResult {
        public List<UctAction> Actions { get; }
        public double MillisecondsPerIteration { get; }

        public UctSearchResult(List<UctAction> actions, double millisecondsPerIteration) {
            Actions = actions;
            MillisecondsPerIteration = millisecondsPerIteration;
        }
    }
}