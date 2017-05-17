namespace HexMage.Simulator.Model {
    public class CachedMob {
        public readonly int MobId;
        public MobInfo MobInfo;
        public MobInstance MobInstance;

        public CachedMob(GameInstance gameInstance, int mobId) {
            MobId = mobId;

            if (mobId == -1) return;
            MobInfo = gameInstance.MobManager.MobInfos[mobId];
            MobInstance = gameInstance.State.MobInstances[mobId];
        }

        public static CachedMob Create(GameInstance gameInstance, int mobId) {
            return new CachedMob(gameInstance, mobId);
        }
    }
}