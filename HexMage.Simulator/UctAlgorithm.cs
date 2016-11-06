using System;
using System.Collections.Generic;

namespace HexMage.Simulator {
    public class UctAction {}

    public class UctNode {
        public float Q { get; set; }
        public int N { get; set; }
        public UctAction Action { get; set; }
        public GameInstance State { get; set; }
        public UctNode Parent { get; set; }
        public List<UctNode> Children { get; set; } = new List<UctNode>();
        public List<UctAction> PossibleActions = null;

        public bool IsTerminal => State.State.IsFinished;
        public bool IsFullyExpanded => PossibleActions != null && PossibleActions.Count == Children.Count;

        public UctNode(float q, int n, UctAction action, GameInstance state) {
            Q = q;
            N = n;
            Action = action;
            State = state;
        }

        public void ComputePossibleActions() {
            PossibleActions = new List<UctAction>();
        }
    }

    public enum PossibleActions {
        MoveForward,
        MoveBack,
        Attack
    }

    public class UctAlgorithm {
        public UctNode _root;

        public UctNode UctSearch(GameInstance initialState) {
            _root = new UctNode(0, 0, null, initialState);

            int iterations = 1000;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(_root);
                float delta = DefaultPolicy(v.State);
                Backup(v, delta);
            }

            return BestChild(_root);
        }

        public UctNode TreePolicy(UctNode node) {
            while (!node.IsTerminal) {
                if (!node.IsFullyExpanded) {
                    return Expand(node);
                } else {
                    node = BestChild(node);
                }
            }

            return node;
        }

        public UctNode BestChild(UctNode node) {
            if (node.Children.Count == 0) return null;

            UctNode best = node.Children[0];
            foreach (var child in node.Children) {
                if (UcbValue(node, child) > UcbValue(node, best)) {
                    best = node;
                }
            }

            return best;
        }

        public float UcbValue(UctNode parent, UctNode node) {
            return (float) (node.Q/node.N + Math.Sqrt(2*Math.Log(parent.N)/node.N));
        }

        public UctNode Expand(UctNode node) {
            if (node.PossibleActions == null) {
                node.ComputePossibleActions();
            }

            var action = node.PossibleActions[node.Children.Count];
            var child = new UctNode(0, 1, action, F(node.State, action));

            node.Children.Add(child);

            return child;
        }

        private GameInstance F(GameInstance state, UctAction action) {
            throw new NotImplementedException();
        }

        public float DefaultPolicy(GameInstance game) {
            throw new NotImplementedException();
        }

        public void Backup(UctNode node, float delta) {
            while (node != null) {
                node.N++;
                node.Q += delta;
                node = node.Parent;
            }
        }
    }
}