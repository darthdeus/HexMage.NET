using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    /// <summary>
    /// A collection of math helpers.
    /// </summary>
    public static class Mathf {
        public static float Clamp(float min, float value, float max) {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double Clamp(double min, double value, double max) {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}