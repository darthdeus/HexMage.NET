using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public static class GameSetup {
        public static GameInstance GenerateForDnaSettings(int mobCount, int abilityCount) {
            var game = new GameInstance(Constants.EvolutionMapSize);

            var dna = new DNA(mobCount, abilityCount);

            UnpackTeamsIntoGame(game, dna, dna);
            game.PrepareEverything();

            return game;
        }

        public static GameInstance GenerateFromDna(DNA d1, DNA d2) {
            var game = GenerateForDnaSettings(d1.MobCount, d1.AbilityCount);

            OverrideGameDna(game, d1, d2);

            return game;
        }

        public static void OverrideGameDna(GameInstance game, DNA d1, DNA d2) {
            game.MobManager.Clear();
            game.State.Clear();

            UnpackTeamsIntoGame(game, d1, d2);

            game.PrepareTurnOrder();
            ResetGameAndPositions(game);
        }

        public static void ResetGameAndPositions(GameInstance game) {
            game.State.Reset(game.MobManager);
            ResetPositions(game);
        }

        public static void ResetPositions(GameInstance game) {
            int x = 0;
            // TODO: vratit zpatky
            //int y = game.Size - 1;
            int y = 2;
            var mobIds = game.MobManager.Mobs;
            game.PlaceMob(mobIds[0], new AxialCoord(x, y));
            game.PlaceMob(mobIds[1], new AxialCoord(y, x));

            if (mobIds.Count > 2) {
                game.PlaceMob(mobIds[2], new AxialCoord(-x, -y));
            }
            if (mobIds.Count > 3) {
                game.PlaceMob(mobIds[3], new AxialCoord(-y, -x));

            }
        }

        private static void UnpackTeamsIntoGame(GameInstance game, DNA team1, DNA team2) {
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