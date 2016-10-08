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
        private readonly Map _map;
        public MobManager MobManager { get; set; }

        public IMobController CurrentController
            => CurrentMob != null ? MobManager.Teams[MobManager.MobInfoForId(CurrentMob.Value).Team] : null;

        public MobId? CurrentMob {
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
            MobManager = gameInstance.MobManager;
            _map = gameInstance.Map;
        }

        private List<MobId> _turnOrder;
        private List<MobId> _presortedOrder;


        public void PresortTurnOrder() {
            _presortedOrder = MobManager.Mobs.ToList();
            _presortedOrder.Sort((a, b) => {
                                     var aInfo = MobManager.MobInfoForId(a);
                                     var bInfo = MobManager.MobInfoForId(b);
                                     return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
                                 });

            CopyTurnOrderFromPresort();
        }

        public void StartNextTurn(Pathfinder pathfinder) {
            TurnNumber++;

            foreach (var mobId in MobManager.Mobs) {
                var mob = MobManager.MobInstanceForId(mobId);
                if (mob.Hp > 0) {
                    MobManager.ResetAp(mobId);
                }
            }

            _turnOrder.RemoveAll(x => MobManager.MobInstances[x].Hp <= 0);

            MobManager.ApplyDots(_map, _gameInstance);
            MobManager.LowerCooldowns();

            _current = 0;

            if (CurrentMob != null) {
                pathfinder.PathfindFrom(MobManager.MobInstanceForId(CurrentMob.Value).Coord);
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
                var mobInstance = MobManager.MobInstanceForId(CurrentMob.Value);
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
            _turnOrder = new List<MobId>();

            foreach (var id in _presortedOrder) {
                _turnOrder.Add(id);
            }
        }
    }
}