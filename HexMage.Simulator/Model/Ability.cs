namespace HexMage.Simulator
{
    public enum AbilityElement
    {
        Earth,
        Fire,
        Air,
        Water
    }

    public class Ability
    {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public AbilityElement Element { get; set; }

        public Ability(int dmg, int cost, int range, int cooldown, AbilityElement element) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
            Cooldown = cooldown;
            Element = element;
        }


    }
}