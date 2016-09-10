using System.Collections.Generic;

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
            return _current >= TurnOrder.Count;
        }

        public void StartNextTurn() {
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
        }

        public TurnEndResult NextMobOrNewTurn() {
            if (!MoveNext()) {
                StartNextTurn();
                return TurnEndResult.NextTurn;
            } else {
                return TurnEndResult.NextMob;
            }
        }

        public bool MoveNext() {
            if (!IsTurnDone()) _current++;
            return !IsTurnDone();
        }

    }
}