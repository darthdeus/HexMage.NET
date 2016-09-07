using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public class Buff {
        public AbilityElement Element { get; set; }
        public int HpChange { get; set; }
        public int ApChange { get; set; }
        public int Lifetime { get; set; }
        public float MoveSpeedModifier { get; set; }
        public List<AbilityElement> DisabledElements { get; set; }

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime) :
            this(element, hpChange, apChange, lifetime, 1, new List<AbilityElement>()) {}

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime, float moveSpeedModifier) :
            this(element, hpChange, apChange, lifetime, moveSpeedModifier, new List<AbilityElement>()) {}

        public Buff(AbilityElement element, int hpChange, int apChange, int lifetime, float moveSpeedModifier,
                    List<AbilityElement> disabledElements) {
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

        public override string ToString() {
            return
                $"{nameof(Element)}: {Element}, {nameof(HpChange)}: {HpChange}, {nameof(ApChange)}: {ApChange}, {nameof(Lifetime)}: {Lifetime}, {nameof(MoveSpeedModifier)}: {MoveSpeedModifier}";
        }
    }

    public class Mob {
        public static readonly int NumberOfAbilities = 6;

        private static int _lastId = 0;
        public int Id { get; set; }

        public int Hp { get; set; }
        public int Ap { get; set; }
        public int MaxHp { get; set; }
        public int MaxAp { get; set; }
        public int DefenseCost { get; set; }
        public int Iniciative { get; set; }

        public List<Ability> Abilities { get; set; }
        public Team Team { get; set; }
        public AxialCoord Coord { get; set; }
        public static int AbilityCount => 6;
        public object Metadata { get; set; }
        // TODO - should this maybe just be internal?
        public List<Buff> Buffs { get; set; } = new List<Buff>();

        public Mob(Team team, int maxHp, int maxAp, int defenseCost, int iniciative, List<Ability> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            DefenseCost = defenseCost;
            Iniciative = iniciative;
            Abilities = abilities;
            Hp = maxHp;
            Ap = maxAp;
            Coord = new AxialCoord(0, 0);
            Id = _lastId++;

            team.Mobs.Add(this);
        }

        public override string ToString() {
            return $"{Hp}/{MaxHp} {Ap}/{MaxAp}";
        }

        public float SpeedModifier => Buffs.Select(b => b.MoveSpeedModifier)
                                           .Aggregate(1.0f, (a, m) => a*(1/m));

        public int ModifiedDistance(int distance) {
            return (int) Math.Round(distance*SpeedModifier);
        }
    }
}