﻿using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public enum TurnEndResult {
        NextMob,
        NextTurn
    }

    public class TurnManager {
        private readonly Map _map;
        public MobManager MobManager { get; set; }
        public List<Mob> TurnOrder { get; set; } = new List<Mob>();
        public Mob CurrentMob => _current < TurnOrder.Count ? TurnOrder[_current] : null;
        public IMobController CurrentController => CurrentMob.Team.Controller;

        private int _current = 0;

        public TurnManager(MobManager mobManager, Map map) {
            _map = map;
            MobManager = mobManager;
        }

        public void StartNextTurn(Pathfinder pathfinder) {
            TurnOrder.Clear();

            foreach (var mob in MobManager.AliveMobs) {
                mob.Ap = mob.MaxAp;
                TurnOrder.Add(mob);
            }

            MobManager.ApplyDots(_map);
            MobManager.LowerCooldowns();

            _current = 0;

            TurnOrder.Sort((a, b) => a.Iniciative.CompareTo(b.Iniciative));
            pathfinder.PathfindFrom(CurrentMob.Coord);
        }

        public TurnEndResult NextMobOrNewTurn(Pathfinder pathfinder) {
            if (_current >= TurnOrder.Count - 1) {
                Utils.Log(LogSeverity.Info, nameof(TurnManager), "Starting next turn");
                StartNextTurn(pathfinder);
                return TurnEndResult.NextTurn;
            } else {
                Utils.Log(LogSeverity.Info, nameof(TurnManager), "Moving to next mob (same turn)");
                _current++;
                Debug.Assert(_current < TurnOrder.Count);
                pathfinder.PathfindFrom(CurrentMob.Coord);
                return TurnEndResult.NextMob;
            }
        }
    }
}