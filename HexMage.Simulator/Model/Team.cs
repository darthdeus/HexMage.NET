using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.Model;

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
        private readonly MobManager _mobManager;
        public TeamColor Color { get; private set; }
        public IEnumerable<Mob> Mobs => _mobManager.Mobs.Where(mob => mob.Team == this);
        public IMobController Controller { get; set; }

        public Team(TeamColor color, IMobController controller, MobManager mobManager) {
            _mobManager = mobManager;
            Color = color;
            Controller = controller;
        }

        public Team DeepCopy(MobManager mobManagerCopy) {
            return new Team(Color, Controller, mobManagerCopy);
        }
    }

    public enum DefenseDesire {
        Block,
        Pass
    }
}