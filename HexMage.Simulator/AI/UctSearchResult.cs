using System.Collections.Generic;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Represents the result of MCTS.
    /// </summary>
    public class UctSearchResult {
        public List<UctAction> Actions { get; }
        public double MillisecondsPerIteration { get; }

        public UctSearchResult(List<UctAction> actions, double millisecondsPerIteration) {
            Actions = actions;
            MillisecondsPerIteration = millisecondsPerIteration;
        }
    }
}