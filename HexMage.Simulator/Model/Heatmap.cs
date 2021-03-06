namespace HexMage.Simulator.Model {
    /// <summary>
    /// A simple heatmap with visiblity of all enemies calculated for each hex in the arena.
    /// </summary>
    public class Heatmap {
        public readonly HexMap<int> Map;

        public int MaxValue;
        public int MinValue;

        private Heatmap(int size) {
            Map = new HexMap<int>(size);
            MaxValue = 0;
            MinValue = int.MaxValue;
        }

        public static Heatmap BuildHeatmap(GameInstance game, int? chosenMob = null, bool ignoreAp = false) {
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

                    if (chosenMob.HasValue && chosenMob.Value != mobId) continue;

                    bool isFriendly = playerTeam == enemyInfo.Team;
                    bool isVisible = game.Map.IsVisible(enemyInstance.Coord, coord);

                    if (!isVisible) continue;
                    // We skip friendly mobs only when not focusing on a particular mob
                    if (!chosenMob.HasValue && isFriendly) continue;

                    int maxAbilityDmg = 0;
                    foreach (var abilityId in enemyInfo.Abilities) {
                        var abilityInfo = game.MobManager.Abilities[abilityId];

                        bool withinRange = game.Map.AxialDistance(enemyInstance.Coord, coord) <= abilityInfo.Range;
                        bool onCooldown = game.State.Cooldowns[abilityId] > 0;
                        bool hasEnoughAp = abilityInfo.Cost <= enemyInstance.Ap || ignoreAp;

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