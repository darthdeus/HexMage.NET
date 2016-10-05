using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public enum TurnEndResult {
        NextMob,
        NextTurn
    }

    public class TurnManager : IResettable {
        private readonly GameInstance _gameInstance;
        private readonly Map _map;
        public MobManager MobManager { get; set; }
        public List<Mob> TurnOrder { get; set; } = new List<Mob>();
        public Mob CurrentMob => _current < TurnOrder.Count ? TurnOrder[_current] : null;
        public IMobController CurrentController => CurrentMob != null ? MobManager.Teams[CurrentMob.Team] : null;

        public int TurnNumber { get; private set; }
        private int _current = 0;

        public TurnManager(GameInstance gameInstance) {
            _gameInstance = gameInstance;
            MobManager = gameInstance.MobManager;
            _map = gameInstance.Map;
        }

        public void StartNextTurn(Pathfinder pathfinder) {
            TurnOrder.Clear();
            TurnNumber++;

            foreach (var mob in MobManager.Mobs) {
                if (mob.Hp > 0) {
                    mob.Ap = mob.MaxAp;
                    TurnOrder.Add(mob);
                }
            }

            MobManager.ApplyDots(_map, _gameInstance);
            MobManager.LowerCooldowns();

            _current = 0;

            TurnOrder.Sort((a, b) => a.Iniciative.CompareTo(b.Iniciative));
            if (CurrentMob != null) {
                pathfinder.PathfindFrom(CurrentMob.Coord);
            }
        }

        public TurnEndResult NextMobOrNewTurn(Pathfinder pathfinder) {
            if (_current >= TurnOrder.Count - 1) {
                Utils.Log(LogSeverity.Info, nameof(TurnManager), "Starting next turn");
                StartNextTurn(pathfinder);
                return TurnEndResult.NextTurn;
            } else {
                Utils.Log(LogSeverity.Info, nameof(TurnManager), "Moving to next mob (same turn)");
                _current++;

                if (CurrentMob.Hp <= 0) return NextMobOrNewTurn(pathfinder);

                pathfinder.PathfindFrom(CurrentMob.Coord);
                return TurnEndResult.NextMob;
            }
        }

        public void Reset() {
            TurnOrder.Clear();
            TurnNumber = 0;
        }
    }
}