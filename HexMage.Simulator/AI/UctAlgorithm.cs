//#define XML
//#define DOTGRAPH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class Evolution<T> {
        public Evolution(Func<T, float> fitnessFunc) {
            
        }
    }

    public class UctAlgorithm {
        // TODO - extrahovat do args nebo configuraku
        public static int SearchCount = 0;

        public static int ExpandCount = 0;
        public static int BestChildCount = 0;

        public static int TotalIterationCount = 0;

        public static readonly RollingAverage MillisecondsPerIterationAverage = new RollingAverage();

        private readonly int _thinkTime;

        public UctAlgorithm(int thinkTime) {
            _thinkTime = thinkTime;
        }

        public UctSearchResult UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initialState.DeepCopy());

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
                //} while (iterations < 2000);
            } while (stopwatch.ElapsedMilliseconds < _thinkTime);

            stopwatch.Stop();

            //UctDebug.PrintTreeRepresentation(root);
            Interlocked.Increment(ref SearchCount);

            var actions = SelectBestActions(root);

            var millisecondsPerIteration = (double) stopwatch.ElapsedMilliseconds / (double) iterations;

            return new UctSearchResult(actions, millisecondsPerIteration);
        }

        public static float OneIteration(UctNode root, TeamColor startingTeam) {
            UctNode v = TreePolicy(root, startingTeam);
            // TODO: ma tu byt root node team, nebo aktualni?
            float delta = DefaultPolicy(v.State, startingTeam);
            Backup(v, delta);
            return delta;
        }

        public static UctNode TreePolicy(UctNode node, TeamColor startingTeam) {
            while (!node.IsTerminal) {
                if (!node.IsFullyExpanded) {
                    Interlocked.Increment(ref ExpandCount);
                    return Expand(node);
                } else {
                    Interlocked.Increment(ref BestChildCount);
                    node = BestChild(node, startingTeam);
                }
            }

            return node;
        }

        public static UctNode Expand(UctNode node) {
            try {
                var type = node.Action.Type;

                var allowMove = type != UctActionType.Move && type != UctActionType.DefensiveMove;
                node.PrecomputePossibleActions(allowMove, true || type != UctActionType.EndTurn);

                var action = node.PossibleActions[node.Children.Count];
                var child = new UctNode(0, 0, action, ActionEvaluator.F(node.State, action));
                child.Parent = node;

                node.Children.Add(child);

                return child;
                // TODO: fuj
            } catch (ArgumentOutOfRangeException e) {
                Debugger.Break();
                throw;
            }
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
                throw new InvariantViolationException("MCTS playout timeout");
                ReplayRecorder.Instance.SaveAndClear(game);
                Utils.Log(LogSeverity.Error, nameof(UctAlgorithm),
                          $"DefaultPolicy ran out of time (over {maxDefaultPolicyIterations} iterations for playout), computed results are likely wrong.");
                return 0;
            }

            TeamColor? victoryTeam = copy.VictoryTeam;

            return CalculateDeltaReward(game, startingTeam, victoryTeam);
        }

        public static float CalculateDeltaReward(GameInstance game, TeamColor startingTeam, TeamColor? victoryTeam) {
            const bool rewardDamage = false;
            if (rewardDamage) {
                TeamColor opposingTeam = startingTeam == TeamColor.Red ? TeamColor.Blue : TeamColor.Red;

                return 1 - game.PercentageHp(opposingTeam);
            } else {
                float result;

                // TODO - tohle duplikuje to dole
                if (victoryTeam.HasValue) {
                    if (startingTeam == victoryTeam.Value) {
                        result = 1;
                    } else {
                        result = 0;
                    }
                } else {
                    result = 0;
                }

                if (Constants.UseHpPercentageScaling) {
                    result *= game.PercentageHp(startingTeam);
                }

                return result;
            }
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

                //UctNode max = current.Children[0];

                //foreach (var child in current.Children) {
                //    if (child.Q / child.N > max.Q / max.N) {
                //        max = child;
                //    }
                //}

                if (max.Q / max.N < 0.2) {
                    //Console.WriteLine($"Bad action {max.Q} / {max.N}, generating via rules");
                    var state = current.State.DeepCopy();
                    do {
                        var action = ActionGenerator.RuleBasedAction(state);
                        state = ActionEvaluator.F(state, action);
                        if (action.Type == UctActionType.EndTurn) {
                            return result;
                        } else {
                            result.Add(action);
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

            return result;
        }
    }
}