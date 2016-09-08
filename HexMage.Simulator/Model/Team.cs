using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public int Id { get; set; }
        public TeamColor Color { get; private set; }
        public List<Mob> Mobs { get; set; } = new List<Mob>();
        public IPlayer Player { get; set; }
        public IMobController Controller { get; set; }

        public Team(TeamColor color, IMobController controller) {
            Color = color;
            Controller = controller;
        }
    }

    public enum DefenseDesire {
        Block,
        Pass
    }

    public interface IMobController {
        Task<DefenseDesire> RequestDesireToDefend(Mob mob, Ability ability);
    }
}