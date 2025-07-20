using AssimilationSoftware.Maroon.Model;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssimilationSoftware.TodoSort.Core.Export
{
    public class HtmlExporter : IExporter
    {
        public string Filename { get; set; }

        public void Export(List<ActionItem> items)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("<html><body>");
            foreach (var n in items)
            {
                var line = n.Title;
                if (n.Tags.ContainsKey("nsfw") && n.Tags["nsfw"].ToLower() == "true")
                {
                    line = "(nsfw) " + Rot13.Transform(line);
                }
                result.Append("<input type=\"checkbox\">");
                if (n.Tags.ContainsKey("url"))
                {
                    result.Append(string.Format("<a href=\"{0}\" target=\"todosort\">{1}</a>", n.Tags["url"], line));
                }
                else
                {
                    result.Append(line);
                }
                if (n.Tags.ContainsKey("type"))
                {
                    result.Append(string.Format(" ({0})", n.Tags["type"]));
                }
                result.AppendLine("</input><br />");
            }
            result.AppendLine("</body></html>");

            File.WriteAllText(Filename, result.ToString());
        }
    }
}
