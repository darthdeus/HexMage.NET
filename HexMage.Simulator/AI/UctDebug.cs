//#define XML
#define DOT
using System;
using System.IO;
using System.Text;

namespace HexMage.Simulator {
    public class UctDebug {
        public static void PrintDotgraph(UctNode root, Func<int> indexFunc = null) {
            var builder = new StringBuilder();

            builder.AppendLine("digraph G {");
            int budget = 4;
            PrintDotNode(builder, root, budget);
            builder.AppendLine("}");

            string str = builder.ToString();

            string dirname = @"c:\dev\graphs";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            int index = indexFunc == null ? UctAlgorithm.SearchCount : indexFunc();

            File.WriteAllText($@"graph{index}.dot", str);
        }

        private static void PrintDotNode(StringBuilder builder, UctNode node, int budget) {
            if (budget == 0) return;

            foreach (var child in node.Children) {
                builder.AppendLine($"\"{node}\" -> \"{child}\"");
            }

            foreach (var child in node.Children) {
                PrintDotNode(builder, child, budget - 1);
            }
        }


        public static void PrintTreeRepresentation(UctNode root) {
#if XML
            var dirname = @"c:\dev\graphs\xml\";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            string filename = dirname + "iter-" + searchCount + ".xml";

            using (var writer = new StreamWriter(filename)) {
                new XmlTreePrinter(root).Print(writer);
            }
#endif
#if DOT
            PrintDotgraph(root);            
#endif
        }
    }
}