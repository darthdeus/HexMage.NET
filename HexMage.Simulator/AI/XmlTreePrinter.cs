using System;
using System.IO;
using System.Xml;

namespace HexMage.Simulator.AI {
    public class XmlTreePrinter {
        private readonly UctNode _root;
        private XmlDocument _doc;

        public XmlTreePrinter(UctNode root) {
            _root = root;
            _doc = new XmlDocument();
        }

        public void Print(TextWriter writer) {
            var rootElement = PrintNode(_root);
            _doc.AppendChild(rootElement);

            writer.Write(_doc.OuterXml);
        }

        private XmlElement PrintNode(UctNode node) {
            var element = _doc.CreateElement("action");

            foreach (var child in node.Children) {
                var xmlElement = PrintNode(child);
                element.AppendChild(xmlElement);
            }

            element.SetAttribute("Q", node.Q.ToString());
            element.SetAttribute("N", node.N.ToString());
            element.SetAttribute("Action", node.Action.ToString());
            element.SetAttribute("IsTerminal", node.IsTerminal.ToString());

            return element;
        }
    }
}