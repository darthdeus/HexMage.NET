using System;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using HexMage.Simulator.PCG;

namespace HexMage.Simulator.AI {
    /// <summary>
    /// Contains helpers for generating initial encounter setups.
    /// </summary>
    public static class GameSetup {
        /// <summary>
        /// Generates a game for the given DNA constant settings and a given map.
        /// Can also optionally precompute everything.
        /// </summary>
        public static GameInstance GenerateForDnaSettings(int mobCount, int abilityCount, Map map = null,
                                                          bool prepare = true) {
            if (map == null) {
                map = new Map(Constants.EvolutionMapSize);
                map.PrecomputeCubeLinedraw();
            }

            var game = new GameInstance(map);

            var dna = new DNA(mobCount, abilityCount);

            UnpackTeamsIntoGame(game, dna, dna);
            if (prepare) {
                game.PrepareEverything();
            }

            return game;
        }

        public static GameInstance GenerateFromDna(DNA d1, DNA d2, Map map = null, bool prepare = true) {
            var game = GenerateForDnaSettings(d1.MobCount, d1.AbilityCount, map, prepare);

            OverrideGameDna(game, d1, d2, prepare);

            return game;
        }

        /// <summary>
        /// Takes a game and overrides its settings based on a given DNA pair.
        /// </summary>
        public static void OverrideGameDna(GameInstance game, DNA d1, DNA d2, bool prepare = true) {
            game.MobManager.Clear();
            game.State.Clear();

            UnpackTeamsIntoGame(game, d1, d2);

            if (prepare) {
                game.PrepareTurnOrder();
                ResetGameAndPositions(game);
            }
        }

        /// <summary>
        /// Resets the game to an initial state as well as resetting the mobs to their
        /// starting positions.
        /// </summary>
        public static void ResetGameAndPositions(GameInstance game) {
            game.Reset();
            ResetPositions(game);
        }

        /// <summary>
        /// Resets the mobs to their starting positions.
        /// </summary>
        public static void ResetPositions(GameInstance game) {
            int redCursor = 0;
            int blueCursor = 0;

            var redPositions = game.Map.RedStartingPoints;
            var bluePositions = game.Map.BlueStartingPoints;

            foreach (var mobId in game.MobManager.Mobs) {
                var mobInfo = game.MobManager.MobInfos[mobId];
                bool placed = false;

                if (mobInfo.Team == TeamColor.Red) {
                    if (redCursor < redPositions.Count) {
                        placed = true;
                        game.PlaceMob(mobId, redPositions[redCursor]);
                        redCursor++;
                    }
                } else {
                    if (blueCursor < bluePositions.Count) {
                        placed = true;
                        game.PlaceMob(mobId, bluePositions[blueCursor]);
                        blueCursor++;
                    }
                }

                if (!placed) {
                    Utils.Log(LogSeverity.Error, nameof(GameSetup),
                              $"Ran out of placeholders for {mobInfo.Team}, placing randomly.");
                    Generator.RandomPlaceMob(game, mobId);
                }
            }
        }

        /// <summary>
        /// Takes DNAs for both teams as arguments and de-serializes them into the provided <code>GameInstance</code>
        /// object. Note that both teams must be of the same size.
        /// </summary>
        public static void UnpackTeamsIntoGame(GameInstance game, DNA team1, DNA team2) {
            Debug.Assert(team1.Data.Count == team2.Data.Count);
            var red = team1.ToTeam();
            var blue = team2.ToTeam();

            foreach (var mob in red.mobs) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Red, ids));
            }

            foreach (var mob in blue.mobs) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Blue, ids));
            }
        }
    }
}