using System.IO;

namespace HexMage.Simulator {
    public static class Constants {
        // Benchmark controls
        public const bool MctsBenchmark = true;
        public const bool EvaluateAis = false;

        // Game Evaluation
        public const int MaxPlayoutEvaluationIterations = 100;

        // MCTS
        /// <summary>
        /// When enabled, UCB reward will be scaled by the HP percentage left.
        /// 
        /// TODO - experiment
        /// </summary>
        public const bool UseHpPercentageScaling = true;

        /// <summary>
        /// Probability
        /// 
        /// These represent the normal distribution of ideal game length.
        /// Tweak to generate games of different length.
        /// </summary>
        public const double Mu = 15;
        public const double Sigma = 3;

        // Rule based AI
        public const bool FastActionGeneration = false;
        // TODO - porovnat, co kdyz dovolim utocit na dead cile
        public const bool AllowCorpseTargetting = false;

        /// <summary>
        /// Disabling this will only generate the respective MOVE action,
        /// instead of a combined ATTACK-MOVE.
        /// 
        /// TODO - profile/benchmark both cases and create pretty graphs :)
        /// </summary>
        public const bool AttackMoveEnabled = true;

        /// <summary>
        /// Triggers attack move generation even when a direct attack was found.
        /// 
        /// // TODO - again experiment with triggering this
        /// </summary>
        public const bool AlwaysAttackMove = false;

        /// <summary>
        /// If `true` END-TURN actions will be generated even if there are enough
        /// other actions.
        /// 
        /// // TODO - again experiment with triggering this
        /// </summary>
        public const bool EndTurnAsLastResort = true;

        // Evolution
        public const bool AlwaysJumpToBetter = false;
        public const bool SaveGoodOnes = true;
        public const double InitialT = 100;
        public const int NumGenerations = 100000;
        public const int TeamsPerGeneration = 1;
        public const bool Logging = false;

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

        public const int HpMax = 100;
        public const int ApMax = 25;
        public const int DmgMax = 30;
        public const int CostMax = 20;
        public const int RangeMax = 10;
        public const int EvolutionMapSize = 5;

        public static readonly string SaveFile = @"evo-save.txt";
        public static readonly string SaveDir = "save-files/";

        public static string BuildEvoSavePath(int index) {
            return SaveDir + index.ToString() + SaveFile;
        }
    }
}