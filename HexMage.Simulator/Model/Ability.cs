namespace HexMage.Simulator
{
    public class Ability
    {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }

        public Ability(int dmg, int cost, int range) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
        }
    }
}