using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class GameInstance : IDeepCopyable<GameInstance>, IResettable {
        public Map Map { get; set; }
        public MobManager MobManager { get; set; }
        public Pathfinder Pathfinder { get; set; }
        public TurnManager TurnManager { get; set; }
        public int Size { get; set; }

        public GameInstance(Map map, MobManager mobManager) {
            Map = map;
            MobManager = mobManager;

            Size = map.Size;
            Pathfinder = new Pathfinder(Map, MobManager);
            TurnManager = new TurnManager(MobManager, Map);
        }

        public GameInstance(int size) : this(new Map(size)) {}

        public GameInstance(Map map) : this(map, new MobManager()) {}

        private GameInstance(int size, Map map, MobManager mobManager, Pathfinder pathfinder, TurnManager turnManager) {
            Size = size;
            MobManager = mobManager;
            Map = map;
            Pathfinder = pathfinder;
            TurnManager = turnManager;
        }

        public bool IsFinished() {
            bool redAlive = false;
            bool blueAlive = false;

            foreach (var mob in MobManager.Mobs) {
                if (mob.Hp > 0) {
                    switch (mob.Team) {
                        case TeamColor.Red:
                            redAlive = true;
                            break;
                        case TeamColor.Blue:
                            blueAlive = true;
                            break;
                    }
                }
            }

            return !redAlive || !blueAlive;
        }

        public bool IsAbilityUsable(Mob mob, Ability ability) {
            return mob.Ap >= ability.Cost && ability.CurrentCooldown == 0;
        }

        public IList<UsableAbility> UsableAbilities(Mob mob, Mob target) {
            var line = Map.AxialLinedraw(mob.Coord, target.Coord);
            int distance = line.Count - 1;

            var result = new List<UsableAbility>();

            foreach (var coord in line) {
                if (Map[coord] == HexType.Wall) {
                    Utils.Log(LogSeverity.Debug, nameof(GameInstance), "Path obstructed, no usable abilities.");
                    return result;
                }
            }

            for (int i = 0; i < mob.Abilities.Count; i++) {
                var ability = mob.Abilities[i];
                if (ability.Range >= distance && IsAbilityUsable(mob, ability)) {
                    result.Add(new UsableAbility(mob, target, ability, i));
                }
            }

            return result;
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            var result = new List<Mob>();

            var ability = mob.UsableMaxRange();

            foreach (var target in MobManager.Mobs) {
                if (target.Hp > 0 && Pathfinder.Distance(target.Coord) <= ability.Range && target.Team != mob.Team) {
                    result.Add(target);
                }
            }

            return result;
        }

        public IList<Mob> Enemies(Mob mob) {
            var result = new List<Mob>();

            foreach (var target in MobManager.Mobs) {
                if (target.Hp > 0 && target.Team != mob.Team) {
                    result.Add(target);
                }
            }

            return result;
        }

        public GameInstance DeepCopy() {
#warning TODO - tohle prepsat poradne
            var mapCopy = Map.DeepCopy();
            var mobManagerCopy = MobManager.DeepCopy();
            return new GameInstance(Size, mapCopy, mobManagerCopy, new Pathfinder(mapCopy, mobManagerCopy),
                                    new TurnManager(mobManagerCopy, mapCopy));
        }

        public void Reset() {
            Map.Reset();
            MobManager.Reset();
            TurnManager.Reset();
            Pathfinder.Reset();
        }
    }
}