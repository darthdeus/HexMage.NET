//#define XML
#define DOTGRAPH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class UctAlgorithm {
        public static int SearchCount = 0;

        public static int ExpandCount = 0;
        public static int BestChildCount = 0;

        public static int TotalIterationCount = 0;

        public static readonly RollingAverage MillisecondsPerIterationAverage = new RollingAverage();

        private readonly int _thinkTime;
        private readonly double _exploExplo;
        private readonly bool _iterationsOverTime;

        public UctAlgorithm(int thinkTime, double exploExplo = 2, bool iterationsOverTime = true) {
            _thinkTime = thinkTime;
            _exploExplo = exploExplo;
            _iterationsOverTime = iterationsOverTime;
        }

        public UctSearchResult UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initialState.CopyStateOnly());

            if (!initialState.CurrentTeam.HasValue)
                throw new ArgumentException("Trying to do UCT search on a finished game.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var iterationStopwatch = new Stopwatch();
            var startingTeam = initialState.CurrentTeam.Value;
            int iterations = 0;
            do {
                iterations++;
                TotalIterationCount++;

                iterationStopwatch.Restart();
                OneIteration(root, startingTeam);
                iterationStopwatch.Stop();

                MillisecondsPerIterationAverage.Add(iterationStopwatch.Elapsed.TotalMilliseconds);

                if (_iterationsOverTime) {
                    if (iterations >= _thinkTime) break;
                } else {
                    if (stopwatch.ElapsedMilliseconds >= _thinkTime) break;
                }
            } while (true);

            stopwatch.Stop();

#if DOTGRAPH
            UctDebug.PrintTreeRepresentation(root);
#endif
            Interlocked.Increment(ref SearchCount);

            var actions = SelectBestActions(root);

            var millisecondsPerIteration = (double) stopwatch.ElapsedMilliseconds / (double) iterations;

            return new UctSearchResult(actions, millisecondsPerIteration);
        }

        public float OneIteration(UctNode root, TeamColor startingTeam) {
            UctNode v = TreePolicy(root, startingTeam);
            // TODO: ma tu byt root node team, nebo aktualni?
            float delta = DefaultPolicy(v.State, startingTeam);
            Backup(v, delta);
            return delta;
        }

        public UctNode TreePolicy(UctNode node, TeamColor startingTeam) {
            bool wasDefense = node.Action.Type == UctActionType.DefensiveMove;

            while (!node.IsTerminal) {
                if (!node.IsFullyExpanded) {
                    Interlocked.Increment(ref ExpandCount);
                    var expanded = Expand(node);

                    var type = expanded.Action.Type;
                    var allowMove = type != UctActionType.Move && type != UctActionType.DefensiveMove;

                    if (!expanded.IsTerminal) {
                        expanded.PrecomputePossibleActions(allowMove, true);
                        if (expanded.PossibleActions.Count == 1) {
                            expanded = Expand(expanded);
                        }
                    }

                    return expanded;
                } else {
                    Interlocked.Increment(ref BestChildCount);
                    node = BestChild(node, startingTeam);

                    if (node.Action.Type == UctActionType.DefensiveMove) {
                        if (wasDefense) {
                            throw new InvalidOperationException();
                        }
                    }
                    wasDefense = node.Action.Type == UctActionType.DefensiveMove;
                }
            }

            return node;
        }

        public static UctNode Expand(UctNode node) {
            var type = node.Action.Type;

            var allowMove = type != UctActionType.Move && type != UctActionType.DefensiveMove;
            node.PrecomputePossibleActions(allowMove, true || type != UctActionType.EndTurn);

            var action = node.PossibleActions[node.Children.Count];
            var child = new UctNode(0, 0, action, ActionEvaluator.F(node.State, action));
            child.Parent = node;

            node.Children.Add(child);

            return child;
        }

        public static float PlayoutAction(GameInstance game, UctAction action, TeamColor startingTeam) {
            return DefaultPolicy(ActionEvaluator.F(game, action), startingTeam);
        }

        public static float DefaultPolicy(GameInstance game, TeamColor startingTeam) {
            if (game.IsFinished) {
                Debug.Assert(game.VictoryTeam.HasValue || game.AllDead, "game.VictoryTeam.HasValue");
                return CalculateDeltaReward(game, startingTeam, game.VictoryTeam);
            }

            Debug.Assert(game.CurrentTeam.HasValue, "game.CurrentTeam.HasValue");

            var copy = game.CopyStateOnly();
            const int maxDefaultPolicyIterations = 200;
            int iterations = maxDefaultPolicyIterations;

            ReplayRecorder.Instance.Clear();
            bool wasMove = false;

            while (!copy.IsFinished && iterations-- > 0) {
                var action = ActionGenerator.DefaultPolicyAction(copy);
                if (action.Type == UctActionType.Move) {
                    if (wasMove) {
                        action = UctAction.EndTurnAction();
                    }
                    wasMove = true;
                }

                if (action.Type == UctActionType.EndTurn) {
                    wasMove = false;
                }

                if (action.Type == UctActionType.Null) {
                    throw new InvalidOperationException();
                }

                ActionEvaluator.FNoCopy(copy, action);
            }

            if (iterations <= 0) {
                ReplayRecorder.Instance.SaveAndClear(game, 0);
                //throw new InvariantViolationException("MCTS playout timeout");
                Utils.Log(LogSeverity.Error, nameof(UctAlgorithm),
                          $"DefaultPolicy ran out of time (over {maxDefaultPolicyIterations} iterations for playout), computed results are likely wrong.");
                return 0;
            }

            TeamColor? victoryTeam = copy.VictoryTeam;

            return CalculateDeltaReward(game, startingTeam, victoryTeam);
        }

        public static float CalculateDeltaReward(GameInstance game, TeamColor startingTeam, TeamColor? victoryTeam) {
            float result;

            if (victoryTeam.HasValue) {
                if (startingTeam == victoryTeam.Value) {
                    result = 1;
                } else {
                    result = 0;
                }
            } else {
                result = 0;
            }

            return result;
        }

        public static void Backup(UctNode node, float delta) {
            while (node != null) {
                node.N++;
                node.Q += delta;
                node = node.Parent;

                // TODO: Nepouziva se, protoze odmeny jsou 0 nebo 1
                //if (node != null && node.Action.Type == UctActionType.EndTurn) {
                //    bool teamColorChanged = node.State.CurrentTeam != node.State.State.LastTeamColor;

                //    if (teamColorChanged) {
                //        //delta = -delta;
                //    }
                //}
            }
        }

        public static UctNode BestChild(UctNode node, TeamColor startingTeam, double k = 2) {
            if (node.Children.Count == 0) return null;

            return node.Children.FastMax(c => UcbValue(node, c, k, startingTeam));
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float UcbValue(UctNode parent, UctNode node, double k, TeamColor startingTeam) {
            float value = node.Q / node.N;

            if (node.State.CurrentTeam != startingTeam) {
                value = 1 - value;
            }

            return (float) (value + Math.Sqrt(k * Math.Log(parent.N) / node.N));
        }

        private List<UctAction> SelectBestActions(UctNode root) {
            var result = new List<UctAction>();
            UctNode current = root;

            do {
                if (current.Children.Count == 0) break;

                UctNode max = current.Children.FastMax(c => c.Q / c.N);
                if (max.Q / max.N < 0.2) {
                    var state = current.State.CopyStateOnly();
                    do {
                        var action = ActionGenerator.RuleBasedAction(state);
                        state = ActionEvaluator.F(state, action);
                        if (action.Type == UctActionType.EndTurn) {
                            goto done;
                        } else {
                            result.Add(action);

                            if (action.Type == UctActionType.DefensiveMove) {
                                goto done;
                            }
                        }
                    } while (true);
                }

                if (max.Action.Type != UctActionType.EndTurn) {
                    if (max.IsTerminal) {
                        //Console.WriteLine("Found terminal");
                    }
                    result.Add(max.Action);
                }

                current = max;
            } while (current.Action.Type != UctActionType.EndTurn);

            done:

            //bool wasDefense = false;
            //foreach (var action in result) {
            //    if (action.Type == UctActionType.DefensiveMove) {
            //        if (wasDefense) {
            //            UctDebug.PrintDotgraph(root, () => 1);
            //            Debugger.Break();
            //            throw new InvalidOperationException($"Double defensive move");
            //        }
            //        wasDefense = true;
            //    } else {
            //        wasDefense = false;
            //    }
            //}

            return result;
        }
    }
}