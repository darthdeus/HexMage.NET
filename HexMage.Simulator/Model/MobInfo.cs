using System.Collections.Generic;
using System.Linq;

namespace HexMage.Simulator.Model {
    // TODO - start using this everywhere
    // TODO - class or struct? benchmark both
    public class CachedMob {
        public int MobId;
        public MobInfo MobInfo;
        public MobInstance MobInstance;

        public CachedMob(GameInstance gameInstance, int mobId) {
            MobId = mobId;

            MobInfo = gameInstance.MobManager.MobInfos[mobId];
            MobInstance = gameInstance.State.MobInstances[mobId];
        }


        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu
        // TODO - grafy porovnavajici winrate jednotlivych AI pro ruzne kombo flagu


        public static CachedMob Create(GameInstance gameInstance, int mobId) {
            return new CachedMob(gameInstance, mobId);
        }
    }

    public struct MobInfo {
        // TODO - this is no longer needed
        public static readonly int NumberOfAbilities = 6;

        public int MaxHp { get; set; }
        public int MaxAp { get; set; }
        public int Iniciative { get; set; }

        public List<int> Abilities { get; set; }
        public TeamColor Team { get; set; }
        public AxialCoord OrigCoord;

        // TODO - this is no longer needed
        public static int AbilityCount => 6;

        public MobInfo(TeamColor team, int maxHp, int maxAp, int iniciative, IEnumerable<int> abilities) {
            Team = team;
            MaxHp = maxHp;
            MaxAp = maxAp;
            Iniciative = iniciative;
            Abilities = abilities.ToList();
            OrigCoord = AxialCoord.Zero;
        }

        public bool Equals(MobInfo other) {
            bool abilitiesEqual = Abilities.SequenceEqual(other.Abilities);
            return OrigCoord.Equals(other.OrigCoord) && MaxHp == other.MaxHp && MaxAp == other.MaxAp &&
                   Iniciative == other.Iniciative && abilitiesEqual && Team == other.Team;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MobInfo && Equals((MobInfo) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = OrigCoord.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxHp;
                hashCode = (hashCode * 397) ^ MaxAp;
                hashCode = (hashCode * 397) ^ Iniciative;
                foreach (var ability in Abilities) {
                    hashCode = (hashCode * 397) ^ ability.GetHashCode();
                }
                hashCode = (hashCode * 397) ^ (int) Team;
                return hashCode;
            }
        }

        public static bool operator ==(MobInfo left, MobInfo right) {
            return left.Equals(right);
        }

        public static bool operator !=(MobInfo left, MobInfo right) {
            return !left.Equals(right);
        }
    }
}