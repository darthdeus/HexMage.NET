using System;
using System.Diagnostics;
using System.Linq;

namespace HexMage.Simulator {
    public static class GenomeLoader {
        public static Team FromDna(DNA dna) {
            var team = new Team();

            for (int i = 0; i < dna.MobCount; i++) {
                int mobOffset = i * dna.MobSize;

                var mob = new JsonMob();
                mob.hp = (int) Math.Ceiling(dna.Data[mobOffset] * Constants.HpMax);
                mob.ap = (int) Math.Ceiling(dna.Data[mobOffset + 1] * Constants.ApMax);

                for (int j = 0; j < dna.AbilityCount; j++) {
                    int offset = mobOffset + DNA.MobAttributeCount + j * DNA.AbilityAttributeCount;

                    var ability = new JsonAbility((int) Math.Ceiling(dna.Data[offset + 0] * Constants.DmgMax),
                                                  (int) Math.Ceiling(dna.Data[offset + 1] * Constants.CostMax),
                                                  (int) Math.Ceiling(dna.Data[offset + 2] * Constants.RangeMax),
                                                  0);
                    mob.abilities.Add(ability);
                }

                team.mobs.Add(mob);
            }

            return team;
        }

        public static DNA FromTeam(Team team) {
            var mobCount = team.mobs.Count;
            var abilityCount = team.mobs[0].abilities.Count;

            Debug.Assert(team.mobs.All(m => m.abilities.Count == abilityCount));

            var dna = new DNA();
            dna.MobCount = mobCount;
            dna.AbilityCount = abilityCount;

            foreach (var mob in team.mobs) {
                dna.Data.Add(mob.hp / (float)Constants.HpMax);
                dna.Data.Add(mob.ap / (float)Constants.ApMax);

                foreach (var ability in mob.abilities) {
                    dna.Data.Add(ability.dmg / (float)Constants.DmgMax);
                    dna.Data.Add(ability.ap / (float)Constants.CostMax);
                    dna.Data.Add(ability.range / (float)Constants.RangeMax);
                }
            }

            return dna;
        }
    }
}