using System;

namespace HexMage.GUI {
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program {
        [STAThread]
        static void Main() {
            using (var game = new HexMageGame())
                game.Run();
        }
    }
}