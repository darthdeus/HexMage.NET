using System.IO;

namespace HexMage.Simulator {
    public static class Constants {
        public static bool RecordReplays = false;

        // MCTS benchmarks
        public static int MctsBenchIterations = 200;
        public static int MctsBenchType = 1;

        public static bool GnuPlot = true;

        public static bool MeasureSearchSpaceStats = false;
        public static int MeasureSamples = 1000000;
        public static int MeasureNeighboursPerSample = 10;

        // Benchmark controls
        public static bool MctsBenchmark = false;
        public static bool EvaluateAis = false;

        // Game Evaluation
        public static int MaxPlayoutEvaluationIterations = 1;

        // MCTS
        /// <summary>
        /// When enabled, UCB reward will be scaled by the HP percentage left.
        /// 
        /// TODO - experiment
        /// </summary>
        public static bool UseHpPercentageScaling = false;

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
        public static double Mu = 10;
        public static double Sigma = 3;

        // Rule based AI
        public static bool FastActionGeneration = false;

        /// <summary>
        /// Disabling this will only generate the respective MOVE action,
        /// instead of a combined ATTACK-MOVE.
        /// 
        /// TODO - profile/benchmark both cases and create pretty graphs
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
        public static bool RestartFailures = true;
        public static double FitnessThreshold = 0.1;

        public static bool RandomizeInitialTeam = true;

        public static bool AverageHpTotals = true;
        public static bool SaveGoodOnes = true;
        public static float InitialT = 1;
        public static bool Logging = false;

        // TODO - check if disabling this helps
        public static bool ForbidTimeouts = true;

        public static bool HillClimbing = false;

        public static int NumGenerations = 10000;
        public static int EvolutionPrintModulo = 10;
        public static int TeamsPerGeneration = 40;
        public static double MutationDelta = 0.25;

        /// <summary>
        /// Take game length into account when evaluating the fitness function
        /// 
        /// Note that this doesn't converge at all, never goes above 0.5
        /// TODO: why?
        /// </summary>
        public static bool FitnessGameLength = false;

        public static double SecondMutationProb = 0.35f;

        public static bool PrintFitnessDiff = true;

        /// <summary>
        /// Misc
        /// </summary>        
        public static int RandomSeed = 12345;

        public static bool UseGlobalSeed = true;

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
        public static int ElementCount = 4;

        public static int BuffDmgMax = 5;
        public static int BuffApDmgMax = 5;
        public static int BuffLifetimeMax = 3;
        public static int BuffMaxRadius = 5;

        public static readonly string SaveFile = @"evo-save.txt";
        public static readonly string SaveDir = "data/save-files/";

        public static string BuildEvoSavePath(int index) {
            return SaveDir + index.ToString() + SaveFile;
        }
    }
}