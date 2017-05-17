using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.AI;

namespace HexMage.Simulator.Model {
    /// <summary>
    /// Encapsulates the logic of evaluating actions on a given game state.
    /// </summary>
    public static class ActionEvaluator {
        public static int Actions = 0;

        public static readonly Dictionary<UctActionType, int> ActionCounts = new Dictionary<UctActionType, int>();

        public static string ActionCountString() {
            return
                $"E: {ActionCounts[UctActionType.EndTurn]}, " +
                $"A: {ActionCounts[UctActionType.AbilityUse]}, " +
                $"M: {ActionCounts[UctActionType.Move]}, " +
                $"N: {ActionCounts[UctActionType.Null]}, " +
                $"D: {ActionCounts[UctActionType.DefensiveMove]}, " +
                $"AM: {ActionCounts[UctActionType.AttackMove]}";
        }

        static ActionEvaluator() {
            ActionCounts.Add(UctActionType.Null, 0);
            ActionCounts.Add(UctActionType.EndTurn, 0);
            ActionCounts.Add(UctActionType.AbilityUse, 0);
            ActionCounts.Add(UctActionType.Move, 0);
            ActionCounts.Add(UctActionType.DefensiveMove, 0);
            ActionCounts.Add(UctActionType.AttackMove, 0);
        }

        public static GameInstance F(GameInstance state, UctAction action) {
            return FNoCopy(state.CopyStateOnly(), action);
        }

        public static GameInstance FNoCopy(GameInstance state, UctAction action) {
            Actions++;
            ActionCounts[action.Type]++;

            Constants.WriteLogLine(action);

            if (Constants.RecordReplays) {
                ReplayRecorder.Instance.Actions.Add(action);
            }

            GameInvariants.AssertValidAction(state, action);

            switch (action.Type) {
                case UctActionType.Null:
                    // do nothing
                    break;
                case UctActionType.EndTurn:
                    state.State.LastTeamColor = state.CurrentTeam;
                    state.TurnManager.NextMobOrNewTurn();
                    break;
                case UctActionType.AbilityUse:
                    FastUse(state, action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.AttackMove:
                    FastMove(state, action.MobId, action.Coord);
                    FastUse(state, action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.Move:
                case UctActionType.DefensiveMove:
                    FastMove(state, action.MobId, action.Coord);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value of {action.Type}");
            }

            return state;
        }

        private static void FastMove(GameInstance game, int mobId, AxialCoord coord) {
            var mobInstance = game.State.MobInstances[mobId];

            int distance = game.Pathfinder.Distance(mobInstance.Coord, coord);

            game.State.ChangeMobAp(mobId, -distance);
            game.State.SetMobPosition(mobId, coord);
        }

        private static void FastUse(GameInstance game, int abilityId, int mobId, int targetId) {
            var ability = game.MobManager.AbilityForId(abilityId);

            game.State.Cooldowns[abilityId] = ability.Cooldown;

            TargetHit(game, abilityId, mobId, targetId);
        }

        private static void TargetHit(GameInstance game, int abilityId, int mobId, int targetId) {
            var ability = game.MobManager.AbilityForId(abilityId);

            Debug.Assert(ability.Dmg > 0);
            game.State.ChangeMobHp(game, targetId, -ability.Dmg);

            var targetInstance = game.State.MobInstances[targetId];

            if (!ability.Buff.IsZero) {
                targetInstance.Buff = Buff.Combine(targetInstance.Buff, ability.Buff);
            }

            game.State.MobInstances[targetId] = targetInstance;

            if (!ability.AreaBuff.IsZero) {
                var copy = ability.AreaBuff;
                copy.Coord = targetInstance.Coord;

                game.State.AreaBuffs.Add(copy);
            }

            if (game.State.MobInstances[mobId].Ap < ability.Cost) {
                ReplayRecorder.Instance.SaveAndClear(game, 0);
                throw new InvalidOperationException("Trying to use an ability with not enough AP.");
            }
            Debug.Assert(game.State.MobInstances[mobId].Ap >= ability.Cost,
                         "State.MobInstances[mobId].Ap >= ability.Cost");

            game.State.ChangeMobAp(mobId, -ability.Cost);
        }
    }
}