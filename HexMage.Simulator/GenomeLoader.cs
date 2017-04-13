using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public static class GenomeLoader {
#warning TODO: pridat do Constants
        const int minDmg = 5;

        public static Team FromDna(DNA dna) {
            var team = new Team();

            for (int i = 0; i < dna.MobCount; i++) {
                int mobOffset = i * dna.MobSize;

                var mob = new JsonMob();
                mob.hp = (int) Math.Ceiling(dna.Data[mobOffset] * Constants.HpMax);
                mob.ap = (int) Math.Ceiling(dna.Data[mobOffset + 1] * Constants.ApMax);

                for (int j = 0; j < dna.AbilityCount; j++) {
                    int offset = mobOffset + DNA.MobAttributeCount + j * DNA.AbilityAttributeCount;

                    int dmg = (int) Math.Round(dna.Data[offset + 0] * (Constants.DmgMax - minDmg) + minDmg);
                    var ability = new JsonAbility(dmg,
                                                  (int) Math.Round(dna.Data[offset + 1] * Constants.CostMax),
                                                  (int) Math.Round(dna.Data[offset + 2] * Constants.RangeMax),
                                                  0,
                                                  ElementFromNumber(dna.Data[offset + 3]));
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
                dna.Data.Add(mob.hp / (float) Constants.HpMax);
                dna.Data.Add(mob.ap / (float) Constants.ApMax);

                foreach (var ability in mob.abilities) {
                    dna.Data.Add((ability.dmg - minDmg) / (float) Constants.DmgMax);
                    dna.Data.Add(ability.ap / (float) Constants.CostMax);
                    dna.Data.Add(ability.range / (float) Constants.RangeMax);
                    dna.Data.Add(NumberFromElement(ability.element));
                }
            }

            return dna;
        }

        public static bool IsNear(float x, float n) {
            float delta = 0.05f;
            return Math.Abs(x - n) < delta;
        }

        public static AbilityElement ElementFromNumber(float x) {
            if (IsNear(x, 0)) {
                return AbilityElement.Fire;
            } else if (IsNear(x, 0.25f)) {
                return AbilityElement.Earth;
            } else if (IsNear(x, 0.5f)) {
                return AbilityElement.Air;
            } else if (IsNear(x, 0.75f)) {
                return AbilityElement.Water;
            } else {
                throw new InvalidEnumArgumentException($"Invalid value of {x} doesn't match any conversion.");
            }
        }

        public static float NumberFromElement(AbilityElement element) {
            switch (element) {
                case AbilityElement.Fire:
                    return 0;
                case AbilityElement.Earth:
                    return 0.25f;
                case AbilityElement.Air:
                    return 0.5f;
                case AbilityElement.Water:
                    return 0.75f;
                default:
                    throw new InvalidEnumArgumentException($"Invalid value of {element} doesn't match any conversion.");
            }
        }
    }
}