namespace HexMage.Simulator {
    public static class Constants {
        public const int HpMax = 100;
        public const int ApMax = 25;
        public const int DmgMax = 30;
        public const int CostMax = 20;
        public const int RangeMax = 10;
        public const int EvolutionMapSize = 5;

        public static readonly string SaveFile = @"evo-save.txt";
        public static readonly string SaveDir = "save-files/";

        public static string BuildEvoSavePath(int index) {
            return SaveDir + index.ToString() + SaveFile;
        }
    }
}