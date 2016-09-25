﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            public DefenseDesire DefenseDesire { get; set; }

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
                        await hub.BroadcastAbilityUsedWithDefense(Current, Target, UsableAbility, DefenseDesire);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid value for {nameof(_type)}: '{_type}'.");
                }
            }

            public override string ToString() {
                return $"{nameof(_type)}: {_type}, {nameof(Current)}: {Current}, {nameof(Target)}: {Target}, {nameof(UsableAbility)}: {UsableAbility}, {nameof(Coord)}: {Coord}, {nameof(DefenseDesire)}: {DefenseDesire}";
            }
        }

        public readonly List<ReplayAction> Actions = new List<ReplayAction>();

        private ReplayAction _unfinishedAbiltiyAction;

        public Task<bool> EventAbilityUsed(Mob mob, Mob target, UsableAbility ability) {
            Debug.Assert(_unfinishedAbiltiyAction == null, "Received another ability event when waiting for defense desire result.");
            _unfinishedAbiltiyAction = ReplayAction.CreateAbilityUsed(mob, target, ability);
            return Task.FromResult(true);
        }

        public Task<bool> EventMobMoved(Mob mob, AxialCoord pos) {
            Debug.Assert(_unfinishedAbiltiyAction == null, "Received move event when waiting for defense desire result.");
            Actions.Add(ReplayAction.CreateMobMoved(mob, pos));
            return Task.FromResult(true);
        }

        public Task<bool> EventDefenseDesireAcquired(Mob mob, DefenseDesire defenseDesireResult) {
            _unfinishedAbiltiyAction.DefenseDesire = defenseDesireResult;
            Actions.Add(_unfinishedAbiltiyAction);
            _unfinishedAbiltiyAction = null;

            return Task.FromResult(true);
        }

        public void DumpReplay(TextWriter writer) {
            foreach (var action in Actions) {
                writer.WriteLine(action);
            }
        }
    }
}