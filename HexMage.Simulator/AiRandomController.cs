using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class GameState {
        public MobInstance[] MobInstances = new MobInstance[0];
        public List<int> Cooldowns = new List<int>();
        public HexMap<int?> MobPositions;
        public int? CurrentMobIndex;
        public int TurnNumber;
    }

    public class GameInfo {
        
    }

    public class UctAction {}

    public class UctNode {
        public float Q { get; set; }
        public int N { get; set; }
        public UctAction Action { get; set; }
        public GameInstance State { get; set; }
        public UctNode Parent { get; set; }
        public List<UctNode> Children { get; set; } = new List<UctNode>();
        public List<UctAction> PossibleActions = null;

        public bool IsTerminal => State.IsFinished;
        public bool IsFullyExpanded => PossibleActions != null && PossibleActions.Count == Children.Count;

        public UctNode(float q, int n, UctAction action, GameInstance state) {
            Q = q;
            N = n;
            Action = action;
            State = state;
        }

        public void ComputePossibleActions() {
            PossibleActions = new List<UctAction>();
        }
    }

    public class UctAlgorithm {
        public UctNode _root;

        public UctNode UctSearch(GameInstance initialState) {
            _root = new UctNode(0, 0, null, initialState);

            int iterations = 1000;

            while (iterations-- > 0) {
                UctNode v = TreePolicy(_root);
                float delta = DefaultPolicy(v.State);
                Backup(v, delta);
            }

            return BestChild(_root);
        }

        public UctNode TreePolicy(UctNode node) {
            while (!node.IsTerminal) {
                if (!node.IsFullyExpanded) {
                    return Expand(node);
                } else {
                    node = BestChild(node);
                }
            }

            return node;
        }

        public UctNode BestChild(UctNode node) {
            if (node.Children.Count == 0) return null;

            UctNode best = node.Children[0];
            foreach (var child in node.Children) {
                if (UcbValue(node, child) > UcbValue(node, best)) {
                    best = node;
                }
            }

            return best;
        }

        public float UcbValue(UctNode parent, UctNode node) {
            return (float) (node.Q/node.N + Math.Sqrt(2*Math.Log(parent.N)/node.N));
        }

        public UctNode Expand(UctNode node) {
            if (node.PossibleActions == null) {
                node.ComputePossibleActions();
            }

            var action = node.PossibleActions[node.Children.Count];
            var child = new UctNode(0, 1, action, F(node.State, action));

            node.Children.Add(child);

            return child;
        }

        private GameInstance F(GameInstance state, UctAction action) {
            throw new NotImplementedException();
        }

        public float DefaultPolicy(GameInstance game) {
            throw new NotImplementedException();
        }

        public void Backup(UctNode node, float delta) {
            while (node != null) {
                node.N++;
                node.Q += delta;
                node = node.Parent;
            }
        }
    }

    public enum PossibleActions {
        MoveForward,
        MoveBack,
        Attack
    }

    public class FlatMonteCarlo {
        public UctAction Run(GameInstance initialState) {
            var possibleStates = new List<GameInstance>();
            if (!initialState.TurnManager.CurrentMob.HasValue) {
                throw new NotImplementedException();
            }

            var mobId = initialState.TurnManager.CurrentMob.Value;
            var mobInfo = initialState.MobManager.MobInfos[mobId];
            var mobInstance = initialState.MobManager.MobInstances[mobId];

            {
                var pathfinder = initialState.Pathfinder;

                int moveTarget = MobInstance.InvalidId;
                MobInstance moveTargetInstance = new MobInstance();

                foreach (var possibleTargetId in initialState.MobManager.Mobs) {
                    var possibleTargetInstance = initialState.MobManager.MobInstanceForId(possibleTargetId);

                    if (possibleTargetInstance.Hp <= 0) continue;


                    foreach (var abilityId in mobInfo.Abilities) {
                        if (initialState.IsAbilityUsable(mobId, abilityId, possibleTargetId)) {
                            var stateWithUsedAbility = initialState.DeepCopy();

                            stateWithUsedAbility.FastUseWithDefenseDesire(mobId, possibleTargetId, abilityId,
                                                                          DefenseDesire.Pass);

                            possibleStates.Add(stateWithUsedAbility);
                        }
                    }

                    if (moveTarget == MobInstance.InvalidId) {
                        moveTarget = possibleTargetId;
                        moveTargetInstance = possibleTargetInstance;
                    }

                    if (pathfinder.Distance(moveTargetInstance.Coord) >
                        pathfinder.Distance(possibleTargetInstance.Coord)) {
                        moveTarget = possibleTargetId;
                        moveTargetInstance = possibleTargetInstance;
                    }
                }

                var moveForwardCopy = initialState.DeepCopy();
                FastMoveTowardsEnemy(moveForwardCopy, mobId, moveTarget);

                possibleStates.Add(moveForwardCopy);
            }

            foreach (var state in possibleStates) {
                var hub = new GameEventHub(state);
                state.MobManager.Teams[TeamColor.Red] = new AiRandomController(state);
                state.MobManager.Teams[TeamColor.Blue] = new AiRandomController(state);

                var rounds = hub.FastMainLoop(TimeSpan.Zero);
                Console.WriteLine($"Took {rounds} rounds");
            }            

            return null;
        }

        private void FastMoveTowardsEnemy(GameInstance state, int mobId, int targetId) {
            var pathfinder = state.Pathfinder;
            var mobInstance = state.MobManager.MobInstanceForId(mobId);
            var targetInstance = state.MobManager.MobInstanceForId(targetId);

            var moveTarget = pathfinder.FurthestPointToTarget(mobInstance, targetInstance);

            if (moveTarget != null && pathfinder.Distance(moveTarget.Value) <= mobInstance.Ap) {
                state.MobManager.FastMoveMob(state.Map, state.Pathfinder, mobId,
                                             moveTarget.Value);
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
            }
        }
    }

    public class AiRandomController : IMobController {
        private readonly GameInstance _gameInstance;

        public AiRandomController(GameInstance gameInstance) {
            _gameInstance = gameInstance;
        }

        public DefenseDesire FastRequestDesireToDefend(int mobId, int ability) {
            return DefenseDesire.Pass;
        }

        public void FastPlayTurn(GameEventHub eventHub) {
            FastRandomAction(eventHub);
        }

        public void FastRandomAction(GameEventHub eventHub) {
            var mobId = _gameInstance.TurnManager.CurrentMob;

            if (mobId == null)
                throw new InvalidOperationException("Requesting mob action when there is no current mob.");

            var pathfinder = _gameInstance.Pathfinder;

            var mobInfo = _gameInstance.MobManager.MobInfoForId(mobId.Value);
            var mobInstance = _gameInstance.MobManager.MobInstanceForId(mobId.Value);

            Ability ability = null;
            foreach (var possibleAbilityId in mobInfo.Abilities) {
                var possibleAbility = _gameInstance.MobManager.AbilityForId(possibleAbilityId);

                if (possibleAbility.Cost <= mobInstance.Ap &&
                    _gameInstance.MobManager.CooldownFor(possibleAbilityId) == 0) {
                    ability = possibleAbility;
                }
            }

            int spellTarget = MobInstance.InvalidId;
            int moveTarget = MobInstance.InvalidId;

            foreach (var possibleTarget in _gameInstance.MobManager.Mobs) {
                var possibleTargetInstance = _gameInstance.MobManager.MobInstanceForId(possibleTarget);
                var possibleTargetInfo = _gameInstance.MobManager.MobInfoForId(possibleTarget);
                if (possibleTargetInstance.Hp <= 0) continue;

                // TODO - mela by to byt viditelna vzdalenost
                if (possibleTargetInfo.Team != mobInfo.Team) {
                    if (pathfinder.Distance(possibleTargetInstance.Coord) <= ability.Range) {
                        spellTarget = possibleTarget;
                        break;
                    }

                    moveTarget = possibleTarget;
                }
            }

            if (spellTarget != MobInstance.InvalidId) {
                _gameInstance.FastUse(ability.Id, mobId.Value, spellTarget);
            } else if (moveTarget != MobInstance.InvalidId) {
                FastMoveTowardsEnemy(mobId.Value, moveTarget);
            } else {
                throw new InvalidOperationException("No targets, game should be over.");
            }
        }

        private void FastMoveTowardsEnemy(int mobId, int targetId) {
            var pathfinder = _gameInstance.Pathfinder;
            var mobInstance = _gameInstance.MobManager.MobInstanceForId(mobId);
            var targetInstance = _gameInstance.MobManager.MobInstanceForId(targetId);

            var moveTarget = pathfinder.FurthestPointToTarget(mobInstance, targetInstance);

            if (moveTarget != null && pathfinder.Distance(moveTarget.Value) <= mobInstance.Ap) {
                _gameInstance.MobManager.FastMoveMob(_gameInstance.Map, _gameInstance.Pathfinder, mobId,
                                                     moveTarget.Value);
            } else {
                Utils.Log(LogSeverity.Debug, nameof(AiRandomController),
                          $"Move failed since target is too close, source {mobInstance.Coord}, target {targetInstance.Coord}");
            }
        }

        public Task<DefenseDesire> SlowRequestDesireToDefend(int targetId, int abilityId) {
            return Task.FromResult(DefenseDesire.Pass);
        }

        public Task SlowPlayTurn(GameEventHub eventHub) {
            FastRandomAction(eventHub);
            return Task.CompletedTask;
        }

        public string Name => nameof(AiRandomController);
    }
}