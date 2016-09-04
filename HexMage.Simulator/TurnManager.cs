using System.Collections.Generic;

namespace HexMage.Simulator {
    public class TurnManager {
        private readonly Map _map;
        public MobManager MobManager { get; set; }
        public List<Mob> TurnOrder { get; set; } = new List<Mob>();
        public Mob CurrentMob => TurnOrder[_current];
        public Mob CurrentTarget { get; set; }
        public int? SelectedAbilityIndex { get; set; }

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
                mob.AP = mob.MaxAP;
                if (mob.HP > 0) {
                    TurnOrder.Add(mob);
                }
            }

            MobManager.ApplyDots(_map);
            MobManager.LowerCooldowns();

            _current = 0;

            TurnOrder.Sort((a, b) => a.AP.CompareTo(b.AP));
        }

        public void NextMobOrNewTurn() {
            if (!MoveNext()) {
                StartNextTurn();
            }
        }

        public bool MoveNext() {
            CurrentTarget = null;
            SelectedAbilityIndex = null;
            if (!IsTurnDone()) _current++;
            return !IsTurnDone();
        }

        public void ToggleAbilitySelected(int index) {
            if (SelectedAbilityIndex == index) {
                SelectedAbilityIndex = null;
            } else {
                SelectedAbilityIndex = index;
            }
        }
    }
}