using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class JsonAbility {
        public int dmg;
        public int ap;
        public int range;
        public int cooldown;

        public JsonAbility() {}

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
        public List<JsonAbility> abilities = new List<JsonAbility>();

        public MobInfo ToMobInfo(TeamColor color, IEnumerable<int> abilityIds) {
            return new MobInfo(color, hp, ap, 0, abilityIds.ToList());
        }
    }

    public class Team {
        public List<JsonMob> mobs = new List<JsonMob>();
    }

    public class Setup {
        public List<JsonMob> red = new List<JsonMob>();
        public List<JsonMob> blue = new List<JsonMob>();

        public Setup() {
            
        }

        public Setup(List<JsonMob> red, List<JsonMob> blue) {
            this.red = red;
            this.blue = blue;
        }

        public void UnpackIntoGame(GameInstance game) {
            var mobIds = new List<int>();

            foreach (var mob in red) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                mobIds.Add(game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Red, ids)));
            }

            foreach (var mob in blue) {
                var ids = mob.abilities.Select(ab => game.AddAbilityWithInfo(ab.ToAbility()));
                mobIds.Add(game.AddMobWithInfo(mob.ToMobInfo(TeamColor.Blue, ids)));
            }
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