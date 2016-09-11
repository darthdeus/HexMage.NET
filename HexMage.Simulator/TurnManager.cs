using System.Collections.Generic;
using System.Diagnostics;

namespace HexMage.Simulator {
    public enum TurnEndResult {
        NextMob,
        NextTurn
    }

    public class TurnManager {
        private readonly Map _map;
        public MobManager MobManager { get; set; }
        public List<Mob> TurnOrder { get; set; } = new List<Mob>();
        public Mob CurrentMob => TurnOrder[_current];
        public IMobController CurrentController => CurrentMob.Team.Controller;
        //public Mob CurrentTarget { get; set; }
        //public int? SelectedAbilityIndex { get; set; }

        private int _current = 0;

        public TurnManager(MobManager mobManager, Map map) {
            _map = map;
            MobManager = mobManager;
        }

        public bool IsTurnDone() {
            return _current - 1 >= TurnOrder.Count;
        }

        public void StartNextTurn(Pathfinder pathfinder) {
            TurnOrder.Clear();

            foreach (var mob in MobManager.Mobs) {
                mob.Ap = mob.MaxAp;
                if (mob.Hp > 0) {
                    TurnOrder.Add(mob);
                }
            }

            MobManager.ApplyDots(_map);
            MobManager.LowerCooldowns();

            _current = 0;

            TurnOrder.Sort((a, b) => a.Iniciative.CompareTo(b.Iniciative));
            pathfinder.PathfindFrom(CurrentMob.Coord);
        }

        public TurnEndResult NextMobOrNewTurn(Pathfinder pathfinder) {
            if (!MoveNext(pathfinder)) {
                StartNextTurn(pathfinder);
                Utils.ThreadLog("Starting next turn");
                return TurnEndResult.NextTurn;
            } else {
                return TurnEndResult.NextMob;
            }
        }

        private bool MoveNext(Pathfinder pathfinder) {
            if (!IsTurnDone()) {
                _current++;
                Debug.Assert(_current < TurnOrder.Count);
            }

            pathfinder.PathfindFrom(CurrentMob.Coord);

            return !IsTurnDone();
        }
    }
}