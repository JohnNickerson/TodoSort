using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Export
{
    public class GraphVizExporter : IExporter
    {
        public string Filename { get; set; }

        public void Export(List<ActionItem> items)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("digraph {");
            foreach (var n in items)
            {
                var line = n.Title.Replace("\"", "");
                result.AppendLine(string.Format("    ID{0} [label=\"{1}\"];", n.ID.ToString().Replace("-", ""), line.Replace("\"", "")));
                if (n.RankParent != null)
                {
                    result.AppendLine(string.Format("    ID{0} -> ID{1};", n.RankParent.ID.ToString().Replace("-", ""), n.ID.ToString().Replace("-", "")));
                }
                else
                {
                }
            }
            result.AppendLine("}");

            File.WriteAllText(Filename, result.ToString());
        }
    }
}
