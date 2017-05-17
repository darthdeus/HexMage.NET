using System.Collections.Generic;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    /// <summary>
    /// Represents a single replay
    /// </summary>
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
}