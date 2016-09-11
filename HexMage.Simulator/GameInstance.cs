using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HexMage.Simulator {
    public class GameInstance {
        public Map Map { get; set; }
        public MobManager MobManager { get; set; }
        public Pathfinder Pathfinder { get; set; }
        public TurnManager TurnManager { get; set; }
        public int Size { get; set; }
        public Mob SelectedMob { get; set; }

        public GameInstance(int size) : this(size, new Map(size)) {}

        public GameInstance(int size, Map map) {
            Size = size;
            MobManager = new MobManager();
            Map = map;
            Pathfinder = new Pathfinder(Map, MobManager);
            TurnManager = new TurnManager(MobManager, Map);
        }

        public bool IsFinished() {
            // TODO - why is this still here?
#if DEBUG
            Debug.Assert(MobManager.Teams.All(team => team.Mobs.Count > 0));
#endif
            return MobManager.Teams.Any(team => team.Mobs.All(mob => mob.Hp == 0));
        }

        [Obsolete]
        public IList<Ability> UsableAbilities(Mob mob) {
            return mob.Abilities.Where(ability => IsAbilityUsable(mob, ability)).ToList();
        }

        public bool IsAbilityUsable(Mob mob, Ability ability) {
            // TODO - handle visibiliy
            var isElementdisabled = mob.Buffs
                                       .SelectMany(b => b.DisabledElements)
                                       .Distinct().Contains(ability.Element);

            return !isElementdisabled && mob.Ap >= ability.Cost && ability.CurrentCooldown == 0;
        }

        public IList<UsableAbility> UsableAbilities(Mob mob, Mob target) {
            int distance = Pathfinder.Distance(target.Coord);
            // TODO - handle visibiliy

            return mob.Abilities
                      .Select((ability, i) => new UsableAbility(mob, target, ability, i))
                      .Where(ua => ua.Ability.Range >= distance && IsAbilityUsable(mob, ua.Ability))
                      .ToList();
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            var usableAbilities = mob.Abilities.Where(ability => IsAbilityUsable(mob, ability)).ToList();

            if (usableAbilities.Count == 0) {
                return new List<Mob>();
            }

            int maxRange = usableAbilities.Max(ability => ability.Range);

            return MobManager
                .Mobs
                .Where(m => m != mob
                            && Pathfinder.Distance(m.Coord) <= maxRange
                            && m.Team != mob.Team)
                .ToList();
        }

        public IList<Mob> Enemies(Mob mob) {
            return MobManager.Mobs.Where(enemy => !enemy.Team.Equals(mob.Team)).ToList();
        }
    }
}