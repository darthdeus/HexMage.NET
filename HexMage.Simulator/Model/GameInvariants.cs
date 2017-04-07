using System;

namespace HexMage.Simulator.Model {
    public static class GameInvariants {
        public static bool IsAbilityUsableNoTarget(GameInstance game, int mobId, int abilityId) {
            var ability = game.MobManager.AbilityForId(abilityId);
            var mobInstance = game.State.MobInstances[mobId];

            bool enoughAp = mobInstance.Ap >= ability.Cost;
            bool noCooldown = game.State.Cooldowns[abilityId] == 0;

            return enoughAp && noCooldown;
        }

        public static bool CanMoveTo(GameInstance game, CachedMob mob, AxialCoord coord) {
            int remainingAp, distance;
            return CanMoveTo(game, mob, coord, out remainingAp, out distance);
        }

        public static bool CanMoveTo(GameInstance game, CachedMob mob, AxialCoord coord, out int remainingAp,
                                     out int distance) {
            bool isEmpty = game.Map[coord] == HexType.Empty && game.State.AtCoord(coord) == null;

            distance = game.Pathfinder.Distance(mob.MobInstance.Coord, coord);
            remainingAp = mob.MobInstance.Ap - distance;
            bool enoughAp = remainingAp >= 0;

            return isEmpty && enoughAp;
        }

        public static bool IsAbilityUsableAtCoord(CachedMob mob, AxialCoord coord, int abilityId) {
            throw new NotImplementedException();
        }

        public static bool IsAbilityUsableFrom(GameInstance game, CachedMob mob, AxialCoord from, CachedMob target,
                                               int abilityId) {
            var ability = game.MobManager.Abilities[abilityId];

            // TODO - kontrolovat i ze na to policko dojdu?
            bool withinRange = ability.Range >= from.Distance(target.MobInstance.Coord);
            bool enoughAp = mob.MobInstance.Ap >= ability.Cost;

            return withinRange && enoughAp;
        }

        public static bool IsTargetable(GameInstance game, CachedMob mob, CachedMob target,
                                        bool checkVisibility = true) {
            bool isVisible = !checkVisibility || game.Map.IsVisible(mob.MobInstance.Coord, target.MobInstance.Coord);

            bool isTargetAlive = Constants.AllowCorpseTargetting || target.MobInstance.Hp > 0;
            bool isEnemy = mob.MobInfo.Team != target.MobInfo.Team;

            return isVisible && isTargetAlive && isEnemy;
        }

        public static bool IsAbilityUsableApRangeCheck(GameInstance game, CachedMob mob, CachedMob target,
                                                       int abilityId) {
            var abilityInfo = game.MobManager.Abilities[abilityId];

            bool enoughAp = mob.MobInstance.Ap >= abilityInfo.Cost;
            bool withinRange = mob.MobInstance.Coord.Distance(target.MobInstance.Coord) <= abilityInfo.Range;

            return enoughAp && withinRange;
        }

        public static bool IsAbilityUsable(GameInstance game, CachedMob mob, CachedMob target, int abilityId) {
            return IsTargetable(game, mob, target) && IsAbilityUsableApRangeCheck(game, mob, target, abilityId);
        }
    }
}