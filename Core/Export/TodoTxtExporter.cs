using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Export;

public class TodoTxtExporter : IExporter
{
    public string Filename { get; set; }

    public void Export(List<ActionItem> items)
    {
        // Ensure the file exists.
        var file = new StreamWriter(Filename);

        // Each item goes on one line, so this is easy.
        foreach (var item in items)
        {
            StringBuilder line = new StringBuilder();
            var createdDate = item.Tags.TryGetValue("created-date", out string created) && DateTime.TryParse(created, out var result) ? result : DateTime.Today;
            // TODO: Depth as priority? eg 0 -> (A), 1 -> (B)
            if (item.Done)
            {
                line.Append($"x {item.DoneDate:yyyy-MM-dd} {createdDate:yyyy-MM-dd} ");
            }
            else
            {
                line.Append($"{createdDate:yyyy-MM-dd} ");
            }
            line.Append(item.Title);
            if (item.ProjectId.HasValue)
            {
                // TODO: use CamelCase project title if known
                line.Append($" +{item.ProjectId}");
            }
            line.Append($" @{item.Context}");
            foreach (var tag in item.Tags.Where(t => t.Key != "created-date"))
            {
                line.Append($" {tag.Key}:{Sanitise(tag.Value)}");
            }
            file.WriteLine(line.ToString());
        }
        file.Close();
    }

    private string Sanitise(string tagValue)
    {
        return tagValue.Replace(":", "").Replace(" ", "");
    }
}