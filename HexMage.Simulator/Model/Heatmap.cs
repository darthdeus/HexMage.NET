using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class Heatmap {
        // TODO - make these properties
        public HexMap<int> Map;

        public int Size;
        public int MaxValue;
        public int MinValue;

        public Heatmap(int size) {
            Size = size;
            Map = new HexMap<int>(size);
            MaxValue = 0;
            MinValue = int.MaxValue;
        }

        public static Heatmap BuildHeatmap(GameInstance game, int? chosenMob = null) {
            var heatmap = new Heatmap(game.Size);

            int maxDmg = 0;
            int minDmg = int.MaxValue;

            if (!game.CurrentTeam.HasValue) return heatmap;

            TeamColor playerTeam = game.CurrentTeam.Value;

            foreach (var coord in heatmap.Map.AllCoords) {
                int coordValue = 0;
                foreach (var mobId in game.MobManager.Mobs) {
                    var enemyInfo = game.MobManager.MobInfos[mobId];
                    var enemyInstance = game.State.MobInstances[mobId];

                    // TODO - fuj
                    if (chosenMob.HasValue && chosenMob.Value != mobId) continue;

                    bool isFriendly = playerTeam == enemyInfo.Team;
                    bool isVisible = game.Map.IsVisible(enemyInstance.Coord, coord);

                    if (!isVisible) continue;
                    // We skip friendly mobs only when not focusing on a particular mob
                    if (!chosenMob.HasValue && isFriendly) continue;

                    int maxAbilityDmg = 0;
                    foreach (var abilityId in enemyInfo.Abilities) {
                        var abilityInfo = game.MobManager.AbilityForId(abilityId);

                        bool withinRange = game.Map.AxialDistance(enemyInstance.Coord, coord) <= abilityInfo.Range;
                        bool onCooldown = game.State.Cooldowns[abilityId] > 0;
                        bool hasEnoughAp = abilityInfo.Cost <= enemyInstance.Ap;

                        bool isAbilityUsable = withinRange && !onCooldown && hasEnoughAp;

                        if (isAbilityUsable && abilityInfo.Dmg > maxAbilityDmg) {
                            maxAbilityDmg = abilityInfo.Dmg;
                        }
                    }

                    coordValue += maxAbilityDmg;

                    if (coordValue < minDmg) minDmg = coordValue;
                    if (coordValue > maxDmg) maxDmg = coordValue;
                }

                heatmap.Map[coord] = coordValue;
            }

            heatmap.MinValue = minDmg;
            heatmap.MaxValue = maxDmg;

            return heatmap;
        }
    }
}