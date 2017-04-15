using System.Diagnostics;

namespace HexMage.Simulator.AI {
    public class FlatMonteCarlo {
        public static UctNode Search(GameInstance initial) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initial.CopyStateOnly());

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!root.IsFullyExpanded) {
                UctAlgorithm.Expand(root);
            }

            foreach (var child in root.Children) {
                float reward = UctAlgorithm.DefaultPolicy(child.State, initial.CurrentTeam.Value);
                child.Q = reward;
                child.N++;
            }

            //for (int i = 0; i < 10000; i++) {
            //    var child = root.Children[i % root.Children.Count];

            //    float reward = UctAlgorithm.DefaultPolicy(child.State, initial.CurrentTeam.Value);
            //    UctAlgorithm.Backup(child, reward);
            //}

            var bestChild = UctAlgorithm.BestChild(root, initial.CurrentTeam.Value, 0);

            UctDebug.PrintDotgraph(root, () => 0);

            return bestChild;
        }
    }
}