using System.Collections.Specialized;
using System.Media;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
namespace HexMage.Simulator
{
    public class Path
    {
        public AxialCoord? Source { get; set; }
        public VertexState State { get; set; }
        public int Distance { get; set; }
        public bool Reachable { get; set; }

        public override string ToString() {
            return $"{Source} - {State} - {Distance}";
        }
    }
}