//#define XML

#define DOT
using System;
using System.IO;
using System.Text;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class UctDebug {
        public static void PrintDotgraph(UctNode root, Func<int> indexFunc = null) {
            var builder = new StringBuilder();

            builder.AppendLine("digraph G {");
            int budget = 5;
            PrintDotNode(builder, root, budget);
            builder.AppendLine("}");

            string str = builder.ToString();

            string dirname = @"data\graphs";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            int index = indexFunc == null ? UctAlgorithm.SearchCount : indexFunc();

            File.WriteAllText($@"data\graphs\graph{index}.dot", str);
        }

        private static void PrintDotNode(StringBuilder builder, UctNode node, int budget) {
            if (budget == 0) return;

            foreach (var child in node.Children) {
                builder.AppendLine($"\"{node}\" -> \"{child}\"");

                string color;
                var teamColor = child.State.CurrentTeam;

                if (teamColor.HasValue) {
                    color = teamColor.Value == TeamColor.Red ? "pink" : "lightblue";
                } else {
                    color = "gray";
                }
                builder.AppendLine($"\"{child}\" [fillcolor = {color}, style=filled]");
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