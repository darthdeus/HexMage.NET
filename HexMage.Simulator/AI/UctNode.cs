using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Represents a single node in the MCTS tree.
    /// </summary>
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

        public UctNode(UctAction action, GameInstance state) : this(0, 0, action, state) { }

        public UctNode(float q, int n, UctAction action, GameInstance state) {
            Id = _id++;
            Q = q;
            N = n;
            Action = action;
            State = state;
        }

        public static UctNode FromAction(GameInstance game, UctAction action) {
            return new UctNode(action, ActionEvaluator.F(game, action));
        }

        public void PrecomputePossibleActions(bool allowMove, bool allowEndTurn) {
            Debug.Assert(!State.IsFinished, "!State.IsFinished");

            if (PossibleActions == null) {
                if (Action.Type == UctActionType.DefensiveMove) {
                    PossibleActions = new List<UctAction> {UctAction.EndTurnAction()};
                } else {
                    PossibleActions = ActionGenerator.PossibleActions(State, this, allowMove, allowEndTurn);
                }
            }
        }

        public override string ToString() {
            string team;
            if (State.CurrentTeam.HasValue && State.State.LastTeamColor.HasValue) {
                var currentTeam = State.CurrentTeam.Value;
                var lastTeam = State.State.LastTeamColor.Value;

                team = Action.Type == UctActionType.EndTurn
                           ? $"{ShortTeam(lastTeam)}->{ShortTeam(currentTeam)}"
                           : ShortTeam(currentTeam);
            } else if (State.CurrentTeam.HasValue && !State.State.LastTeamColor.HasValue) {
                team = ShortTeam(State.CurrentTeam.Value);
            } else {
                team = "NOTEAM";
            }

            string terminal = IsTerminal ? $"[T]" : "";

            return $"{Id}\n[{team}]{terminal}\\n{Q}/{N}\\n{Action}";
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

        public void Print(int indentation) {
            Console.WriteLine(new string('\t', indentation) + this);
            foreach (var child in Children) {
                child.Print(indentation + 1);
            }
        }
    }
}