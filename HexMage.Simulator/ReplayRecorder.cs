using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public readonly GameInstance Game;
        public readonly List<UctAction> Actions;
        public List<string> Log;

        public Replay(GameInstance game, List<UctAction> actions, List<string> log) {
            Game = game;
            Actions = actions;
            Log = log;
        }
    }

    public class ReplayRecorder {
        public const string ReplayDirectory = @"data\replays";
        public static readonly ReplayRecorder Instance = new ReplayRecorder();
        public static int Index = 0;
        public readonly List<UctAction> Actions = new List<UctAction>();
        public readonly List<string> Log = new List<string>();

        public void Clear() {
            Actions.Clear();
            Log.Clear();
        }

        public void SaveAndClear(GameInstance game, int? index = null) {
            if (!Constants.RecordReplays) {
                Utils.Log(LogSeverity.Info, nameof(ReplayRecorder),
                          "Skipping replay save since replay recording is disabled. This is done mostly to prevent debug replays from being overwritten.");
                return;
            }

            if (!index.HasValue) {
                index = Index++;
            }

            if (!Directory.Exists(ReplayDirectory)) {
                Directory.CreateDirectory(ReplayDirectory);
            }

            var replay = new Replay(game, Actions, Log);
            var contents = JsonConvert.SerializeObject(replay, Formatting.Indented);

            using (var writer = new StreamWriter($@"{ReplayDirectory}\replay{index}.json")) {
                writer.Write(contents);
            }

            Clear();

            Utils.Log(LogSeverity.Info, nameof(ReplayRecorder), $"Replay saved to file 'replay{index}.json'");
        }

        public Replay Load(int index) {
            using (var reader = new StreamReader($@"{ReplayDirectory}\replay{index}.json")) {
                var replay = JsonConvert.DeserializeObject<Replay>(reader.ReadToEnd());

                replay.Game.TurnManager.Game = replay.Game;
                replay.Game.Pathfinder.Game = replay.Game;
                replay.Game.Pathfinder.AllPaths = new HexMap<HexMap<Path>>(replay.Game.Size);
                replay.Game.Pathfinder.PathfindDistanceAll();

                replay.Game.PrepareEverything();                

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