using System.IO;

namespace HexMage.Simulator {
    public static class Constants {
        /// <summary>
        /// When set to <code>true</code> it will enable the sound effects. Note that OpenAL compatible
        /// drivers must be installed, otherwise playing the sound effects will crash with an internal error.
        /// </summary>
        public static bool EnableSounds = false;

        /// <summary>
        /// When set to <code>true</code> games' replays will be automatically recorded.
        /// See <code>ReplayRecorder</code> for more details.
        /// </summary>
        public static bool RecordReplays = false;

        // MCTS benchmarks
        /// <summary>
        /// The number of iterations of MCTS during the AI benchmark.
        /// </summary>
        public static int MctsBenchIterations = 100;

        /// <summary>
        /// Specifies the type of AIs playing against each other when running <code>CompareAIs</code>
        /// </summary>
        public static int MctsBenchType = 2;

        /// <summary>
        /// When set to <code>true</code> evolution will generate a resulting plot using GNU Plot.
        /// </summary>
        public static bool GnuPlot = false;

        public static bool MeasureSearchSpaceStats = false;
        /// <summary>
        /// Determines the number of samples measured in the serach space.
        /// </summary>
        public static int MeasureSamples = 1000000;
        /// <summary>
        /// Determines the number of neighbours measured for each sample.
        /// </summary>
        public static int MeasureNeighboursPerSample = 10;

        // Benchmark controls
        public static bool MctsBenchmark = false;
        public static bool EvaluateAis = false;

        // Game Evaluation
        public static int MaxPlayoutEvaluationIterations = 1;

        /// <summary>
        /// Enables logging of MCTS progress.
        /// </summary>
        public static bool MctsLogging = false;

        /// <summary>
        /// Probability
        ///
        /// These represent the normal distribution of ideal game length.
        /// Tweak to generate games of different length.
        /// </summary>
        public static double Mu = 10;
        public static double Sigma = 3;

        /// <summary>
        /// Determines which ruleset the Rule based AI uses for generating tis actions.
        /// </summary>
        public static bool FastActionGeneration = false;

        /// <summary>
        /// Disabling this will only generate the respective MOVE action,
        /// instead of a combined ATTACK-MOVE.
        /// </summary>
        public static bool AttackMoveEnabled = true;

        /// <summary>
        /// Triggers attack move generation even when a direct attack was found.
        /// </summary>
        public static bool AlwaysAttackMove = false;

        /// <summary>
        /// If `true` END-TURN actions will be generated even if there are enough
        /// other actions.
        /// </summary>
        public static bool EndTurnAsLastResort = true;

        // Evolution
        /// <summary>
        /// Enables restarting when the fitness lowers below a given threshold
        /// </summary>
        public static bool RestartFailures = true;

        /// <summary>
        /// Sets the fitness threshold for determining a bad result and restarts.
        /// </summary>
        public static double FitnessThreshold = 0.1;

        /// <summary>
        /// Sets the initial temperature for Simulated Annealing.
        /// </summary>
        public static float InitialT = 0.5f;

        /// <summary>
        /// Determines whether the results with high enough fitness value are saved.
        /// </summary>
        public static bool SaveGoodOnes = true;
        public static bool Logging = false;

        /// <summary>
        /// Switches Simulated Annealing over to hill climbing when enabled.
        /// </summary>
        public static bool HillClimbing = false;

        /// <summary>
        /// Determines the number of iterations of ES/SA.
        /// </summary>
        public static int NumGenerations = 20000;
        /// <summary>
        /// Determines how often are the intermediate results printed.
        /// </summary>
        public static int EvolutionPrintModulo = 10;
        /// <summary>
        /// Determines the number of neighbours ES looks at.
        /// </summary>
        public static int TeamsPerGeneration = 40;
        /// <summary>
        /// Determines the maximum size of a given mutation.
        /// </summary>
        public static double MutationDelta = 0.25;

        /// <summary>
        /// Take game length into account when evaluating the fitness function
        /// </summary>
        public static bool FitnessGameLength = false;

        /// <summary>
        /// Determines the probability of consequent mutations.
        /// </summary>
        public static double SecondMutationProb = 0.35f;

        /// <summary>
        /// When enabled, the intermediate results will print the difference between fitness values.
        /// </summary>
        public static bool PrintFitnessDiff = true;

        /// <summary>
        /// Misc
        /// </summary>        
        public static int RandomSeed = 12345;

        /// <summary>
        /// Enables the global rnadom seed.
        /// </summary>
        public static bool UseGlobalSeed = true;

        #region DNA constants
        
        public static int HpMax = 100;
        public static int ApMax = 25;
        public static int DmgMax = 30;
        public static int CostMax = 20;
        public static int CooldownMax = 2;
        public static int RangeMax = 10;
        public static int EvolutionMapSize = 5;
        public static int ElementCount = 4;

        public static int BuffDmgMax = 5;
        public static int BuffApDmgMax = 5;
        public static int BuffLifetimeMax = 3;
        public static int BuffMaxRadius = 5;

        #endregion

        #region Logging

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

        public static readonly string SaveFile = @"evo-save.txt";
        public static readonly string SaveDir = "data/save-files/";

        public static string BuildEvoSavePath(int index) {
            return SaveDir + index.ToString() + SaveFile;
        }

        #endregion Logging
    }
}