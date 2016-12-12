using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public enum UctActionType {
        EndTurn,
        Null,
        AbilityUse,
        Move
    }

    public struct UctAction {
        public readonly UctActionType Type;
        public readonly int AbilityId;
        public readonly int MobId;
        public readonly int TargetId;
        public readonly AxialCoord Coord;

        private UctAction(UctActionType type, int abilityId, int mobId, int targetId, AxialCoord coord) {
            Type = type;
            AbilityId = abilityId;
            MobId = mobId;
            TargetId = targetId;
            Coord = coord;
        }

        public static UctAction NullAction() {
            return new UctAction(UctActionType.Null, -1, -1, -1, AxialCoord.Zero);
        }

        public static UctAction EndTurnAction() {
            return new UctAction(UctActionType.EndTurn, -1, -1, -1, AxialCoord.Zero);
        }

        public static UctAction AbilityUseAction(int abilityId, int mobId, int targetId) {
            return new UctAction(UctActionType.AbilityUse, abilityId, mobId, targetId, AxialCoord.Zero);
        }

        public static UctAction MoveAction(int mobId, AxialCoord coord) {
            return new UctAction(UctActionType.Move, -1, mobId, -1, coord);
        }

        public override string ToString() {
            switch (Type) {
                case UctActionType.Null:
                    return $"NullAction";
                case UctActionType.EndTurn:
                    return $"EndTurnAction";
                case UctActionType.AbilityUse:
                    return $"ABILITY[{AbilityId}]: {MobId} -> {TargetId}";
                case UctActionType.Move:
                    return $"MOVE[{MobId}] -> {Coord}";
                default:
                    throw new InvalidOperationException($"Invalid value of ${Type}");
            }
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
            var root = new UctNode(0, 1, UctAction.NullAction(), initialState);

            int iterations = 10;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(root);
                float delta = DefaultPolicy(v.State);
                Backup(v, delta);
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
            return FNoCopy(state.DeepCopy(), action);
        }

        public static GameInstance FNoCopy(GameInstance state, UctAction action) {
            switch (action.Type) {
                case UctActionType.Null:
                    // do nothing
                    break;
                case UctActionType.EndTurn:
                    state.NextMobOrNewTurn();
                    break;
                case UctActionType.AbilityUse:
                    state.FastUse(action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.Move:
                    state.FastMove(action.MobId, action.Coord);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value of {action.Type}");
            }

            return state;
        }

        public static float DefaultPolicy(GameInstance game) {
            if (game.IsFinished) {
                // TODO - ma to byt 1?
                return 1;
                //throw new InvalidOperationException("The game is already finished.");
            }

            var rnd = new Random();

            Debug.Assert(game.CurrentTeam.HasValue, "game.CurrentTeam.HasValue");
            TeamColor startingTeam = game.CurrentTeam.Value;

            var copy = game.DeepCopy();
            int iterations = 1000;

            while (!copy.IsFinished && iterations-- > 0) {
                var action = DefaultPolicyAction(copy);
                FNoCopy(copy, action);
                copy.State.SlowUpdateIsFinished(copy.MobManager);
            }

            if (iterations <= 0) {
                return 0;
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
                if (node != null && node.Action.Type == UctActionType.EndTurn)
                {
                    delta = -delta;
                }

                
            }
        }

        public static UctAction DefaultPolicyAction(GameInstance state) {
            var mobId = state.TurnManager.CurrentMob;

            if (mobId == null)
                throw new InvalidOperationException("Requesting mob action when there is no current mob.");

            var pathfinder = state.Pathfinder;

            var mobInfo = state.MobManager.MobInfos[mobId.Value];
            var mobInstance = state.State.MobInstances[mobId.Value];

            int? abilityId = null;
            Ability ability = null;
            foreach (var possibleAbilityId in mobInfo.Abilities) {
                var possibleAbility = state.MobManager.AbilityForId(possibleAbilityId);

                if (possibleAbility.Cost <= mobInstance.Ap &&
                    state.State.Cooldowns[possibleAbilityId] == 0) {
                    ability = possibleAbility;
                    abilityId = possibleAbilityId;
                }
            }

            int spellTarget = MobInstance.InvalidId;
            int moveTarget = MobInstance.InvalidId;

            foreach (var possibleTarget in state.MobManager.Mobs) {
                var possibleTargetInstance = state.State.MobInstances[possibleTarget];
                var possibleTargetInfo = state.MobManager.MobInfos[possibleTarget];
                if (possibleTargetInstance.Hp <= 0) continue;

                // TODO - mela by to byt viditelna vzdalenost
                if (possibleTargetInfo.Team != mobInfo.Team) {
                    if (abilityId.HasValue &&
                        pathfinder.Distance(mobInstance.Coord, possibleTargetInstance.Coord) <= ability.Range) {
                        spellTarget = possibleTarget;
                        break;
                    }

                    moveTarget = possibleTarget;
                }
            }

            if (spellTarget != MobInstance.InvalidId) {
                Debug.Assert(abilityId.HasValue);
                return UctAction.AbilityUseAction(abilityId.Value, mobId.Value, spellTarget);
                //state.FastUse(abilityId.Value, mobId.Value, spellTarget);
            } else if (moveTarget != MobInstance.InvalidId) {
                var action = FastMoveTowardsEnemy(state, mobId.Value, moveTarget);
                if (action.Type == UctActionType.Null) {
                    return UctAction.EndTurnAction();
                } else {
                    return action;
                }
                //FastMoveTowardsEnemy(mobId.Value, moveTarget);
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

        public static UctAction FastMoveTowardsEnemy(GameInstance state, int mobId, int targetId) {
            var pathfinder = state.Pathfinder;
            var mobInstance = state.State.MobInstances[mobId];
            var targetInstance = state.State.MobInstances[targetId];

            var moveTarget = pathfinder.FurthestPointToTarget(mobInstance, targetInstance);

            if (moveTarget != null && pathfinder.Distance(mobInstance.Coord, moveTarget.Value) <= mobInstance.Ap) {
                return UctAction.MoveAction(mobId, moveTarget.Value);
                //state.FastMove(mobId, moveTarget.Value);
            } else if (moveTarget == null) {
                // TODO - intentionally doing nothing
                return UctAction.EndTurnAction();
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
                return UctAction.EndTurnAction();
            }
        }

        public static List<UctAction> PossibleActions(GameInstance state) {
            var result = new List<UctAction> {
                UctAction.EndTurnAction()
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
                            result.Add(UctAction.MoveAction(mobId, coord));
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
                                result.Add(UctAction.AbilityUseAction(abilityId, mobId, targetId));
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