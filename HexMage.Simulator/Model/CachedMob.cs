namespace HexMage.Simulator.Model {
    // TODO - start using this everywhere
    // TODO - class or struct? benchmark both
    public class CachedMob {
        public readonly int MobId;
        public MobInfo MobInfo;
        public MobInstance MobInstance;

        public CachedMob(GameInstance gameInstance, int mobId) {
            MobId = mobId;

            // TODO: zkontrolovat, jestli to funguje
            if (mobId == -1) return;
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
}