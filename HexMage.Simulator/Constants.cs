using System.IO;

namespace HexMage.Simulator {
    public static class Constants {
        public static bool EnableGnuPlot = true;

        // Benchmark controls
        public static bool MctsBenchmark = true;
        public static bool EvaluateAis = false;

        // Game Evaluation
        public static int MaxPlayoutEvaluationIterations = 100;

        // MCTS
        /// <summary>
        /// When enabled, UCB reward will be scaled by the HP percentage left.
        /// 
        /// TODO - experiment
        /// </summary>
        public static bool UseHpPercentageScaling = true;
        /// <summary>
        /// When enabled, rewards are slowly dampened each turn, penalizing rewards
        /// from longer games.
        /// </summary>
        public static bool DampenLongRewards = false;
        public static float DampeningFactor = 0.95f;
        public static bool MctsLogging = false;

        /// <summary>
        /// Probability
        /// 
        /// These represent the normal distribution of ideal game length.
        /// Tweak to generate games of different length.
        /// </summary>
        public static double Mu = 15;
        public static double Sigma = 3;

        // Rule based AI
        public static bool FastActionGeneration = false;
        // TODO - porovnat, co kdyz dovolim utocit na dead cile
        public static bool AllowCorpseTargetting = false;

        /// <summary>
        /// Disabling this will only generate the respective MOVE action,
        /// instead of a combined ATTACK-MOVE.
        /// 
        /// TODO - profile/benchmark both cases and create pretty graphs :)
        /// </summary>
        public static bool AttackMoveEnabled = true;

        /// <summary>
        /// Triggers attack move generation even when a direct attack was found.
        /// 
        /// // TODO - again experiment with triggering this
        /// </summary>
        public static bool AlwaysAttackMove = false;

        /// <summary>
        /// If `true` END-TURN actions will be generated even if there are enough
        /// other actions.
        /// 
        /// // TODO - again experiment with triggering this
        /// </summary>
        public static bool EndTurnAsLastResort = true;

        // Evolution
        public static bool AlwaysJumpToBetter = false;
        public static bool SaveGoodOnes = true;
        public static double InitialT = 100;
        public static int NumGenerations = 100000;
        public static int ExtraIterations = 10;
        public static int TeamsPerGeneration = 1;
        public static bool Logging = false;

        // TODO - fuj, to sem nepatri
        private static StringWriter LogBuffer = new StringWriter();

        public static StringWriter GetLogBuffer() {
            return LogBuffer;
        }

        public static void ResetLogBuffer() {
            if (Logging) {
                LogBuffer = new StringWriter();
            }
        }


        public static void WriteLogLine(object str) {
            if (Logging) {
                LogBuffer.WriteLine(str);
            }
        }

        public static int HpMax = 100;
        public static int ApMax = 25;
        public static int DmgMax = 30;
        public static int CostMax = 20;
        public static int RangeMax = 10;
        public static int EvolutionMapSize = 5;

        public static readonly string SaveFile = @"evo-save.txt";
        public static readonly string SaveDir = "save-files/";

        public static string BuildEvoSavePath(int index) {
            return SaveDir + index.ToString() + SaveFile;
        }
    }
}
