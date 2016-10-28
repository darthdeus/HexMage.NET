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
        private MobManager _mobManager => _gameInstance.MobManager;

        public IMobController CurrentController
            => CurrentMob != null ? _mobManager.Teams[_mobManager.MobInfoForId(CurrentMob.Value).Team] : null;

        public int? CurrentMob {
            get {
                if (_current < _turnOrder.Count) {
                    return _turnOrder[_current];
                } else {
                    return null;
                }
            }
        }

        public int TurnNumber { get; private set; }
        private int _current = 0;

        public TurnManager(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        private List<int> _turnOrder;
        private List<int> _presortedOrder;


        public void PresortTurnOrder() {
            _presortedOrder = _mobManager.Mobs.ToList();
            _presortedOrder.Sort((a, b) => {
                                     var aInfo = _mobManager.MobInfoForId(a);
                                     var bInfo = _mobManager.MobInfoForId(b);
                                     return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
                                 });

            CopyTurnOrderFromPresort();
        }

        public void StartNextTurn(Pathfinder pathfinder) {
            TurnNumber++;

            for (int i = 0; i < _mobManager.MobInstances.Length; i++) {
                _mobManager.MobInstances[i].Ap = _mobManager.MobInfos[i].MaxAp;
            }

            //foreach (var mobId in MobManager.Mobs) {
            //    var mob = MobManager.MobInstanceForId(mobId);
            //    if (mob.Hp > 0) {
            //        MobManager.ResetAp(mobId);
            //    }
            //}

            _turnOrder.RemoveAll(x => _mobManager.MobInstances[x].Hp <= 0);

            _mobManager.ApplyDots(_gameInstance.Map, _gameInstance);
            _mobManager.LowerCooldowns();

            _current = 0;

            if (CurrentMob != null) {
                pathfinder.PathfindFrom(_mobManager.MobInstanceForId(CurrentMob.Value).Coord);
            } else {
                Utils.Log(LogSeverity.Warning, nameof(Pathfinder), "CurrentMob is NULL, pathfind current failed");
            }
        }

        public TurnEndResult NextMobOrNewTurn(Pathfinder pathfinder) {
            if (_current >= _turnOrder.Count - 1) {
                //Utils.Log(LogSeverity.Info, nameof(TurnManager), "Starting next turn");
                StartNextTurn(pathfinder);
                return TurnEndResult.NextTurn;
            } else {
                //Utils.Log(LogSeverity.Info, nameof(TurnManager), "Moving to next mob (same turn)");
                _current++;

                Debug.Assert(CurrentMob.HasValue, "There's no current mob but still trying to move to one.");
                var mobInstance = _mobManager.MobInstanceForId(CurrentMob.Value);
                if (mobInstance.Hp <= 0) return NextMobOrNewTurn(pathfinder);

                pathfinder.PathfindFrom(mobInstance.Coord);
                return TurnEndResult.NextMob;
            }
        }

        public void Reset() {
            CopyTurnOrderFromPresort();
            TurnNumber = 0;
        }

        private void CopyTurnOrderFromPresort() {
            //_presortedOrder = MobManager.Mobs.ToList();
            //_presortedOrder.Sort((a, b) => {
            //    var aInfo = MobManager.MobInfoForId(a);
            //    var bInfo = MobManager.MobInfoForId(b);
            //    return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
            //});

            //_turnOrder = _presortedOrder;
            _turnOrder = new List<int>();

            foreach (var id in _presortedOrder) {
                _turnOrder.Add(id);
            }
        }
    }
}