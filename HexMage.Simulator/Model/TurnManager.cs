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

        public TurnManager(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public IMobController CurrentController
            => CurrentMob != null ? MobManager.Teams[MobManager.MobInfos[CurrentMob.Value].Team] : null;

        public int? CurrentMob {
            get {
                if (!State.CurrentMobIndex.HasValue) {
                    return null;
                } else if (State.CurrentMobIndex.Value < _turnOrder.Count) {
                    return _turnOrder[State.CurrentMobIndex.Value];
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Prepare turn order for the initial mob configuration
        /// </summary>
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

            state.ApplyDots(_gameInstance.Map, _gameInstance);

            foreach (var mobInstance in state.MobInstances) {
                Debug.Assert(mobInstance.Hp >= 0, "mobInstance.Hp >= 0");
            }

            _turnOrder.RemoveAll(x => state.MobInstances[x].Hp <= 0);

            state.LowerCooldowns();

            state.CurrentMobIndex = 0;

            // TODO: wut, ma tu tohle vubec byt?
            if (CurrentMob.HasValue) {
                Debug.Assert(state.MobInstances[CurrentMob.Value].Hp > 0, "Current mob is dead");
            }
        }

        public TurnEndResult NextMobOrNewTurn(Pathfinder pathfinder, GameState state) {
            Debug.Assert(state.CurrentMobIndex.HasValue, "state.CurrentMobIndex.HasValue");
            if (!state.CurrentMobIndex.HasValue) {
                Utils.Log(LogSeverity.Error, nameof(TurnManager),
                          "CurrentMob has no value but trying to move to the next.");
                return TurnEndResult.NextMob;
            }

            if (state.CurrentMobIndex.Value >= _turnOrder.Count - 1) {
                StartNextTurn(pathfinder, state);
                return TurnEndResult.NextTurn;
            } else {
                state.CurrentMobIndex = state.CurrentMobIndex.Value + 1;

                Debug.Assert(CurrentMob.HasValue, "There's no current mob but still trying to move to one.");
                var mobInstance = state.MobInstances[CurrentMob.Value];
                if (mobInstance.Hp <= 0) return NextMobOrNewTurn(pathfinder, state);

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

            // TODO - this is certainly the wrong place to do it, but at some point the game instance needs to be initialized
            if (_presortedOrder == null) {
                Utils.Log(LogSeverity.Warning, nameof(TurnManager),
                          "Initiated DeepCopy on an uninitialized GameInstance");
                PresortTurnOrder();
            }

            if (_turnOrder == null) {
                CopyTurnOrderFromPresort();
            }

            copy._presortedOrder = _presortedOrder.ToList();
            copy._turnOrder = _turnOrder.ToList();
            copy.TurnNumber = TurnNumber;

            return copy;
        }
    }
}