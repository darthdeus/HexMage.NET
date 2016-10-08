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
        public List<MobId> TurnOrder { get; set; } = new List<MobId>();
        public MobId? CurrentMob => _current < TurnOrder.Count ? TurnOrder[_current] : null;
        public IMobController CurrentController => CurrentMob != null ? MobManager.Teams[MobManager.MobInfoForId(CurrentMob.Value).Team] : null;

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

            foreach (var mobId in MobManager.Mobs) {
                var mob = MobManager.MobInstanceForId(mobId);
                if (mob.Hp > 0) {
                    MobManager.ResetAp(mobId);
                    TurnOrder.Add(mobId);
                }
            }

            MobManager.ApplyDots(_map, _gameInstance);
            MobManager.LowerCooldowns();

            _current = 0;

            TurnOrder.Sort((a, b) => {
                               var aInfo = MobManager.MobInfoForId(a);
                               var bInfo = MobManager.MobInfoForId(b);
                               return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
                           });

            if (CurrentMob != null) {
                pathfinder.PathfindFrom(MobManager.MobInstanceForId(CurrentMob.Value).Coord);
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

                Debug.Assert(CurrentMob.HasValue, "There's no current mob but still trying to move to one.");
                var mobInstance = MobManager.MobInstanceForId(CurrentMob.Value);
                if (mobInstance.Hp <= 0) return NextMobOrNewTurn(pathfinder);

                pathfinder.PathfindFrom(mobInstance.Coord);
                return TurnEndResult.NextMob;
            }
        }

        public void Reset() {
            TurnOrder.Clear();
            TurnNumber = 0;
        }
    }
}