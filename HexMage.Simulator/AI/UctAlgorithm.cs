//#define XML
//#define DOTGRAPH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class UctAlgorithm {
        // TODO - extrahovat do args nebo configuraku
        public static int Actions = 0;
        public static int SearchCount = 0;

        public static int ExpandCount = 0;
        public static int BestChildCount = 0;

        public UctSearchResult UctSearch(GameInstance initialState) {
            var root = new UctNode(0, 0, UctAction.NullAction(), initialState.DeepCopy());

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // TODO - for loop? lul
            int totalIterations = _thinkTime;
            int iterations = totalIterations;

            while (iterations-- > 0) {
                OneIteration(root, initialState.CurrentTeam.Value);
            }

            stopwatch.Stop();

            UctDebug.PrintTreeRepresentation(root);
            SearchCount++;

            //Console.WriteLine($"Total Q: {root.Children.Sum(c => c.Q)}, N: {root.Children.Sum(c => c.N)}");

            var actions = SelectBestActions(root);
            return new UctSearchResult(actions, (double) stopwatch.ElapsedMilliseconds / (double) totalIterations);
        }

        public static float OneIteration(UctNode root, TeamColor startingTeam) {
            UctNode v = TreePolicy(root);
            // TODO: ma tu byt root node team, nebo aktualni?
            float delta = DefaultPolicy(v.State, startingTeam);
            Backup(v, delta);
            return delta;
        }


        public static UctNode TreePolicy(UctNode node) {
            while (!node.IsTerminal) {
                if (!node.IsFullyExpanded) {
                    ExpandCount++;
                    return Expand(node);
                } else {
                    BestChildCount++;
                    node = BestChild(node);
                }
            }

            return node;
        }

        public static UctNode BestChild(UctNode node, double k = 2) {
            if (node.Children.Count == 0) return null;

            UctNode best = node.Children[0];
            foreach (var child in node.Children) {
                if (UcbValue(node, child, k) > UcbValue(node, best, k)) {
                    best = child;
                }
            }

            return best;
        }

        public static float UcbValue(UctNode parent, UctNode node, double k) {
            return (float) (node.Q / node.N + Math.Sqrt(k * Math.Log(parent.N) / node.N));
        }

        public static UctNode Expand(UctNode node) {
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

        // TODO: extract all the accounting
        public static readonly Dictionary<UctActionType, int> ActionCounts = new Dictionary<UctActionType, int>();
        private readonly int _thinkTime;

        public static string ActionCountString() {
            return
                $"E: {ActionCounts[UctActionType.EndTurn]}, " +
                $"A: {ActionCounts[UctActionType.AbilityUse]}, " +
                $"M: {ActionCounts[UctActionType.Move]}, " +
                $"N: {ActionCounts[UctActionType.Null]}, " +
                $"D: {ActionCounts[UctActionType.DefensiveMove]}, " +
                $"AM: {ActionCounts[UctActionType.AttackMove]}";
        }

        static UctAlgorithm() {
            ActionCounts.Add(UctActionType.Null, 0);
            ActionCounts.Add(UctActionType.EndTurn, 0);
            ActionCounts.Add(UctActionType.AbilityUse, 0);
            ActionCounts.Add(UctActionType.Move, 0);
            ActionCounts.Add(UctActionType.DefensiveMove, 0);
            ActionCounts.Add(UctActionType.AttackMove, 0);
        }

        public UctAlgorithm(int thinkTime) {
            _thinkTime = thinkTime;
        }

        public static GameInstance FNoCopy(GameInstance state, UctAction action) {
            Actions++;
            ActionCounts[action.Type]++;

            Constants.WriteLogLine(action);

            ReplayRecorder.Instance.Actions.Add(action);

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
                case UctActionType.AttackMove:
                    Debug.Assert(state.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    state.FastMove(action.MobId, action.Coord);
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

        public static float PlayoutAction(GameInstance game, UctAction action, TeamColor startingTeam) {
            return DefaultPolicy(F(game, action), startingTeam);
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

            while (!copy.IsFinished && iterations-- > 0) {
                var action = ActionGenerator.DefaultPolicyAction(copy);

                //Console.WriteLine(action);
                //if (action.Type == UctActionType.EndTurn) {
                //    Console.WriteLine("**************************");
                //}

                if (action.Type == UctActionType.Null) {
                    throw new InvalidOperationException();
                }

                FNoCopy(copy, action);

                // TODO - odebrat az se opravi
                copy.State.SlowUpdateIsFinished(copy.MobManager);
            }

            if (iterations <= 0) {
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

                float totalMax = 0;
                float totalCurrent = 0;
                foreach (var mobId in game.MobManager.Mobs) {
                    var mobInfo = game.MobManager.MobInfos[mobId];
                    if (mobInfo.Team != opposingTeam) continue;

                    totalMax += mobInfo.MaxHp;
                    totalCurrent += game.State.MobInstances[mobId].Hp;
                }

                return 1 - (totalCurrent / totalMax);
            } else {
                float result;

                // TODO - tohle duplikuje to dole
                if (victoryTeam.HasValue) {
                    if (startingTeam == victoryTeam.Value) {
                        result = 1;
                    } else {
                        result = -1;
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

                if (node != null && node.Action.Type == UctActionType.EndTurn) {
                    delta = -delta;
                }
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
                    if (child.Q / child.N > max.Q / max.N) {
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
    }
}