using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class JsonBuff {
        public int HpChange;
        public int ApChange;
        public int Lifetime;

        public JsonBuff() { }

        public JsonBuff(Buff buff) {
            HpChange = buff.HpChange;
            ApChange = buff.ApChange;
            Lifetime = buff.Lifetime;
        }

        public Buff ToBuff() {
            var res = new Buff(HpChange, ApChange, Lifetime);
            return res.IsZero ? Buff.ZeroBuff() : res;
        }
    }

    public class JsonAreaBuff {
        public AxialCoord Coord;
        public int Radius;
        public JsonBuff Effect;

        public JsonAreaBuff() {
        }

        public JsonAreaBuff(AreaBuff areaBuff) {
            Coord = areaBuff.Coord;
            Radius = areaBuff.Radius;
            Effect = new JsonBuff(areaBuff.Effect);
        }

        public AreaBuff ToBuff() {
            var res = new AreaBuff(Coord, Radius, Effect.ToBuff());
            return res.IsZero ? AreaBuff.ZeroBuff() : res;
        }
    }

    public class JsonAbility {
        public int dmg;
        public int ap;
        public int range;
        public int cooldown;
        public JsonBuff buff;
        public JsonAreaBuff areaBuff;

        public JsonAbility() { }

        public JsonAbility(int dmg, int ap, int range, int cooldown, JsonBuff buff, JsonAreaBuff areaBuff) {
            this.dmg = dmg;
            this.ap = ap;
            this.range = range;
            this.cooldown = cooldown;
            this.buff = buff;
            this.areaBuff = areaBuff;
        }

        public AbilityInfo ToAbility() {
            return new AbilityInfo(dmg, ap, range, cooldown, buff.ToBuff(), areaBuff.ToBuff());
        }
    }

    public class JsonMob {
        public int hp;
        public int ap;
        public List<JsonAbility> abilities = new List<JsonAbility>();

        public MobInfo ToMobInfo(TeamColor color, IEnumerable<int> abilityIds) {
            return new MobInfo(color, hp, ap, 0, abilityIds.ToList());
        }
    }

    public class Team {
        public List<JsonMob> mobs = new List<JsonMob>();

        public DNA ToDna() {
            return GenomeLoader.FromTeam(this);
        }

        public bool IsValid() {
            foreach (var mob in mobs) {
                if (mob.hp == 0) return false;

                foreach (var ability in mob.abilities) {
                    if (ability.ap > mob.ap) return false;
                    if (ability.dmg <= 0) return false;
                    if (ability.ap == 0) return false;
                    if (ability.range == 0) return false;
                }
            }

            return true;
        }
    }

    public class Setup {
        public List<JsonMob> red = new List<JsonMob>();
        public List<JsonMob> blue = new List<JsonMob>();

        public Setup() { }

        public Setup(List<JsonMob> red, List<JsonMob> blue) {
            this.red = red;
            this.blue = blue;
        }
    }

    public class JsonLoader {
        public static Setup Load(string content) {
            return JsonConvert.DeserializeObject<Setup>(content);
        }

        public static Team LoadTeam(string content) {
            return JsonConvert.DeserializeObject<Team>(content);
        }
    }
}