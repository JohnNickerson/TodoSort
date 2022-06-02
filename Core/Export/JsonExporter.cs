using AssimilationSoftware.Maroon.Model;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssimilationSoftware.TodoSort.Core.Export
{
    public class JsonExporter : IExporter
    {
        public string Filename { get; set; }

        public void Export(List<ActionItem> items)
        {
            var result = new StringBuilder();
            result.AppendLine("[");
            var first = true;
            foreach (var n in items)
            {
                if (!first)
                {
                    result.AppendLine(",");
                }
                else
                {
                    first = false;
                }
                result.AppendLine("\t{");
                result.AppendLine($"\t\t\"id\": \"{n.ID}\",");
                result.AppendLine($"\t\t\"title\": \"{n.Title}\",");
                result.AppendLine($"\t\t\"context\": \"{n.Context}\",");
                if (n.ParentId.HasValue) result.AppendLine($"\t\t\"parentId\": \"{n.ParentId}\",");
                if (n.ProjectId.HasValue) result.AppendLine($"\t\t\"projectId\": \"{n.ProjectId}\",");
                if (n.Tags?.ContainsKey("type") ?? false)
                {
                    result.AppendLine($"\t\t\"type\": \"{n.Tags["type"]}\",");
                }
                if (n.DoneDate.HasValue)
                {
                    result.AppendLine(string.Format("\t\t\"doneDate\": \"{0:yyyy-MM-dd}\",", n.DoneDate.Value));
                }
                if (n.TickleDate.HasValue)
                {
                    result.AppendLine(string.Format("\t\t\"tickleDate\": \"{0:yyyy-MM-dd}\",", n.TickleDate.Value));
                }
                if (n.Notes.Count > 0)
                {
                    result.AppendLine($"\t\t\"notes\": [");
                    foreach (var a in n.Notes)
                    {
                        result.AppendLine($"\t\t\t\"{a}\",");
                    }
                    result.AppendLine("],");
                }
                if (n.Tags.Count > 0)
                {
                    result.AppendLine("\t\t\"tags\": {");
                    foreach (var k in n.Tags)
                    {
                        result.AppendLine($"\t\t\t\"{k.Key}\": \"{k.Value}\",");
                    }
                    result.AppendLine("\t\t},");
                }
                if (n.Upvotes > 0)
                {
                    result.AppendLine(string.Format("\t\t\"upvotes\": \"{0}\",", n.Upvotes));
                }
                if (n.DoneDate.HasValue)
                {
                    result.AppendLine(string.Format("\t\t\"doneDate\": \"{0:yyyy-MM-dd}\",", n.DoneDate.Value));
                }
                result.AppendLine(string.Format("\t\t\"lastModified\": \"{0:yyyy-MM-dd}\"", n.LastModified));

                result.Append("\t}");
            }
            result.AppendLine();
            result.AppendLine("]");

            File.WriteAllText(Filename, result.ToString());
        }
    }
}