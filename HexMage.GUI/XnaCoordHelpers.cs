using HexMage.Simulator;
using Microsoft.Xna.Framework;

namespace HexMage.GUI
{
    public static class XnaCoordHelpers
    {
        public static Vector3 ToVector3(this PixelCoord coord) {
            return new Vector3(coord.X, coord.Y, 0);
        }

        public static Point ToPoint(this PixelCoord coord) {
            return new Point(coord.X, coord.Y);
        }
    }
}