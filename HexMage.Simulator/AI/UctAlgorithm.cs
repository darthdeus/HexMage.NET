#define DOTGRAPH
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class UctAlgorithm {
        public static int actions = 0;
        public static int searchCount = 0;

        public List<UctNode> UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initialState.DeepCopy());

            int iterations = _thinkTime * 1000;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(root);
                float delta = DefaultPolicy(v.State, initialState.CurrentTeam.Value);
                Backup(v, delta);
            }

#if DOTGRAPH
            var builder = new StringBuilder();

            builder.AppendLine("digraph G {");
            PrintDot(builder, root);
            builder.AppendLine("}");

            string str = builder.ToString();

            string dirname = @"c:\dev\graphs";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            File.WriteAllText($"c:\\dev\\graphs\\graph{searchCount}.dot", str);
#endif
            searchCount++;

            //UctNode result = root.Children[0];

            //foreach (var child in root.Children) {
            //    if (child.Q > result.Q) {
            //        result = child;
            //    }
            //}

            //return result;

            var result = new List<UctNode>();
            UctNode current = root;

            do {
                if (current.Children.Count == 0) break;

                UctNode max = current.Children[0];

                foreach (var child in current.Children) {
                    if (child.Q > max.Q) {
                        max = child;
                    }
                }

                if (max.Action.Type != UctActionType.EndTurn) {
                    result.Add(max);
                }

                current = max;
            } while (current.Action.Type != UctActionType.EndTurn);

            return result;
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
                var child = new UctNode(0, 0, action, F(node.State, action));
                child.Parent = node;

                node.Children.Add(child);

                return child;
            } catch (ArgumentOutOfRangeException e) {
                Debugger.Break();
                throw;
            }
        }

        public static GameInstance F(GameInstance state, UctAction action) {
            return FNoCopy(state.CopyStateOnly(), action);
        }

        public static readonly Dictionary<UctActionType, int> ActionCounts = new Dictionary<UctActionType, int>();
        private readonly int _thinkTime;

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

        public UctAlgorithm(int thinkTime) {
            _thinkTime = thinkTime;
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
                    // TODO - gameinstance co se jmenuje state?
                    Debug.Assert(state.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    state.FastMove(action.MobId, action.Coord);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value of {action.Type}");
            }

            return state;
        }

        public static float DefaultPolicy(GameInstance game, TeamColor startingTeam) {
            if (game.IsFinished) {
                if (game.VictoryTeam.HasValue) {
                    if (startingTeam == game.VictoryTeam.Value) {
                        return 1;
                    } else {
                        return -1;
                    }
                } else {
                    return 0;
                }
            }

            var rnd = new Random();

            Debug.Assert(game.CurrentTeam.HasValue, "game.CurrentTeam.HasValue");

            var copy = game.CopyStateOnly();
            int iterations = 100;

            while (!copy.IsFinished && iterations-- > 0) {
                var action = DefaultPolicyAction(copy);
                if (action.Type == UctActionType.Null) {
                    throw new InvalidOperationException();
                }

                FNoCopy(copy, action);

                // TODO - odebrat az se opravi
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
                // 5. puntik, nema se delta prepnout kdyz ja jsem EndTurn, a ne muj rodic?
                if (node != null && node.Action.Type == UctActionType.EndTurn) {
                    // 6. puntik, kdy se ma prepinat?
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
                    moveTarget = possibleTarget;

                    var from = mobInstance.Coord;
                    var to = possibleTargetInstance.Coord;

                    if (abilityId.HasValue && state.Map.AxialDistance(from, to) <= ability.Range) {
                        if (state.Map.IsVisible(from, to)) {
                            spellTarget = possibleTarget;
                            break;
                        }
                    }

                    // TODO - tohle uz neni potreba?
                    //if (abilityId.HasValue &&
                    //    pathfinder.Distance(mobInstance.Coord, possibleTargetInstance.Coord) <= ability.Range) {
                    //    spellTarget = possibleTarget;
                    //    break;
                    //}
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

        public static List<UctAction> PossibleActions(GameInstance state, bool allowMove, bool allowEndTurn) {
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

                    //Console.WriteLine($"Allowed {abilityId} not on cooldown");

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

                if (allowMove) {
                    // TODO - proper move action concatenation
                    var moveActions = new List<UctAction>();

                    // TODO - properly define max actions
                    int count = 2;
                    foreach (var coord in state.Map.AllCoords) {
                        if (coord == mobInstance.Coord) continue;

                        if (state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
                            if (state.State.AtCoord(coord) == null && count-- > 0) {
                                moveActions.Add(UctAction.MoveAction(mobId, coord));
                                //result.Add(UctAction.MoveAction(mobId, coord));
                            }
                        }
                    }

                    if (count == 0) {
                        //Console.WriteLine("More than 100 possible move actions.");
                    }

                    Shuffle(moveActions);

                    result.AddRange(moveActions.Take(20));
                }
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

        // TODO - replace with a global RNG
        private static Random rng = new Random();

        public static void Shuffle<T>(IList<T> list) {
            // TODO - rewrite this to be better
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}