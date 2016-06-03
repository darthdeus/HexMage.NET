using System.Collections.Generic;

namespace HexMage.Simulator
{
    public class Team
    {
        public int ID { get; set; }
        public Color Color { get; set; }
        public List<Mob> Mobs { get; set; } = new List<Mob>();
        public IPlayer Player { get; set; }
    }
}