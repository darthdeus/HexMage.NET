using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public static class GameSetup {
        public static GameInstance PrepareForSettings(int mobCount, int abilityCount) {
            var game = new GameInstance(Constants.EvolutionMapSize);

            var dna = new DNA(mobCount, abilityCount);

            UnpackTeamsIntoGame(game, dna, dna);
            game.PrepareEverything();

            return game;
        }

        public static GameInstance FromDNAs(DNA d1, DNA d2) {
            var game = PrepareForSettings(d1.MobCount, d1.AbilityCount);

            OverrideGameDNA(game, d1, d2);

            return game;
        }

        public static void OverrideGameDNA(GameInstance game, DNA d1, DNA d2) {
            game.MobManager.Clear();
            game.State.Clear();

            UnpackTeamsIntoGame(game, d1, d2);

            game.PrepareTurnOrder();
            ResetGameAndPositions(game);
        }

        private static void ResetGameAndPositions(GameInstance game) {
            game.State.Reset(game.MobManager);
            ResetPositions(game);
        }

        private static void ResetPositions(GameInstance game) {
            int x = 0;
            int y = game.Size - 1;
            var mobIds = game.MobManager.Mobs;
            game.State.SetMobPosition(mobIds[0], new AxialCoord(x, y));
            game.State.SetMobPosition(mobIds[1], new AxialCoord(y, x));

            game.State.SetMobPosition(mobIds[2], new AxialCoord(-x, -y));
            game.State.SetMobPosition(mobIds[3], new AxialCoord(-y, -x));
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