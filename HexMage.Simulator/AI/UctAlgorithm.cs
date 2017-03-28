#define XML
//#define DOTGRAPH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator {
    public class UctAlgorithm {
        public static int actions = 0;
        public static int searchCount = 0;

        public UctSearchResult UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initialState.DeepCopy());

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int totalIterations = _thinkTime * 1000;
            int iterations = totalIterations;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(root);
                float delta = DefaultPolicy(v.State, initialState.CurrentTeam.Value);
                Backup(v, delta);
            }

            stopwatch.Stop();

#if XML
            string filename = @"c:\dev\graphs\xml\iter-" + searchCount + ".xml";
            using (var writer = new StreamWriter(filename)) {
                new XmlTreePrinter(root).Print(writer);
            };
#endif
#if DOTGRAPH
            PrintDotgraph(root);            
#endif
            searchCount++;

            //Console.WriteLine($"Total Q: {root.Children.Sum(c => c.Q)}, N: {root.Children.Sum(c => c.N)}");

            var actions = SelectBestActions(root);
            return new UctSearchResult(actions, (double) stopwatch.ElapsedMilliseconds / (double) totalIterations);
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
            try {
                var type = node.Action.Type;

                var allowMove = type != UctActionType.Move && type != UctActionType.DefensiveMove;
                node.PrecomputePossibleActions(allowMove, true || type != UctActionType.EndTurn);

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
                $"EndTurn: {ActionCounts[UctActionType.EndTurn]}, Ability: {ActionCounts[UctActionType.AbilityUse]}, Move: {ActionCounts[UctActionType.Move]}, Null: {ActionCounts[UctActionType.Null]}, DefensiveMove: {ActionCounts[UctActionType.DefensiveMove]}";
        }

        static UctAlgorithm() {
            ActionCounts.Add(UctActionType.Null, 0);
            ActionCounts.Add(UctActionType.EndTurn, 0);
            ActionCounts.Add(UctActionType.AbilityUse, 0);
            ActionCounts.Add(UctActionType.Move, 0);
            ActionCounts.Add(UctActionType.DefensiveMove, 0);
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
                case UctActionType.DefensiveMove:
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

            var rnd = Generator.Random;

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

                delta *= 0.95f;
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

                Utils.Log(LogSeverity.Debug, nameof(AiRuleBasedController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
                return UctAction.EndTurnAction();
            }
        }

        public static List<UctAction> PossibleActions(GameInstance state, bool allowMove, bool allowEndTurn) {
            var result = new List<UctAction>();

            bool foundAbilityUse = false;
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
                                foundAbilityUse = true;
                                result.Add(UctAction.AbilityUseAction(abilityId, mobId, targetId));
                            }
                        }
                    }
                }

                // We disable movement if there is a possibility to cast abilities.
                if (allowMove && !foundAbilityUse) {
                    GenerateAggressiveMoveActions(state, mobInstance, mobId, result);
                }

                if (allowMove) {
                    GenerateDefensiveMoveActions(state, mobInstance, mobId, result);
                }
            } else {
                throw new InvalidOperationException();
                Utils.Log(LogSeverity.Warning, nameof(UctNode),
                          "Final state reached while trying to compute possible actions.");
            }

            const bool endTurnAsLastResort = true;

            if (allowEndTurn) {
                // We would skip end turn if there are not enough actions.
                // TODO - generate more move actions if we don't have enough?
                if (!endTurnAsLastResort || result.Count <= 1) {
                    result.Add(UctAction.EndTurnAction());
                }
            }

            return result;
        }

        private static void GenerateDefensiveMoveActions(GameInstance state, MobInstance mobInstance, int mobId,
                                                         List<UctAction> result) {
            var heatmap = state.BuildHeatmap();
            //var usedValues = new HashSet<int>();
            var coords = new List<AxialCoord>();

            foreach (var coord in heatmap.Map.AllCoords) {
                if (heatmap.Map[coord] != heatmap.MinValue) continue;
                if (state.Map[coord] == HexType.Wall) continue;
                if (state.State.AtCoord(coord).HasValue) continue;

                bool canMoveTo = state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap;

                if (!canMoveTo) continue;


                // TODO - samplovat po sektorech
                coords.Add(coord);

                // TODO - tohle je asi overkill, ale nemame lepsi zpusob jak iterovat
                //int value = heatmap.Map[coord];

                //if (usedValues.Contains(value)) continue;

                //usedValues.Add(value);
                //result.Add(UctAction.MoveAction(mobId, coord));
            }

            Shuffle(coords);
            for (int i = 0; i < Math.Min(coords.Count, 3); i++) {
                result.Add(UctAction.DefensiveMoveAction(mobId, coords[i]));
            }
        }

        private static void GenerateAggressiveMoveActions(GameInstance state, MobInstance mobInstance, int mobId,
                                                          List<UctAction> result) {
            var mobInfo = state.MobManager.MobInfos[mobId];
            var enemyDistances = new HexMap<int>(state.Size);

            // TODO - preferovat blizsi policka pri vyberu akci?
            foreach (var enemyInstance in state.State.MobInstances) {
                AxialCoord myCoord = mobInstance.Coord;
                AxialCoord? closestCoord = null;
                int? distance = null;

                foreach (var coord in enemyDistances.AllCoords) {
                    if (state.Map[coord] == HexType.Wall) continue;
                    if (!state.Map.IsVisible(coord, enemyInstance.Coord)) continue;
                    if (state.State.AtCoord(coord).HasValue) continue;

                    int remainingAp = mobInstance.Ap - state.Pathfinder.Distance(mobInstance.Coord, coord);

                    foreach (var abilityId in mobInfo.Abilities) {
                        var ability = state.MobManager.Abilities[abilityId];
                        bool withinRange = ability.Range >= coord.Distance(enemyInstance.Coord);
                        bool enoughAp = remainingAp >= ability.Cost;

                        if (withinRange && enoughAp) {
                            int myDistance = state.Pathfinder.Distance(myCoord, coord);

                            if (!closestCoord.HasValue) {
                                closestCoord = coord;
                                distance = myDistance;
                            } else if (distance.Value > myDistance) {
                                closestCoord = coord;
                                distance = myDistance;
                            }
                        }
                    }
                }

                if (closestCoord.HasValue) {
                    result.Add(UctAction.MoveAction(mobId, closestCoord.Value));
                }
            }

            //    //// TODO - properly define max actions
            //    int count = 2;
            //    foreach (var coord in state.Map.AllCoords) {
            //        if (coord == mobInstance.Coord) continue;

            //        if (state.Pathfinder.Distance(mobInstance.Coord, coord) <= mobInstance.Ap) {
            //            if (state.State.AtCoord(coord) == null && count-- > 0) {
            //                moveActions.Add(UctAction.MoveAction(mobId, coord));
            //                //result.Add(UctAction.MoveAction(mobId, coord));
            //            }
            //        }
            //    }

            //    if (count == 0) {
            //        //Console.WriteLine("More than 100 possible move actions.");
            //    }

            //    Shuffle(moveActions);

            //    result.AddRange(moveActions.Take(20));
        }

        public static void Shuffle<T>(IList<T> list) {
            // TODO - rewrite this to be better
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Generator.Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        private List<UctAction> SelectBestActions(UctNode root) {
            //UctNode result = root.Children[0];

            //foreach (var child in root.Children) {
            //    if (child.Q > result.Q) {
            //        result = child;
            //    }
            //}

            //return result;

            var result = new List<UctAction>();
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
                    result.Add(max.Action);
                }

                current = max;
            } while (current.Action.Type != UctActionType.EndTurn);

            return result;
        }

        private void PrintDotgraph(UctNode root) {
            var builder = new StringBuilder();

            builder.AppendLine("digraph G {");
            int budget = 2;
            PrintDotNode(builder, root, budget);
            builder.AppendLine("}");

            string str = builder.ToString();

            string dirname = @"c:\dev\graphs";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            File.WriteAllText($"c:\\dev\\graphs\\graph{searchCount}.dot", str);
        }

        private void PrintDotNode(StringBuilder builder, UctNode node, int budget) {
            if (budget == 0) return;

            foreach (var child in node.Children) {
                builder.AppendLine($"\"{node}\" -> \"{child}\"");
            }

            foreach (var child in node.Children) {
                PrintDotNode(builder, child, budget - 1);
            }
        }
    }
}