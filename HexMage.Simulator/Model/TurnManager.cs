using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace HexMage.Simulator {
    public class TurnManager {
        public List<int> PresortedOrder;

        [JsonIgnore] public GameInstance Game;

        [JsonConstructor]
        public TurnManager() { }

        public TurnManager(GameInstance game) {
            Game = game;
        }

        /// <summary>
        /// Prepare turn order for the initial mob configuration
        /// </summary>
        public void PresortTurnOrder() {
            PresortedOrder = Game.MobManager.Mobs.ToList();
            PresortedOrder.Sort((a, b) => {
                var aInfo = Game.MobManager.MobInfos[a];
                var bInfo = Game.MobManager.MobInfos[b];
                return aInfo.Iniciative.CompareTo(bInfo.Iniciative);
            });

            Game.State.SetCurrentMobIndex(Game, 0);
        }

        public void NextMobOrNewTurn() {
            var currentMobIndex = Game.State.CurrentMobIndex;
            if (!currentMobIndex.HasValue) {
                throw new InvalidOperationException("CurrentMob has no value but trying to move to the next.");
            }

            if (!Game.State.AllPlayed) {
                var currentMobId = Game.CurrentMob.Value;
                var mobInstance = Game.State.MobInstances[Game.CurrentMob.Value];
                if (mobInstance.Hp > 0) {
                    Game.State.PlayersPlayed.Add(currentMobId);
                }
            }

            if (currentMobIndex.Value >= Game.State.TurnOrder.Count - 1) {
                if (!Game.State.AllPlayed) {
                    Game.State.AllPlayed = Game.MobManager.Mobs.All(mobId => Game.State.PlayersPlayed.Contains(mobId));
                }
                StartNextTurn(Game);
            } else {
                Game.State.SetCurrentMobIndex(Game, currentMobIndex.Value + 1);

                Debug.Assert(Game.CurrentMob.HasValue, "There's no current mob but still trying to move to one.");
                var mobInstance = Game.State.MobInstances[Game.CurrentMob.Value];
                if (mobInstance.Hp <= 0) NextMobOrNewTurn();
            }
        }

        public TurnManager ShallowCopy(GameInstance gameCopy) {
            return new TurnManager(gameCopy) {
                PresortedOrder = PresortedOrder.ToList()
            };
        }

        public TurnManager DeepCopy(GameInstance gameCopy) {
            var copy = new TurnManager(gameCopy);

            // TODO - this is certainly the wrong place to do it, but at some point the game instance needs to be initialized
            if (PresortedOrder == null) {
                Utils.Log(LogSeverity.Warning, nameof(TurnManager),
                          "Initiated DeepCopy on an uninitialized GameInstance");
                PresortTurnOrder();
            }

            copy.PresortedOrder = PresortedOrder.ToList();

            return copy;
        }

        private void StartNextTurn(GameInstance game) {
            var state = game.State;
            for (int i = 0; i < state.MobInstances.Length; i++) {
                state.MobInstances[i].Ap = Game.MobManager.MobInfos[i].MaxAp;
            }

            state.ApplyDots(Game.Map, Game);

            foreach (var mobInstance in state.MobInstances) {
                Debug.Assert(mobInstance.Hp >= 0, "mobInstance.Hp >= 0");
            }

            state.TurnOrder.RemoveAll(x => state.MobInstances[x].Hp <= 0);
            state.LowerCooldowns();
            state.SetCurrentMobIndex(game, 0);

            // TODO: wut, ma tu tohle vubec byt?
            if (Game.CurrentMob.HasValue) {
                Debug.Assert(state.MobInstances[Game.CurrentMob.Value].Hp > 0, "Current mob is dead");
            }
        }
    }
}