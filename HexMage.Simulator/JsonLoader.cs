using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HexMage.Simulator
{
    public class JsonAbility {
        public int dmg;
        public int ap;
        public int range;
        public int cooldown;
    }

    public class JsonMob {
        public int hp;
        public int ap;
        public List<JsonAbility> abilities;
    }

    public class Team {
        public string color;
        public List<JsonMob> mobs;
    }

    public class JsonLoader
    {
        public static List<JsonMob> Load(string filename) {
            return JsonConvert.DeserializeObject<List<JsonMob>>(filename);
        }
    }
}
