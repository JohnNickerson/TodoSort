using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    result.AppendLine(string.Format("\t\t\"returnDate\": \"{0:yyyy-MM-dd}\",", n.TickleDate.Value));
                }
                var actualNotes = n.Notes.Where(l => l.Length > 0);
                if (actualNotes.Any())
                {
                    result.AppendLine($"\t\t\"notes\": [");
                    result.Append("\t\t\t");
                    result.AppendLine(String.Join(",\n\t\t\t", actualNotes.Select(line => $"\"{line}\"")));
                    result.AppendLine("\t\t],");
                }
                if (n.Tags.Count > 0)
                {
                    result.AppendLine("\t\t\"tags\": {");
                    result.Append("\t\t\t");
                    result.AppendLine(String.Join(",\n\t\t\t", n.Tags.Select(k => $"\"{k.Key}\": \"{k.Value}\"")));
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