using System.Diagnostics;

namespace HexMage.Simulator.AI {
    public class FlatMonteCarlo {
        public static UctNode Search(GameInstance initial) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initial.DeepCopy());

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!root.IsFullyExpanded) {
                UctAlgorithm.Expand(root);
            }

            for (int i = 0; i < 10000; i++) {
                var child = root.Children[i % root.Children.Count];

                float reward = UctAlgorithm.DefaultPolicy(child.State, initial.CurrentTeam.Value);
                UctAlgorithm.Backup(child, reward);
            }

            var bestChild = UctAlgorithm.BestChild(root);

            return root;
        }
    }
}