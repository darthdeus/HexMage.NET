//#define XML
//#define DOTGRAPH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class UctAlgorithm {
        // TODO - extrahovat do args nebo configuraku
        public const bool UseHpPercentageScaling = true;

        public static int Actions = 0;
        public static int SearchCount = 0;

        public static int ExpandCount = 0;
        public static int BestChildCount = 0;

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

            PrintTreeRepresentation(root);
            SearchCount++;

            //Console.WriteLine($"Total Q: {root.Children.Sum(c => c.Q)}, N: {root.Children.Sum(c => c.N)}");

            var actions = SelectBestActions(root);
            return new UctSearchResult(actions, (double) stopwatch.ElapsedMilliseconds / (double) totalIterations);
        }


        public UctNode TreePolicy(UctNode node) {
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
                $"EndTurn: {ActionCounts[UctActionType.EndTurn]}, " +
                $"Ability: {ActionCounts[UctActionType.AbilityUse]}, " +
                $"Move: {ActionCounts[UctActionType.Move]}, " +
                $"Null: {ActionCounts[UctActionType.Null]}, " +
                $"DefensiveMove: {ActionCounts[UctActionType.DefensiveMove]}, " +
                $"AttackMove: {ActionCounts[UctActionType.AttackMove]}";
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

        public static float DefaultPolicy(GameInstance game, TeamColor startingTeam) {
            if (game.IsFinished) {
                Debug.Assert(game.VictoryTeam.HasValue, "game.VictoryTeam.HasValue");
                return CalculateDeltaReward(game, startingTeam, game.VictoryTeam);
            }

            Debug.Assert(game.CurrentTeam.HasValue, "game.CurrentTeam.HasValue");

            var copy = game.CopyStateOnly();
            int iterations = 100;

            while (!copy.IsFinished && iterations-- > 0) {
                var action = ActionGenerator.DefaultPolicyAction(copy);
                if (action.Type == UctActionType.Null) {
                    throw new InvalidOperationException();
                }

                FNoCopy(copy, action);

                // TODO - odebrat az se opravi
                copy.State.SlowUpdateIsFinished(copy.MobManager);
            }

            if (iterations <= 0) {
                Utils.Log(LogSeverity.Error, nameof(UctAlgorithm),
                          "DefaultPolicy ran out of time (over 100 iterations for playout), computed results are likely wrong.");
                return 0;
            }

            TeamColor? victoryTeam = copy.VictoryTeam;

            return CalculateDeltaReward(game, startingTeam, victoryTeam);
        }

        public static float CalculateDeltaReward(GameInstance game, TeamColor startingTeam, TeamColor? victoryTeam) {
            // TODO - tohle duplikuje to dole
            if (victoryTeam.HasValue) {
                if (startingTeam == victoryTeam.Value) {
                    if (UseHpPercentageScaling) {
                        return 1 * game.PercentageHp(startingTeam);
                    } else {
                        return 1;
                    }
                } else {
                    if (UseHpPercentageScaling) {
                        return -1 * (1 - game.PercentageHp(victoryTeam.Value));
                    } else {
                        return -1;
                    }
                }
            } else {
                // The game was a draw
                return 0;
            }
        }

        public void Backup(UctNode node, float delta) {
            while (node != null) {
                node.N++;
                node.Q += delta;
                node = node.Parent;

                //delta *= 0.95f;
                // 5. puntik, nema se delta prepnout kdyz ja jsem EndTurn, a ne muj rodic?
                if (node != null && node.Action.Type == UctActionType.EndTurn) {
                    // 6. puntik, kdy se ma prepinat?
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
            int budget = 4;
            PrintDotNode(builder, root, budget);
            builder.AppendLine("}");

            string str = builder.ToString();

            string dirname = @"c:\dev\graphs";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            File.WriteAllText($"c:\\dev\\graphs\\graph{SearchCount}.dot", str);
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


        private void PrintTreeRepresentation(UctNode root) {
#if XML
            var dirname = @"c:\dev\graphs\xml\";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            string filename = dirname + "iter-" + searchCount + ".xml";

            using (var writer = new StreamWriter(filename)) {
                new XmlTreePrinter(root).Print(writer);
            }
#endif
#if DOTGRAPH
            PrintDotgraph(root);            
#endif
        }
    }
}