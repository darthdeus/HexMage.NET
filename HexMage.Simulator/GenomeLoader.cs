using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using HexMage.Simulator.Model;
using MathNet.Numerics.LinearAlgebra.Single;

namespace HexMage.Simulator {
    /// <summary>
    /// Helpers for converting between DNA and Team.
    /// </summary>
    public static class GenomeLoader {
        const int minDmg = 5;

        public static Team FromDna(DNA dna) {
            var team = new Team();

            for (int i = 0; i < dna.MobCount; i++) {
                int mobOffset = i * dna.MobSize;

                var mob = new JsonMob();
                mob.hp = (int) Math.Round(dna.Data[mobOffset] * Constants.HpMax);
                mob.ap = (int) Math.Round(dna.Data[mobOffset + 1] * Constants.ApMax);

                for (int j = 0; j < dna.AbilityCount; j++) {
                    int offset = mobOffset + DNA.MobAttributeCount + j * DNA.AbilityAttributeCount;

                    int dmg = (int) Math.Round(dna.Data[offset + 0] * (Constants.DmgMax - minDmg) + minDmg);
                    int cost = (int) Math.Round(dna.Data[offset + 1] * Constants.CostMax);
                    int range = (int) Math.Round(dna.Data[offset + 2] * Constants.RangeMax);
                    int cooldown = (int) Math.Round(dna.Data[offset + 3] * Constants.CooldownMax);

                    int buffDmg = (int) Math.Round(dna.Data[offset + 4] * Constants.BuffDmgMax);
                    int buffApDmg = (int) Math.Round(dna.Data[offset + 5] * Constants.BuffApDmgMax);
                    int buffLifetime = (int) Math.Round(dna.Data[offset + 6] * Constants.BuffLifetimeMax);

                    int radius = (int) Math.Round(dna.Data[offset + 7] * Constants.BuffMaxRadius);
                    int areaBuffDmg = (int) Math.Round(dna.Data[offset + 8] * Constants.BuffDmgMax);
                    int areaBuffApDmg = (int) Math.Round(dna.Data[offset + 9] * Constants.BuffApDmgMax);
                    int areaBuffLifetime = (int) Math.Round(dna.Data[offset + 10] * Constants.BuffLifetimeMax);

                    var buff = new Buff(-buffDmg,
                                        -buffApDmg,
                                        buffLifetime);

                    var areaEffect = new Buff(-areaBuffDmg,
                                              -areaBuffApDmg,
                                              areaBuffLifetime);

                    var areaBuff = new AreaBuff(AxialCoord.Zero, radius, areaEffect);

                    var ability = new JsonAbility(dmg,
                                                  cost,
                                                  range,
                                                  cooldown,
                                                  new JsonBuff(buff), 
                                                  new JsonAreaBuff(areaBuff));
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

            var dna = new DNA(mobCount, abilityCount);
            var data = new List<float>();

            foreach (var mob in team.mobs) {
                data.Add(mob.hp / (float) Constants.HpMax);
                data.Add(mob.ap / (float) Constants.ApMax);

                foreach (var ability in mob.abilities) {
                    data.Add((ability.dmg - minDmg) / (float) (Constants.DmgMax - minDmg));
                    data.Add(ability.ap / (float) Constants.CostMax);
                    data.Add(ability.range / (float) Constants.RangeMax);
                    data.Add(ability.cooldown/ (float) Constants.CooldownMax);

                    var buff = ability.buff;
                    data.Add(-buff.HpChange / (float) Constants.BuffDmgMax);
                    data.Add(-buff.ApChange / (float) Constants.BuffApDmgMax);
                    data.Add(buff.Lifetime / (float) Constants.BuffLifetimeMax);

                    var areaBuff = ability.areaBuff;

                    data.Add(areaBuff.Radius / (float) Constants.BuffMaxRadius);
                    data.Add(-areaBuff.Effect.HpChange / (float) Constants.BuffDmgMax);
                    data.Add(-areaBuff.Effect.ApChange / (float) Constants.BuffApDmgMax);
                    data.Add(areaBuff.Effect.Lifetime / (float) Constants.BuffLifetimeMax);
                }
            }

            dna.Data = DenseVector.OfEnumerable(data);

            return dna;
        }

        public static bool IsNear(float x, float n) {
            float delta = 0.05f;
            return Math.Abs(x - n) < delta;
        }        
    }
}