using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class UctAlgorithm {
        public static int actions = 0;

        public UctNode UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 1, UctAction.NullAction(), initialState);

            int iterations = 10000;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(root);
                float delta = DefaultPolicy(v.State);
                Backup(v, delta);
            }

            var builder = new StringBuilder();

            builder.AppendLine("digraph G {");
            PrintDot(builder, root);
            builder.AppendLine("}");

            string str = builder.ToString();

            File.WriteAllText("c:\\dev\\graph.dot", str);

            return BestChild(root);
        }

        void PrintDot(StringBuilder builder, UctNode node) {            
            foreach (var child in node.Children) {
                builder.AppendLine($"\"{node}\" -> \"{child}\"");
            }

            foreach (var child in node.Children) {
                PrintDot(builder, child);
            }
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

            //if (node.Action.Type == UctActionType.EndTurn && best.Action.Type == UctActionType.EndTurn) {
            //    throw new InvalidOperationException();
            //}

            return best;
        }

        public float UcbValue(UctNode parent, UctNode node) {
            return (float) (node.Q / node.N + Math.Sqrt(2 * Math.Log(parent.N) / node.N));
        }

        public UctNode Expand(UctNode node) {
            try {
                var type = node.Action.Type;
                node.PrecomputePossibleActions(type != UctActionType.Move, true || type != UctActionType.EndTurn);

                var action = node.PossibleActions[node.Children.Count];
                var child = new UctNode(0, 1, action, F(node.State, action));
                child.Parent = node;

                node.Children.Add(child);

                return child;
            } catch (ArgumentOutOfRangeException e) {
                Debugger.Break();
                throw;
            }
        }

        public static GameInstance F(GameInstance state, UctAction action) {
            return FNoCopy(state.DeepCopy(), action);
        }

        public static Dictionary<UctActionType, int> ActionCounts = new Dictionary<UctActionType, int>();

        public static string ActionCountString() {
            return
                $"EndTurn: {ActionCounts[UctActionType.EndTurn]}, Ability: {ActionCounts[UctActionType.AbilityUse]}, Move: {ActionCounts[UctActionType.Move]}, Null: {ActionCounts[UctActionType.Null]}";
        }

        static UctAlgorithm() {
            ActionCounts.Add(UctActionType.Null, 0);
            ActionCounts.Add(UctActionType.EndTurn, 0);
            ActionCounts.Add(UctActionType.AbilityUse, 0);
            ActionCounts.Add(UctActionType.Move, 0);
        }

        public static GameInstance FNoCopy(GameInstance state, UctAction action) {
            actions++;
            ActionCounts[action.Type]++;

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
            int iterations = 100;

            while (!copy.IsFinished && iterations-- > 0) {
                var action = DefaultPolicyAction(copy);
                if (action.Type == UctActionType.Null) {
                    throw new InvalidOperationException();
                }

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
                if (node != null && node.Action.Type == UctActionType.EndTurn) {
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
            } else if (moveTarget != MobInstance.InvalidId) {
                var action = FastMoveTowardsEnemy(state, mobId.Value, moveTarget);

                if (action.Type == UctActionType.Null) {
                    return UctAction.EndTurnAction();
                } else {
                    return action;
                }
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
            } else if (moveTarget == null) {
                // TODO - intentionally doing nothing
                return UctAction.EndTurnAction();
            } else {
                Console.WriteLine("Move failed!");

                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
                return UctAction.EndTurnAction();
            }
        }

        public static List<UctAction> PossibleActions(GameInstance state, bool allowMove = true,
                                                      bool allowEndTurn = true) {
            var result = new List<UctAction>();

            var currentMob = state.TurnManager.CurrentMob;
            if (currentMob.HasValue) {
                var mobId = currentMob.Value;

                var mobInstance = state.State.MobInstances[mobId];
                var mobInfo = state.MobManager.MobInfos[mobId];

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
                //if (allowMove) {
                //    int count = 4;
                //    foreach (var coord in state.Map.AllCoords) {
                //        if (coord == mobInstance.Coord) continue;

                //        if (state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
                //            if (state.State.AtCoord(coord) == null && count-- > 0) {
                //                result.Add(UctAction.MoveAction(mobId, coord));
                //            }
                //        }
                //    }
                //}
            } else {
                throw new InvalidOperationException();
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
            }

            if (allowEndTurn) {
                result.Add(UctAction.EndTurnAction());
            }

            return result;
        }
    }
}