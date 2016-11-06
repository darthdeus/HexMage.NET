using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public enum TurnEndResult {
        NextMob,
        NextTurn
    }

    public class TurnManager : IResettable {
        private readonly GameInstance _gameInstance;
        public int TurnNumber { get; private set; }
        private List<int> _turnOrder;
        private List<int> _presortedOrder;

        private MobManager MobManager => _gameInstance.MobManager;
        private GameState State => _gameInstance.State;

        public IMobController CurrentController
            => CurrentMob != null ? MobManager.Teams[MobManager.MobInfos[CurrentMob.Value].Team] : null;

        public int? CurrentMob {
            get {
                if (!State.CurrentMobIndex.HasValue) return null;
                if (State.CurrentMobIndex.Value < _turnOrder.Count) {
                    return _turnOrder[State.CurrentMobIndex.Value];
                } else {
                    return null;
                }
            }
        }

        public TurnManager(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }


        public void PresortTurnOrder() {
            _presortedOrder = MobManager.Mobs.ToList();
            _presortedOrder.Sort((a, b) => {
                                     var aInfo = MobManager.MobInfos[a];
                                     var bInfo = MobManager.MobInfos[b];
                                     return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
                                 });

            CopyTurnOrderFromPresort();
        }

        public void StartNextTurn(Pathfinder pathfinder, GameState state) {
            TurnNumber++;

            for (int i = 0; i < state.MobInstances.Length; i++) {
                state.MobInstances[i].Ap = MobManager.MobInfos[i].MaxAp;
            }

            _turnOrder.RemoveAll(x => state.MobInstances[x].Hp <= 0);

            state.ApplyDots(_gameInstance.Map, _gameInstance);
            state.LowerCooldowns();

            state.CurrentMobIndex = 0;

            if (CurrentMob != null) {
                pathfinder.PathfindFrom(state.MobInstances[CurrentMob.Value].Coord);
            } else {
                Utils.Log(LogSeverity.Warning, nameof(Pathfinder), "CurrentMob is NULL, pathfind current failed");
            }
        }

        public TurnEndResult NextMobOrNewTurn(Pathfinder pathfinder, GameState state) {
            Debug.Assert(state.CurrentMobIndex.HasValue, "state.CurrentMobIndex.HasValue");

            if (state.CurrentMobIndex.Value >= _turnOrder.Count - 1) {
                //Utils.Log(LogSeverity.Info, nameof(TurnManager), "Starting next turn");
                StartNextTurn(pathfinder, state);
                return TurnEndResult.NextTurn;
            } else {
                //Utils.Log(LogSeverity.Info, nameof(TurnManager), "Moving to next mob (same turn)");
                state.CurrentMobIndex = state.CurrentMobIndex.Value + 1;

                Debug.Assert(CurrentMob.HasValue, "There's no current mob but still trying to move to one.");
                var mobInstance = state.MobInstances[CurrentMob.Value];
                if (mobInstance.Hp <= 0) return NextMobOrNewTurn(pathfinder, state);

                pathfinder.PathfindFrom(mobInstance.Coord);
                return TurnEndResult.NextMob;
            }
        }

        public void Reset() {
            CopyTurnOrderFromPresort();
            TurnNumber = 0;
        }

        private void CopyTurnOrderFromPresort() {
            _turnOrder = new List<int>();

            foreach (var id in _presortedOrder) {
                _turnOrder.Add(id);
            }
        }

        public TurnManager DeepCopy(GameInstance gameInstanceCopy) {
            var copy = new TurnManager(gameInstanceCopy);

            copy._presortedOrder = _presortedOrder.ToList();
            copy._turnOrder = _turnOrder.ToList();
            copy.TurnNumber = TurnNumber;

            return copy;
        }
    }
}