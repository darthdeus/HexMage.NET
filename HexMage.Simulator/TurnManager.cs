using System.Collections.Generic;

namespace HexMage.Simulator
{
    public class TurnManager
    {
        public MobManager MobManager { get; set; }
        public List<Mob> TurnOrder { get; set; } = new List<Mob>();
        public Mob CurrentMob => TurnOrder[_current];
        public Mob CurrentTarget { get; set; }

        private int _current = 0;

        public TurnManager(MobManager mobManager) {
            MobManager = mobManager;
        }

        public bool IsTurnDone() {
            return _current >= TurnOrder.Count;
        }

        public void StartNextTurn() {
            TurnOrder.Clear();
            CurrentTarget = null;

            foreach (var mob in MobManager.Mobs) {
                mob.AP = mob.MaxAP;
                if (mob.HP > 0) {
                    TurnOrder.Add(mob);
                }
            }

            _current = 0;

            TurnOrder.Sort((a, b) => a.AP.CompareTo(b.AP));
        }

        public void NextMobOrNewTurn() {
            if (!MoveNext()) {
                StartNextTurn();
            }
        }

        public bool MoveNext() {
            if (!IsTurnDone()) _current++;
            return !IsTurnDone();
        }
    }
}