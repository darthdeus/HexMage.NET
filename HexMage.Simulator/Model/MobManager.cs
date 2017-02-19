using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class MobManager {
        public readonly List<Ability> Abilities = new List<Ability>();
        public readonly List<int> Mobs = new List<int>();
        public readonly List<MobInfo> MobInfos = new List<MobInfo>();

        [JsonIgnore] public readonly Dictionary<TeamColor, IMobController> Teams =
            new Dictionary<TeamColor, IMobController>();

        public Ability AbilityForId(int id) {
            return Abilities[id];
        }

        public void InitializeState(GameState state) {
            state.Cooldowns.Clear();
            state.MobInstances = new MobInstance[Mobs.Count];

            foreach (var mobId in Mobs) {
                state.MobInstances[mobId] = new MobInstance(mobId);
                state.SetMobPosition(mobId, MobInfos[mobId].OrigCoord);
            }

            foreach (var _ in Abilities) {
                state.Cooldowns.Add(0);
            }

            state.Reset(this);
        }

        public void Clear() {
            Abilities.Clear();
            MobInfos.Clear();
            Mobs.Clear();
        }
    }
}