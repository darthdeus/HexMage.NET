using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator.AI {
    public class GameSetup {
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

            //for (int dnaSelection = 0; dnaSelection < 2; dnaSelection++) {
            //    Team genTeam = (dnaSelection == 0 ? d1 : d2).ToTeam();

            //    for (int dnaIndex = 0; dnaIndex < game.MobManager.Mobs.Count; dnaIndex++) {
            //        var mobId = game.MobManager.Mobs[dnaIndex + dnaSelection * d1.MobCount];

            //        var genMob = genTeam.mobs[dnaIndex];

            //        var mobInfo = game.MobManager.MobInfos[mobId];
            //        mobInfo.MaxHp = genMob.hp;
            //        mobInfo.MaxAp = genMob.ap;

            //        for (int abilityIndex = 0; abilityIndex < mobInfo.Abilities.Count; abilityIndex++) {
            //            int abilityId = mobInfo.Abilities[abilityIndex];
            //            var genAbility = genMob.abilities[abilityIndex];

            //            var abilityInfo = game.MobManager.Abilities[abilityId];
            //            abilityInfo.Dmg = genAbility.dmg;
            //            abilityInfo.Cost = genAbility.ap;
            //            abilityInfo.Range = genAbility.range;

            //            game.MobManager.Abilities[abilityId] = abilityInfo;
            //        }

            //        game.MobManager.MobInfos[mobId] = mobInfo;
            //    }
            //}

            game.PrepareTurnOrder();
            ResetGameAndPositions(game);
        }


        public static void ResetGameAndPositions(GameInstance game) {
            game.State.Reset(game.MobManager);
            ResetPositions(game);
        }

        public static void ResetPositions(GameInstance game) {
            int x = 0;
            int y = game.Size - 1;
            var mobIds = game.MobManager.Mobs;
            game.State.SetMobPosition(mobIds[0], new AxialCoord(x, y));
            game.State.SetMobPosition(mobIds[1], new AxialCoord(y, x));

            game.State.SetMobPosition(mobIds[2], new AxialCoord(-x, -y));
            game.State.SetMobPosition(mobIds[3], new AxialCoord(-y, -x));
        }

        public static void UnpackTeamsIntoGame(GameInstance game, DNA team1, DNA team2) {
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