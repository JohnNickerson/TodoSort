using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssimilationSoftware.Maroon.Model;
using System.IO;

namespace AssimilationSoftware.TodoSort.Core.Export
{
    public class TemplateExporter : IExporter
    {
        private string templateFilename;
        private string _outputFilename;
        private string _template;
        private Dictionary<string, int> _tagindexes;

        public TemplateExporter(string outfile, string templateFilename)
        {
            _outputFilename = outfile;
            this.templateFilename = templateFilename;
            _template = File.ReadAllText(templateFilename);
            _tagindexes = new Dictionary<string, int>();
            _template = _template.Replace("{{id}}", "{0}");
            _template = _template.Replace("{{title}}", "{1}");
            _template = _template.Replace("{{context}}", "{2}");
            // Find and replace "{{tag:..}}" with a number, and record number in _tagindexes.
            int tagindex = 2;
            while (_template.IndexOf("{{tag:") > 0)
            {
                // Extract the tag name.
                int placeholderloc = _template.IndexOf("{{tag:");
                int taglength = _template.IndexOf("}}", placeholderloc) - placeholderloc - 6;
                string tag = _template.Substring(placeholderloc + 6, taglength);
                // Increment the string format index.
                tagindex++;
                // Replace the placeholder.
                _template = _template.Replace("{{tag:" + tag + "}}", "{" + tagindex + "}");
                // Record the index.
                _tagindexes[tag] = tagindex;
            }
        }

        public void Export(List<ActionItem> items)
        {
            StringBuilder result = new StringBuilder();
            foreach (var i in items)
            {
                // Build an array of tag strings for replacement.
                List<string> tagvals = new List<string>();
                tagvals.Add(i.ID.ToString());
                tagvals.Add(i.Title);
                tagvals.Add(i.Context);
                foreach(var k in _tagindexes.Keys.OrderBy(x => _tagindexes[x]))
                {
                    if (i.Tags.ContainsKey(k))
                    {
                        tagvals.Add(i.Tags[k]);
                    }
                    else
                    {
                        // No tag. Use an empty string.
                        tagvals.Add(string.Empty);
                    }
                }
                // Append the formatted output string.
                result.AppendFormat(_template, tagvals.ToArray());
                result.AppendLine();
            }
            // Write out the file.
            File.WriteAllText(_outputFilename, result.ToString());
        }
    }
}
