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
    public class HtmlExporter : IPimDataMapper<ActionItem>
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
            result.AppendLine("<html><body>");
            foreach (var n in items)
            {
                var line = n.Title;
                if (n.Tags.ContainsKey("url"))
                {
                    result.AppendLine(string.Format("<input type=\"checkbox\"><a href=\"{0}\">{1}</a></input><br />", n.Tags["url"], line));
                }
                else
                {
                    result.AppendLine(string.Format("<input type=\"checkbox\">{0}</input><br />", line));
                }
            }
            result.AppendLine("</body></html>");

            File.WriteAllText(Filename, result.ToString());
        }
    }
}
