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

        public GameInstance(int size) {
            Size = size;
            MobManager = new MobManager();
            Map = new Map(size);
            Pathfinder = new Pathfinder(Map, MobManager);
            TurnManager = new TurnManager(MobManager, Map);
        }


        public bool IsFinished() {
            // TODO - why is this still here?
#if DEBUG
            Debug.Assert(MobManager.Teams.All(team => team.Mobs.Count > 0));
#endif
            return MobManager.Teams.Any(team => team.Mobs.All(mob => mob.HP == 0));
        }

        // TODO - figure out why this isn't being used
        public void Refresh() {
            Pathfinder.PathfindFrom(TurnManager.CurrentMob.Coord);
        }

        [Obsolete]
        public IList<Ability> UsableAbilities(Mob mob) {
            return mob.Abilities.Where(ability => IsAbilityUsable(mob, ability)).ToList();
        }

        public bool IsAbilityUsable(Mob mob, Ability ability) {
            return mob.AP >= ability.Cost && ability.CurrentCooldown == 0;
        }

        public IList<UsableAbility> UsableAbilities(Mob mob, Mob target) {
            int distance = Pathfinder.Distance(target.Coord);

            return mob.Abilities
                .Select((ability, i) => new UsableAbility(mob, target, ability, i))
                .Where(ua => ua.Ability.Range >= distance && IsAbilityUsable(mob, ua.Ability))
                .ToList();
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            int maxRange = mob.Abilities
                .Where(ability => IsAbilityUsable(mob, ability))
                .Max(ability => ability.Range);

            return MobManager
                .Mobs
                .Where(m => m != mob && Pathfinder.Distance(m.Coord) <= maxRange)
                .ToList();
        }

        public IList<Mob> Enemies(Mob mob) {
            return MobManager.Mobs.Where(enemy => !enemy.Team.Equals(mob.Team)).ToList();
        }
    }
}