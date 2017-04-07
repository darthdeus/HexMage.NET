﻿using System;
using System.Threading;
using HexMage.Simulator.AI;

namespace HexMage.Simulator.Model {
    public static class Accounting {
        public static int MctsWins = 0;
        public static int RandomAiWins = 0;
        public static int RuleBasedAiWins = 0;
        public static int FlatMonteCarloWins = 0;

        public static void IncrementWinner(IMobController controller) {
            var type = controller.GetType();
            if (type == typeof(MctsController)) {
                Interlocked.Increment(ref MctsWins);
            } else if (type == typeof(AiRandomController)) {
                Interlocked.Increment(ref RandomAiWins);
            } else if (type == typeof(AiRuleBasedController)) {
                Interlocked.Increment(ref RuleBasedAiWins);
            } else if (type == typeof(FlatMonteCarloController)) {
                Interlocked.Increment(ref FlatMonteCarloWins);
            } else {
                throw new ArgumentException($"Invalid type of {type}", nameof(controller));
            }
        }

        public static string GetStats() {
            return $"MCTS: {MctsWins}\n" +
                   $"Random: {RandomAiWins}\n" +
                   $"Rule: {RuleBasedAiWins}\n" +
                   $"Flat: {FlatMonteCarloWins}";
        }
    }
}