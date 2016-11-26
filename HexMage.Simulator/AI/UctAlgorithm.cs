using System;
using System.Collections.Generic;

namespace HexMage.Simulator {
    public class UctAction {}

    public class EndTurnAction : UctAction {
        public static EndTurnAction Instance = new EndTurnAction();
    }

    public class NullAction : UctAction {
        public static NullAction Instance = new NullAction();
    }

    public class AbilityUseAction : UctAction {
        public readonly int MobId;
        public readonly int TargetId;
        public readonly int AbilityId;

        public AbilityUseAction(int mobId, int targetId, int abilityId) {
            MobId = mobId;
            TargetId = targetId;
            AbilityId = abilityId;
        }
    }

    public class MoveAction : UctAction {
        public readonly int MobId;
        public readonly AxialCoord Coord;

        public MoveAction(int mobId, AxialCoord coord) {
            MobId = mobId;
            Coord = coord;
        }
    }

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
            PossibleActions = new List<UctAction> {
                EndTurnAction.Instance
            };

            var currentMob = State.TurnManager.CurrentMob;
            if (currentMob.HasValue) {
                var mobId = currentMob.Value;

                var mobInstance = State.State.MobInstances[mobId];
                var mobInfo = State.MobManager.MobInfos[mobId];

                foreach (var coord in State.Map.AllCoords) {
                    if (coord == mobInstance.Coord) continue;

                    if (State.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
                        PossibleActions.Add(new MoveAction(mobId, coord));
                    }
                }

                foreach (var abilityId in mobInfo.Abilities) {
                    var abilityInfo = State.MobManager.Abilities[abilityId];

                    if (abilityInfo.Cost <= mobInstance.Ap) {
                        foreach (var targetId in State.MobManager.Mobs) {
                            var targetInfo = State.MobManager.MobInfos[targetId];
                            var targetInstance = State.State.MobInstances[targetId];
                            int enemyDistance = State.Pathfinder.Distance(mobInstance.Coord, targetInstance.Coord);

                            if (targetInfo.Team != mobInfo.Team && enemyDistance <= abilityInfo.Range) {
                                PossibleActions.Add(new AbilityUseAction(mobId, targetId, abilityId));
                            }
                        }
                    }
                }
            } else {
                throw new InvalidOperationException();
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
            }
        }
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
            return (float) (node.Q / node.N + Math.Sqrt(2 * Math.Log(parent.N) / node.N));
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

        public static GameInstance F(GameInstance state, UctAction action) {
            var copy = state.DeepCopy();
            // TODO - null action
            // TODO - end turn action
            
            if (action is AbilityUseAction use) {
                copy.FastUse(use.AbilityId, use.MobId, use.TargetId);
            } else if (action is MoveAction move) {
                copy.FastMove(move.MobId, move.Coord);
            } else if (action is EndTurnAction _) {
                copy.TurnManager.NextMobOrNewTurn(copy.Pathfinder, copy.State);
            } else if (action is NullAction) {
                // do nothing
            }

            return copy;
        }

        public float DefaultPolicy(GameInstance game) {
            var rnd = new Random();

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