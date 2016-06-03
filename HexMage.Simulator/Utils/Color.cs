namespace HexMage.Simulator
{
    public struct Color
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Color(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
}