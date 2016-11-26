using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class UctAction {}

    public class EndTurnAction : UctAction {
        public static EndTurnAction Instance = new EndTurnAction();

        public override string ToString() {
            return $"EndTurnAction";
        }
    }

    public class NullAction : UctAction {
        public static NullAction Instance = new NullAction();

        public override string ToString() {
            return $"NullAction";
        }
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

        public override string ToString() {
            return $"ABILITY[{AbilityId}]: {MobId} -> {TargetId}";
        }
    }

    public class MoveAction : UctAction {
        public readonly int MobId;
        public readonly AxialCoord Coord;

        public MoveAction(int mobId, AxialCoord coord) {
            MobId = mobId;
            Coord = coord;
        }

        public override string ToString() {
            return $"MOVE[{MobId}] -> {Coord}";
        }
    }

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

        public void PrecomputePossibleActions() {
            if (PossibleActions == null) {
                PossibleActions = UctAlgorithm.PossibleActions(State);
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

    public class UctAlgorithm {
        public UctNode UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 1, null, initialState);

            int iterations = 1000;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(root);
                if (v.State.IsFinished) {
                    Backup(v, 1);
                } else {
                    float delta = DefaultPolicy(v.State);
                    Backup(v, delta);
                }
            }

            return BestChild(root);
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
                    best = child;
                }
            }

            return best;
        }

        public float UcbValue(UctNode parent, UctNode node) {
            return (float) (node.Q / node.N + Math.Sqrt(2 * Math.Log(parent.N) / node.N));
        }

        public UctNode Expand(UctNode node) {
            node.PrecomputePossibleActions();

            var action = node.PossibleActions[node.Children.Count];
            var child = new UctNode(0, 1, action, F(node.State, action));
            child.Parent = node;

            node.Children.Add(child);

            return child;
        }

        public static GameInstance F(GameInstance state, UctAction action) {
            var copy = state.DeepCopy();

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
            if (game.IsFinished) {
                throw new InvalidOperationException("The game is already finished.");
            }

            var rnd = new Random();

            Debug.Assert(game.CurrentTeam.HasValue, "game.CurrentTeam.HasValue");
            TeamColor startingTeam = game.CurrentTeam.Value;

            var copy = game.DeepCopy();
            int iterations = 0;

            while (!copy.IsFinished && iterations++ < 10000) {
                var actions = PossibleActions(copy);
                int actionIndex = rnd.Next(0, actions.Count);
                UctAction action = actions[actionIndex];

                copy = F(copy, action);
            }

            TeamColor? victoryTeam = copy.VictoryTeam;

            if (victoryTeam == null) {
                // The game was a draw
                return 0;
            } else if (startingTeam == victoryTeam.Value) {
                return 1;
            } else if (startingTeam != victoryTeam.Value) {
                return -1;
            } else {
                throw new InvalidOperationException("Invalid victory team result when running DefaultPolicy.");
            }
        }

        public void Backup(UctNode node, float delta) {
            while (node != null) {
                node.N++;
                node.Q += delta;
                node = node.Parent;
            }
        }

        public static List<UctAction> PossibleActions(GameInstance state) {
            var result = new List<UctAction> {
                EndTurnAction.Instance
            };

            var currentMob = state.TurnManager.CurrentMob;
            if (currentMob.HasValue) {
                var mobId = currentMob.Value;

                var mobInstance = state.State.MobInstances[mobId];
                var mobInfo = state.MobManager.MobInfos[mobId];

                foreach (var coord in state.Map.AllCoords) {
                    if (coord == mobInstance.Coord) continue;

                    if (state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
                        if (state.State.AtCoord(coord) == null) {
                            result.Add(new MoveAction(mobId, coord));
                        }
                    }
                }

                foreach (var abilityId in mobInfo.Abilities) {
                    var abilityInfo = state.MobManager.Abilities[abilityId];

                    // Skip abilities which are on cooldown
                    if (state.State.Cooldowns[abilityId] > 0) continue;

                    if (abilityInfo.Cost <= mobInstance.Ap) {
                        foreach (var targetId in state.MobManager.Mobs) {
                            var targetInfo = state.MobManager.MobInfos[targetId];
                            var targetInstance = state.State.MobInstances[targetId];
                            int enemyDistance = state.Pathfinder.Distance(mobInstance.Coord, targetInstance.Coord);

                            bool isEnemy = targetInfo.Team != mobInfo.Team;
                            bool withinRange = enemyDistance <= abilityInfo.Range;
                            bool targetAlive = targetInstance.Hp > 0;

                            if (isEnemy && withinRange && targetAlive) {
                                result.Add(new AbilityUseAction(mobId, targetId, abilityId));
                            }
                        }
                    }
                }
            } else {
                throw new InvalidOperationException();
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
            }

            return result;
        }
    }
}