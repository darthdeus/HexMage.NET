using System;
using System.Collections.Generic;
using System.Linq;
using HexMage.Simulator.Model;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class MobManager : IDeepCopyable<MobManager> {
        public List<int> Mobs = new List<int>();
        public readonly List<AbilityInfo> Abilities = new List<AbilityInfo>();
        public readonly List<MobInfo> MobInfos = new List<MobInfo>();

        [JsonIgnore] public readonly Dictionary<TeamColor, IMobController> Teams =
            new Dictionary<TeamColor, IMobController>();

        [Obsolete]
        public AbilityInfo AbilityForId(int id) {
            return Abilities[id];
        }

        [Obsolete]
        public void InitializeState(GameState state) {
#warning DEPRECATED: InitializeState by se asi nemelo pouzivat vubec? K cemu to vlastne je?
            state.Cooldowns.Clear();
            state.MobInstances = new MobInstance[Mobs.Count];

            foreach (var mobId in Mobs) {
                state.MobInstances[mobId] = new MobInstance(mobId);
                state.SetMobPosition(mobId, MobInfos[mobId].OrigCoord);
            }

            foreach (var _ in Abilities) {
                state.Cooldowns.Add(0);
            }
        }

        public void Clear() {
            Abilities.Clear();
            MobInfos.Clear();
            Mobs.Clear();
        }

        public MobManager DeepCopy() {
            var copy = new MobManager {
                Mobs = Mobs.ToList()
            };

            foreach (var mobInfo in MobInfos) {
                copy.MobInfos.Add(mobInfo.DeepCopy());
            }

            foreach (var abilityInfo in Abilities) {
                copy.Abilities.Add(abilityInfo.DeepCopy());
            }

            return copy;
        }
    }
}