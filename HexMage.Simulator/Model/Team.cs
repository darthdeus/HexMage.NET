using System;
using System.Collections.Generic;

namespace HexMage.Simulator
{
    public enum TeamColor
    {
        Red,
        Blue,
        Green
    }
    public class Team
    {
        public int ID { get; set; }
        public TeamColor Color { get; private set; }
        public List<Mob> Mobs { get; set; } = new List<Mob>();
        public IPlayer Player { get; set; }

        public Team(TeamColor color) {
            Color = color;
        }
    }
}