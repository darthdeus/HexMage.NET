using System;
using System.Collections.Generic;

namespace HexMage.Simulator {
    public class UctNode {
        private static int _id = 0;
        public int Id;


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
            Id = _id++;
            Q = q;
            N = n;
            Action = action;
            State = state;
        }

        public void PrecomputePossibleActions(bool allowMove = true, bool allowEndTurn = true) {
            if (PossibleActions == null) {
                PossibleActions = UctAlgorithm.PossibleActions(State, allowMove, allowEndTurn);
            }
        }

        public override string ToString() {
            return $"[{Id}] {Q}/{N}, {nameof(Action)}: {Action}";
        }

        public void Print(int indentation) {
            Console.WriteLine(new string('\t', indentation) + this);
            foreach (var child in Children) {
                child.Print(indentation + 1);
            }
        }
    }
}