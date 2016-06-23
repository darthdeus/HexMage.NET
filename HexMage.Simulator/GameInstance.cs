using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HexMage.Simulator
{
    public class GameInstance
    {
        public Map Map { get; set; }
        public MobManager MobManager { get; set; }
        public Pathfinder Pathfinder { get; set; }
        public TurnManager TurnManager { get; set; }
        public int Size { get; set; }

        public GameInstance(int size) {
            Size = size;
            MobManager = new MobManager();
            Map = new Map(size);
            Pathfinder = new Pathfinder(Map, MobManager);
            TurnManager = new TurnManager(MobManager);
        }


        public bool IsFinished() {
#if DEBUG
            Debug.Assert(MobManager.Teams.All(team => team.Mobs.Count > 0));
#endif
            return MobManager.Teams.Any(team => team.Mobs.All(mob => mob.HP == 0));
        }

        public void Refresh() {
            Pathfinder.PathfindFrom(TurnManager.CurrentMob.Coord);
        }

        public IList<Ability> UsableAbilities(Mob mob) {
            return mob.Abilities.Where(ability => ability.Cost <= mob.AP).ToList();
        }

        public IList<UsableAbility> UsableAbilities(Mob mob, Mob target) {
            int distance = Pathfinder.Distance(target.Coord);

            return mob.Abilities
                .Where(ability => ability.Range >= distance && mob.AP >= ability.Cost)
                .Select(ability => new UsableAbility(mob, target, ability))
                .ToList();
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            int maxRange = mob.Abilities.Max(ability => ability.Range);

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