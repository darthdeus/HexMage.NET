using System;

namespace HexMage.GUI {
    public static class Program {
        [STAThread]
        static void Main() {
            using (var game = new HexMageGame())
                game.Run();
        }
    }
}