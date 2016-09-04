﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HexMage.Simulator
{
    public class Buff {
        public AbilityElement Element { get; set; }
        public int HpChange { get; set; }
        public int ApChange { get; set; }
        public int Lifetime { get; set; }
        public float MoveSpeedModifier { get; set; }
        public List<AbilityElement> DisabledElements { get; set; }

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime):
            this(element, hpChange, apChange, lifetime, 1, new List<AbilityElement>()){
        }

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime, float moveSpeedModifier) :
          this(element, hpChange, apChange, lifetime, moveSpeedModifier, new List<AbilityElement>())
        {
        }

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime, float moveSpeedModifier, List<AbilityElement> disabledElements) {
            Element = element;
            HpChange = hpChange;
            ApChange = apChange;
            Lifetime = lifetime;
            MoveSpeedModifier = moveSpeedModifier;
            DisabledElements = disabledElements;
        }

        // TODO - maybe replace with struct instead?
        [Obsolete]
        public Buff Clone() {
            return (Buff) MemberwiseClone();
        }
    }

    public class Mob
    {
        public static readonly int NumberOfAbilities = 6;

        private static int _lastId = 0;
        public int ID { get; set; }

        public int HP { get; set; }
        public int AP { get; set; }
        public int MaxHP { get; set; }
        public int MaxAP { get; set; }
        public int DefenseCost { get; set; }

        public List<Ability> Abilities { get; set; }
        public Team Team { get; set; }
        public AxialCoord Coord { get; set; }
        public static int AbilityCount => 6;
        public object Metadata { get; set; }
        // TODO - should this maybe just be internal?
        public List<Buff> Buffs { get; set; } = new List<Buff>();

        public Mob(Team team, int maxHp, int maxAp, int defenseCost, List<Ability> abilities) {
            Team = team;
            MaxHP = maxHp;
            MaxAP = maxAp;
            DefenseCost = defenseCost;
            Abilities = abilities;
            HP = maxHp;
            AP = maxAp;
            Coord = new AxialCoord(0, 0);
            ID = _lastId++;

            team.Mobs.Add(this);
        }

        public override string ToString() {
            return $"{HP}/{MaxHP} {AP}/{MaxAP}";
        }
    }
}