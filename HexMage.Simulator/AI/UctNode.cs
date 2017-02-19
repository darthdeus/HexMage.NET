using System;
using System.Collections.Generic;
using HexMage.Simulator.Model;

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
            string team;
            if (State.CurrentTeam.HasValue) {
                var currentTeam = State.CurrentTeam.Value;
                team = Action.Type == UctActionType.EndTurn
                           ? $"{ShortTeam(OtherTeam(currentTeam))}->{ShortTeam(currentTeam)}"
                           : ShortTeam(currentTeam);
            } else {
                team = "";
            }

            string terminal = IsTerminal ? $"[T]" : "";
            var value = Math.Round(UcbValue(), 2);
            return $"[{team}]{terminal}{Q}/{N}/{value}\\n{Action}";
        }

        private string ShortTeam(TeamColor team) {
            switch (team) {
                case TeamColor.Red:
                    return "R";
                case TeamColor.Blue:
                    return "B";
                default:
                    throw new ArgumentException();
            }
        }

        private TeamColor OtherTeam(TeamColor color) {
            if (color == TeamColor.Red) {
                return TeamColor.Blue;
            } else {
                return TeamColor.Red;
            }
        }

        public float UcbValue() {
            float parentN = Parent?.N ?? 0;
            return (float) (Q / N + Math.Sqrt(2 * Math.Log(parentN) / N));
        }

        public void Print(int indentation) {
            Console.WriteLine(new string('\t', indentation) + this);
            foreach (var child in Children) {
                child.Print(indentation + 1);
            }
        }
    }
}