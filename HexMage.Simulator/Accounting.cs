using System;
using System.Text;
using System.Threading;
using HexMage.Simulator.AI;

namespace HexMage.Simulator.Model {
    /// <summary>
    /// Wraps win statistics for a number of AIs in a threadsafe manner.
    /// </summary>
    public static class Accounting {
        public static int MctsWins = 0;
        public static int RandomAiWins = 0;
        public static int RuleBasedAiWins = 0;

        public static void Reset() {
            MctsWins = 0;
            RandomAiWins = 0;
            RuleBasedAiWins = 0;
        }

        public static void IncrementWinner(IMobController controller) {
            var type = controller.GetType();
            if (type == typeof(MctsController)) {
                Interlocked.Increment(ref MctsWins);
            } else if (type == typeof(AiRandomController)) {
                Interlocked.Increment(ref RandomAiWins);
            } else if (type == typeof(AiRuleBasedController)) {
                Interlocked.Increment(ref RuleBasedAiWins);
            } else {
                throw new ArgumentException($"Invalid type of {type}", nameof(controller));
            }
        }

        public static string GetStats() {
            var builder = new StringBuilder();
            if (MctsWins > 0) builder.AppendLine($"MCTS: {MctsWins}");
            if (RandomAiWins > 0) builder.AppendLine($"Random: {RandomAiWins}");
            if (RuleBasedAiWins > 0) builder.AppendLine($"Rule: {RuleBasedAiWins}");

            return builder.ToString();
        }
    }
}