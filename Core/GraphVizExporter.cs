using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core
{
    public class GraphVizExporter : IPimDataMapper<ActionItem>
    {
        public string Filename { get; set; }

        public ActionItem Load(Guid id)
        {
            throw new NotImplementedException();
        }

        public List<ActionItem> LoadAll()
        {
            throw new NotImplementedException();
        }

        public void Save(ActionItem item)
        {
            throw new NotImplementedException();
        }

        public void SaveAll(List<ActionItem> items)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("digraph {");
            foreach (var n in items)
            {
                var line = n.Title.Replace("\"", "");
                result.AppendLine(string.Format("    ID{0} [label=\"{1}\"];", n.ID.ToString().Replace("-", ""), line));
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
