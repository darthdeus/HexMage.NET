using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace HexMage.Simulator.AI {
    public struct UctAction {
        public readonly UctActionType Type;
        public readonly int AbilityId;
        public readonly int MobId;
        public readonly int TargetId;
        public readonly AxialCoord Coord;

        [JsonConstructor]
        private UctAction(UctActionType type, int abilityId, int mobId, int targetId, AxialCoord coord) {
            Type = type;
            AbilityId = abilityId;
            MobId = mobId;
            TargetId = targetId;
            Coord = coord;
        }

        public static UctAction NullAction() {
            return new UctAction(UctActionType.Null, -1, -1, -1, AxialCoord.Zero);
        }

        public static UctAction EndTurnAction() {
            return new UctAction(UctActionType.EndTurn, -1, -1, -1, AxialCoord.Zero);
        }

        public static UctAction AbilityUseAction(int abilityId, int mobId, int targetId) {
            return new UctAction(UctActionType.AbilityUse, abilityId, mobId, targetId, AxialCoord.Zero);
        }

        public static UctAction MoveAction(int mobId, AxialCoord coord) {
            return new UctAction(UctActionType.Move, -1, mobId, -1, coord);
        }

        public static UctAction DefensiveMoveAction(int mobId, AxialCoord coord) {
            return new UctAction(UctActionType.DefensiveMove, -1, mobId, -1, coord);
        }

        public static UctAction AttackMoveAction(int mobId, AxialCoord coord, int abilityId, int targetId) {
            return new UctAction(UctActionType.AttackMove, abilityId, mobId, targetId, coord);
        }

        public UctAction ToPureMove() {
            Debug.Assert(Type == UctActionType.DefensiveMove || Type == UctActionType.AttackMove || Type == UctActionType.Move);
            return MoveAction(MobId, Coord);
        }

        public UctAction ToPureAbilityUse() {
            Debug.Assert(Type == UctActionType.AttackMove || Type == UctActionType.AbilityUse);
            return AbilityUseAction(AbilityId, MobId, TargetId);
        }

        public override string ToString() {
            switch (Type) {
                case UctActionType.Null:
                    return $"NullAction";
                case UctActionType.EndTurn:
                    return $"End";
                case UctActionType.AbilityUse:
                    return $"A[{AbilityId}]:{TargetId}";
                case UctActionType.Move:
                    return $"M[{MobId}]:{Coord}";
                case UctActionType.AttackMove:
                    return $"AM[{Coord}]:{AbilityId}->{TargetId}";
                case UctActionType.DefensiveMove:
                    return $"D[{MobId}]:{Coord}";
                default:
                    throw new InvalidOperationException($"Invalid value of ${Type}");
            }
        }
    }
}