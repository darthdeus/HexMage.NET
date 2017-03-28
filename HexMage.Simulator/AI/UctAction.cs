using System;

namespace HexMage.Simulator {
    public struct UctAction {
        public readonly UctActionType Type;
        public readonly int AbilityId;
        public readonly int MobId;
        public readonly int TargetId;
        public readonly AxialCoord Coord;

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
                case UctActionType.DefensiveMove:
                    return $"D[{MobId}]:{Coord}";
                default:
                    throw new InvalidOperationException($"Invalid value of ${Type}");
            }
        }
    }
}