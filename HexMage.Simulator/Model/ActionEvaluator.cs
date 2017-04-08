using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                    GameInvariants.AssertValidAbilityUseAction(state, action);

                    state.FastUse(action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.AttackMove:
                    GameInvariants.AssertValidMoveAction(state, action);
                    GameInvariants.AssertValidAbilityUseAction(state, action);

                    // TODO - tohle uz tu neni potreba :)
                    Debug.Assert(state.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    state.FastMove(action.MobId, action.Coord);
                    state.FastUse(action.AbilityId, action.MobId, action.TargetId);
                    break;
                case UctActionType.DefensiveMove:
                case UctActionType.Move:
                    GameInvariants.AssertValidMoveAction(state, action);

                    // TODO - gameinstance co se jmenuje state?
                    //Debug.Assert(state.State.AtCoord(action.Coord) == null, "Trying to move into a mob.");
                    state.FastMove(action.MobId, action.Coord);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value of {action.Type}");
            }

            return state;
        }

    }
}