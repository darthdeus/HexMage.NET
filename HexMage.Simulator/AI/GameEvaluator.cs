﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using HexMage.Simulator.Model;
using Newtonsoft.Json.Serialization;

namespace HexMage.Simulator.AI {
    public class GameEvaluator {
        private readonly GameInstance _gameInstance;
        private readonly TextWriter _writer;

        public GameEvaluator(GameInstance gameInstance, TextWriter writer) {
            _gameInstance = gameInstance;
            _writer = writer;
        }

        public static List<IAiFactory> GlobalFactories = new List<IAiFactory>();

        // TODO: wat, tohle dat pryc a pouzivat obecnejsi
        public PlayoutResult Evaluate() {
            if (GlobalFactories.Count == 0) {
                GlobalFactories.Add(new MctsFactory(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var playoutResults = GlobalFactories.Select(factory => {
                                                    var game = _gameInstance.CopyStateOnly();
                                                    var ai = factory.Build(game);
                                                    return Playout(game, ai, ai);
                                                })
                                                .ToList();

            stopwatch.Stop();

            var result = playoutResults.Skip(1)
                                       .Aggregate(playoutResults.First(), (total, curr) => {
                                           return new PlayoutResult(total.TotalTurns + curr.TotalTurns,
                                                                    total.Fitness + curr.Fitness,
                                                                    total.Timeout || curr.Timeout,
                                                                    total.RedWins + curr.RedWins,
                                                                    total.BlueWins + curr.BlueWins);
                                       });

            result.Fitness /= playoutResults.Count;

            return result;
        }


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

            if (game.VictoryTeam.HasValue) {
                if (game.VictoryTeam.Value == TeamColor.Red) {
                    red++;
                } else {
                    blue++;
                }
            }

            var gamePercentage = totalCurrentHp / totalMaxHp;
            Debug.Assert(gamePercentage >= 0);

            return new PlayoutResult(i, 1 - gamePercentage, i == maxIterations, red, blue);
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

        // TODO - na co tohle vlastne je?
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