using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HexMage.Simulator.AI;
using HexMage.Simulator.Pathfinding;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class Replay {
        public Map Map;
        public MobManager MobManager;
        public List<UctAction> Actions;

        public Replay(Map map, MobManager mobManager, List<UctAction> actions) {
            Map = map;
            MobManager = mobManager;
            Actions = actions;
        }
    }

    public class ReplayRecorder {
        public const string ReplayDirectory = @"data\replays";
        public static readonly ReplayRecorder Instance = new ReplayRecorder();
        public static int Index = 0;
        public readonly List<UctAction> Actions = new List<UctAction>();

        public void SaveAndClear(GameInstance game, int? index = null) {
            if (!index.HasValue) {
                index = Index++;
            }

            if (!Directory.Exists(ReplayDirectory)) {
                Directory.CreateDirectory(ReplayDirectory);
            }

            var replay = new Replay(game.Map, game.MobManager, Actions);
            var contents = JsonConvert.SerializeObject(replay);

            using (var writer = new StreamWriter($@"{ReplayDirectory}\replay{index}.json")) {
                writer.Write(contents);
            }

            Actions.Clear();
        }

        public Replay Load(int index) {
            using (var reader = new StreamReader($@"{ReplayDirectory}\replay{index}.json")) {
                var replay = JsonConvert.DeserializeObject<Replay>(reader.ReadToEnd());
                return replay;
            }
        }

        public List<int> AllReplays() {
            return Directory.EnumerateFiles(ReplayDirectory)
                            .Where(path => new Regex("replay\\d+\\.json").IsMatch(path))
                            .Select(path => path.Replace("replay", "")
                                                .Replace(".json", ""))
                            .Select(int.Parse)
                            .ToList();
        }
    }
}