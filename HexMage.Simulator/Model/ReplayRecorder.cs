using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class ReplayRecorder : IGameEventSubscriber {
        public class ReplayAction {
            private enum ActionType {
                MoveAction,
                AbilityAction
            }

            public Mob Current { get; set; }
            public Mob Target { get; set; }
            public UsableAbility UsableAbility { get; set; }
            public AxialCoord Coord { get; set; }
            private ActionType _type;

            private ReplayAction() {}

            public static ReplayAction CreateAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
                return new ReplayAction {
                    Current = mob,
                    Target = target,
                    UsableAbility = ability,
                    _type = ActionType.AbilityAction
                };
            }

            public static ReplayAction CreateMobMoved(Mob mob, AxialCoord pos) {
                return new ReplayAction {
                    Current = mob,
                    Coord = pos,
                    _type = ActionType.MoveAction
                };
            }

            public async Task Play(GameEventHub hub) {
                switch (_type) {
                    case ActionType.MoveAction:
                        await hub.BroadcastMobMoved(Current, Coord);
                        break;
                    case ActionType.AbilityAction:
                        await hub.BroadcastAbilityUsed(Current, Target, UsableAbility);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid value for {nameof(_type)}: '{_type}'.");
                }
            }
        }

        public readonly List<ReplayAction> Actions = new List<ReplayAction>();

        public Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            Actions.Add(ReplayAction.CreateAbilityUsed(mob, target, ability));
            return Task.FromResult(true);
        }

        public Task<bool> EventMobMoved(Mob mob, AxialCoord pos) {
            Actions.Add(ReplayAction.CreateMobMoved(mob, pos));
            return Task.FromResult(true);
        }
    }
}