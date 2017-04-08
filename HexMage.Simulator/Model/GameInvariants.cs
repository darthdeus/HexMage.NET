using System;
using System.Diagnostics;
using HexMage.Simulator.AI;

namespace HexMage.Simulator.Model {
    public static class GameInvariants {
        [Conditional("DEBUG")]
        public static void AssertValidAbilityUseAction(GameInstance game, UctAction action) {
            var mobInstance = game.State.MobInstances[action.MobId];
            var targetInstance = game.State.MobInstances[action.TargetId];
            var abilityInfo = game.MobManager.Abilities[action.AbilityId];

            AssertAndRecord(game, mobInstance.Ap >= abilityInfo.Cost, "mobInstance.Ap >= abilityInfo.Cost");
            AssertAndRecord(game, mobInstance.Hp > 0, $"Using an ability with {mobInstance.Hp}HP");
            AssertAndRecord(game, targetInstance.Hp > 0, $"Using an ability on a target with {mobInstance.Hp}HP");

            var isVisible = game.Map.IsVisible(mobInstance.Coord, targetInstance.Coord);
            AssertAndRecord(game, isVisible, "Target is not visible");
            AssertAndRecord(game, abilityInfo.Range >= mobInstance.Coord.Distance(targetInstance.Coord),
                            "abilityInfo.Range >= mobInstance.Coord.Distance(targetInstance.Coord)");
        }

        [Conditional("DEBUG")]
        public static void AssertAndRecord(GameInstance game, bool condition, string message) {
            if (!condition) {
                ReplayRecorder.Instance.SaveAndClear(game, 0);

                throw new InvariantViolationException(message);
            }
        }

        [Conditional("DEBUG")]
        public static void AssertValidMoveAction(GameInstance game, UctAction action) {
            var atCoord = game.State.AtCoord(action.Coord);

            Debug.Assert(atCoord != action.MobId, "Trying to move into the coord you're already standing on.");
            Debug.Assert(atCoord == null, "Trying to move into a mob.");
        }

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