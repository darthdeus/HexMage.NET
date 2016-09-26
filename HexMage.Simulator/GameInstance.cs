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
            return MobManager.AliveMobs
                             .Select(m => m.Team)
                             .Distinct()
                             .Count() <= 1;
        }

        [Obsolete]
        public IList<Ability> UsableAbilities(Mob mob) {
            return mob.Abilities.Where(ability => IsAbilityUsable(mob, ability)).ToList();
        }

        public bool IsAbilityUsable(Mob mob, Ability ability) {
            // TODO - handle visibiliy
            var isElementDisabled = mob.Buffs
                                       .SelectMany(b => b.DisabledElements)
                                       .Distinct()
                                       .Contains(ability.Element);

            return !isElementDisabled && mob.Ap >= ability.Cost && ability.CurrentCooldown == 0;
        }

        public IList<UsableAbility> UsableAbilities(Mob mob, Mob target) {
            var line = Map.CubeLinedraw(mob.Coord, target.Coord);
            int distance = line.Count - 1;

            var obstructed = line.Any(c => Map[c] == HexType.Wall);

            if (obstructed) {
                Utils.Log(LogSeverity.Debug, nameof(GameInstance), "Path obstructed, no usable abilities.");
                return new List<UsableAbility>();
            }

            return mob.Abilities
                      .Select((ability, i) => new UsableAbility(mob, target, ability, i))
                      .Where(ua => ua.Ability.Range >= distance && IsAbilityUsable(mob, ua.Ability))
                      .ToList();
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            var usableAbilities = mob.Abilities
                                     .Where(ability => IsAbilityUsable(mob, ability))
                                     .ToList();

            if (usableAbilities.Count == 0) {
                return new List<Mob>();
            }

            int maxRange = usableAbilities.Max(ability => ability.Range);

            return MobManager
                .AliveMobs
                .Where(m => m != mob
                            && Pathfinder.Distance(m.Coord) <= maxRange
                            && m.Team != mob.Team)
                .ToList();
        }

        public IList<Mob> Enemies(Mob mob) {
            return MobManager.AliveMobs
                             .Where(enemy => !enemy.Team.Equals(mob.Team))
                             .ToList();
        }

        public GameInstance DeepCopy() {
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