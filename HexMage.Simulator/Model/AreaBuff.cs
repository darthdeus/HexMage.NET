﻿using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class AreaBuff : IDeepCopyable<AreaBuff> {
        public AxialCoord Coord { get; set; }
        public int Radius { get; set; }
        public Buff Effect { get; set; }

        public AreaBuff(AxialCoord coord, int radius, Buff effect) {
            Coord = coord;
            Radius = radius;
            Effect = effect;
        }

        public AreaBuff DeepCopy() {
            return new AreaBuff(Coord, Radius, Effect.DeepCopy());
        }

        public override string ToString() {
            return $"{nameof(Radius)}: {Radius}, {nameof(Effect)}: {Effect}";
        }
    }
}