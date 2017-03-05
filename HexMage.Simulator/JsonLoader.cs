using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator
{
    public class JsonAbility {
        public int dmg;
        public int ap;
        public int range;
        public int cooldown;

        public JsonAbility() {
        }

        public JsonAbility(int dmg, int ap, int range, int cooldown) {
            this.dmg = dmg;
            this.ap = ap;
            this.range = range;
            this.cooldown = cooldown;
        }

        public Ability ToAbility() {
            return new Ability(dmg, ap, range, cooldown, AbilityElement.Fire);
        }
    }

    public class JsonMob {
        public int hp;
        public int ap;
        public List<JsonAbility> abilities;

        public MobInfo ToMobInfo(TeamColor color, IEnumerable<int> abilityIds) {
            return new MobInfo(color, hp, ap, 0, abilityIds.ToList());
        }
    }

    public class Team {
        public List<JsonMob> mobs;
    }

    public class Setup {
        public List<JsonMob> red;
        public List<JsonMob> blue;
    }

    public class JsonLoader
    {
        public static Setup Load(string content) {
            return JsonConvert.DeserializeObject<Setup>(content);
        }

        public static Team LoadTeam(string content) {
            return JsonConvert.DeserializeObject<Team>(content);
        }
    }
}
