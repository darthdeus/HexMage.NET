using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class MobManager {
        public List<Ability> Abilities = new List<Ability>();
        public List<int> Mobs = new List<int>();
        public List<MobInfo> MobInfos = new List<MobInfo>();

        [JsonIgnore] public readonly Dictionary<TeamColor, IMobController> Teams =
            new Dictionary<TeamColor, IMobController>();


        public Ability AbilityForId(int id) {
            return Abilities[id];
        }

        public void Clear() {
            
        }
    }
}