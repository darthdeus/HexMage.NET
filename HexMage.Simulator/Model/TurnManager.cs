using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HexMage.Simulator {
    public class TurnManager {
        private readonly GameInstance _game;
        public List<int> PresortedOrder;

        private MobManager MobManager => _game.MobManager;
        private GameState State => _game.State;

        public TurnManager(GameInstance game) {
            _game = game;
        }

        /// <summary>
        /// Prepare turn order for the initial mob configuration
        /// </summary>
        public void PresortTurnOrder() {
            PresortedOrder = MobManager.Mobs.ToList();
            PresortedOrder.Sort((a, b) => {
                var aInfo = MobManager.MobInfos[a];
                var bInfo = MobManager.MobInfos[b];
                return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
            });

            State.CurrentMobIndex = 0;
        }

        public void NextMobOrNewTurn() {
            var currentMobIndex = State.CurrentMobIndex;
            if (!currentMobIndex.HasValue)
                throw new InvalidOperationException("CurrentMob has no value but trying to move to the next.");

            if (currentMobIndex.Value >= State.TurnOrder.Count - 1) {
                StartNextTurn(State);
            } else {
                State.CurrentMobIndex = currentMobIndex.Value + 1;

                Debug.Assert(_game.CurrentMob.HasValue, "There's no current mob but still trying to move to one.");
                var mobInstance = State.MobInstances[_game.CurrentMob.Value];
                if (mobInstance.Hp <= 0) NextMobOrNewTurn();
            }
        }

        public TurnManager DeepCopy(GameInstance gameInstanceCopy) {
            var copy = new TurnManager(gameInstanceCopy);

            // TODO - this is certainly the wrong place to do it, but at some point the game instance needs to be initialized
            if (PresortedOrder == null) {
                Utils.Log(LogSeverity.Warning, nameof(TurnManager),
                          "Initiated DeepCopy on an uninitialized GameInstance");
                PresortTurnOrder();
            }

            copy.PresortedOrder = PresortedOrder.ToList();

            return copy;
        }

        private void StartNextTurn(GameState state) {
            for (int i = 0; i < state.MobInstances.Length; i++) {
                state.MobInstances[i].Ap = MobManager.MobInfos[i].MaxAp;
            }

            state.ApplyDots(_game.Map, _game);

            foreach (var mobInstance in state.MobInstances) {
                Debug.Assert(mobInstance.Hp >= 0, "mobInstance.Hp >= 0");
            }

            state.TurnOrder.RemoveAll(x => state.MobInstances[x].Hp <= 0);

            state.LowerCooldowns();

            state.CurrentMobIndex = 0;

            // TODO: wut, ma tu tohle vubec byt?
            if (_game.CurrentMob.HasValue) {
                Debug.Assert(state.MobInstances[_game.CurrentMob.Value].Hp > 0, "Current mob is dead");
            }
        }
    }
}