using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator.AI;

namespace HexMage.Simulator.Model {
    public static class ActionEvaluator {
        public static int Actions = 0;

        // TODO: extract all the accounting
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

            switch (action.Type) {
                case UctActionType.Null:
                    // do nothing
                    break;
                case UctActionType.EndTurn:
                    state.NextMobOrNewTurn();
                    break;
                case UctActionType.AbilityUse:
                    AssertValidAbilityUseAction(state, action);

                    state.FastUse(action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.AttackMove:
                    AssertValidMoveAction(state, action);
                    AssertValidAbilityUseAction(state, action);

                    Debug.Assert(state.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    state.FastMove(action.MobId, action.Coord);
                    state.FastUse(action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.DefensiveMove:
                case UctActionType.Move:
                    AssertValidMoveAction(state, action);

                    // TODO - gameinstance co se jmenuje state?
                    //Debug.Assert(state.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    state.FastMove(action.MobId, action.Coord);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value of {action.Type}");
            }

            return state;
        }

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
            AssertAndRecord(game, abilityInfo.Range >= mobInstance.Coord.Distance(targetInstance.Coord), "abilityInfo.Range >= mobInstance.Coord.Distance(targetInstance.Coord)");
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
    }

    public class InvariantViolationException : Exception {
        public InvariantViolationException() { }
        public InvariantViolationException(string message) : base(message) { }
        public InvariantViolationException(string message, Exception innerException) : base(message, innerException) { }
        protected InvariantViolationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}