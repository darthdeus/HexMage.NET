//#define XML

#define DOT

using System;
using System.IO;
using System.Text;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;

namespace HexMage.Simulator {
    public class UctDebug {
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

        public static void PrintDotgraph(UctNode root, Func<int> indexFunc = null) {
            var builder = new StringBuilder();

            int budget = 4;

            builder.AppendLine("digraph G {");
            PrintDotNode(builder, null, root, budget);
            builder.AppendLine("}");

            string str = builder.ToString();

            string dirname = @"data\graphs";
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }

            int index = indexFunc == null ? UctAlgorithm.SearchCount : indexFunc();

            File.WriteAllText($@"data\graphs\graph{index.ToString("00000")}.dot", str);
        }

        private static void PrintDotNode(StringBuilder builder, UctNode parent, UctNode node, int budget) {
            if (budget == 0) return;
            //if (node.N < 8) return;

            string color;
            var teamColor = node.State.CurrentTeam;

            if (teamColor.HasValue) {
                if (node.Action.Type == UctActionType.EndTurn) {
                    var lastTeamColor = node.State.State.LastTeamColor;

                    if (lastTeamColor == teamColor) {
                        color = teamColor.Value == TeamColor.Red ? "pink" : "lightblue";
                    } else {
                        color = teamColor.Value == TeamColor.Red ? "lightblue" : "pink";
                    }
                } else {
                    color = teamColor.Value == TeamColor.Red ? "pink" : "lightblue";
                }
            } else {
                color = "yellow";
            }

            builder.AppendLine($"\"{node}\" [fillcolor = {color}, style=filled]");

            if (parent != null) {
                builder.AppendLine($"\"{parent}\" -> \"{node}\"");
            }

            foreach (var child in node.Children) {
                PrintDotNode(builder, node, child, budget - 1);
            }
        }
    }
}