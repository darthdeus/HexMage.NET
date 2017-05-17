using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HexMage.Simulator.Model;
using MathNet.Numerics.Distributions;
using Newtonsoft.Json.Serialization;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Contains helpers for evaluating the results of an encounter
    /// in various different ways.
    /// </summary>
    public class GameEvaluator {
        private readonly GameInstance _game;
        private readonly TextWriter _writer;

        public GameEvaluator(GameInstance game, TextWriter writer) {
            _game = game;
            _writer = writer;
        }

        public static readonly List<IAiFactory> GlobalFactories = new List<IAiFactory>();

        public PlayoutResult Evaluate() {
            if (GlobalFactories.Count == 0) {
                GlobalFactories.Add(new RuleBasedFactory());
                GlobalFactories.Add(new MctsFactory(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var playoutResults = GlobalFactories.Select(factory => {
                                                    var game = _game.CopyStateOnly();
                                                    var ai = factory.Build(game);
                                                    return Playout(game, ai, ai);
                                                })
                                                .ToList();

            stopwatch.Stop();

            var result = playoutResults.Skip(1)
                                       .Aggregate(playoutResults.First(), (total, curr) => {
                                           return new PlayoutResult(total.TotalTurns + curr.TotalTurns,
                                                                    total.HpPercentage + curr.HpPercentage,
                                                                    total.AllPlayed && curr.AllPlayed,
                                                                    total.Timeout || curr.Timeout,
                                                                    total.RedWins + curr.RedWins,
                                                                    total.BlueWins + curr.BlueWins);
                                       });

            result.HpPercentage /= playoutResults.Count;
            result.TotalTurns /= playoutResults.Count;

            return result;
        }

        /// <summary>
        /// Runs a playout with two given controllers and reports the result.
        /// </summary>
        public static PlayoutResult Playout(GameInstance game, IMobController ai1, IMobController ai2) {
            var hub = new GameEventHub(game);

            game.MobManager.Teams[TeamColor.Red] = ai1;
            game.MobManager.Teams[TeamColor.Blue] = ai2;

            const int maxIterations = 100;
            int i = 0;

            for (; i < maxIterations && !game.IsFinished; i++) {
                game.CurrentController.FastPlayTurn(hub);
                ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            }

            float totalMaxHp = 0;
            float totalCurrentHp = 0;

            foreach (var mobId in game.MobManager.Mobs) {
                totalMaxHp += game.MobManager.MobInfos[mobId].MaxHp;
                totalCurrentHp += Math.Max(0, game.State.MobInstances[mobId].Hp);
            }

            int red = 0;
            int blue = 0;

            Utils.Log(LogSeverity.Error, nameof(GameEvaluator),
                      $"Playout time limit reached at {maxIterations} rounds");

            if (i < maxIterations && game.VictoryTeam.HasValue) {
                if (game.VictoryTeam.Value == TeamColor.Red) {
                    red++;
                } else {
                    blue++;
                }

                Accounting.IncrementWinner(game.VictoryController);
            }


            var gamePercentage = totalCurrentHp / totalMaxHp;
            Debug.Assert(gamePercentage >= 0);

            var mobsCount = game.MobManager.Mobs.Count;

            var dis = new Normal(mobsCount * 2, mobsCount);
            dis.Density(mobsCount * 2);

            return new PlayoutResult(i, gamePercentage, game.State.AllPlayed, i == maxIterations, red, blue);
        }

        /// <summary>
        /// Returns the win percentage of the first controller
        /// </summary>
        public static double CompareAiControllers(GameInstance game, List<DNA> dnas, IMobController c1,
                                                  IMobController c2) {
            game.AssignAiControllers(c1, c2);
            int redWins = 0;
            int totalGames = 0;

            foreach (var dna in dnas) {
                GameSetup.OverrideGameDna(game, dna, dna);

                GameSetup.ResetGameAndPositions(game);
                var r1 = Playout(game, c1, c2);

                GameSetup.ResetGameAndPositions(game);
                var r2 = Playout(game, c2, c1);

                redWins += r1.RedWins;
                redWins += r2.BlueWins;

                Debug.Assert(r1.RedWins + r1.BlueWins <= 1);
                Debug.Assert(r2.RedWins + r2.BlueWins <= 1);

                totalGames += 2;
            }

            return (double) redWins / (double) totalGames;
        }

        /// <summary>
        /// Runs a playout with the given encounter defined by a DNA pair and both controllers.
        /// </summary>
        public static int Playout(GameInstance game, DNA d1, DNA d2, IMobController c1, IMobController c2) {
            GameSetup.OverrideGameDna(game, d1, d2);

            game.AssignAiControllers(c1, c2);

            int iterations = Constants.MaxPlayoutEvaluationIterations;

            var hub = new GameEventHub(game);

            while (!game.IsFinished && iterations-- > 0) {
                game.CurrentController.FastPlayTurn(hub);
                ActionEvaluator.FNoCopy(game, UctAction.EndTurnAction());
            }

            if (Constants.GetLogBuffer().ToString().Length != 0) {
                Console.WriteLine(Constants.GetLogBuffer());
            }
            Constants.ResetLogBuffer();

            return Constants.MaxPlayoutEvaluationIterations - iterations;
        }
    }
}