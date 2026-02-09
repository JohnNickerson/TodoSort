using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using AssimilationSoftware.Maroon.Mappers.Csv;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public class MarkdownImporter : IImporter
    {
        private IFileSystem _fileSystem;

        public string Filename { get; set; }
        private Regex markdownLinks = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);

        public MarkdownImporter(string filename, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? new FileSystem();
            Filename = filename;
        }

        public IEnumerable<ActionItem> GetAllItems()
        {
            foreach (var line in _fileSystem.File.ReadLines(Filename))
            {
                var match = markdownLinks.Match(line);
                if (match.Success)
                {
                    var title = match.Groups[1].Value.Replace("\"", string.Empty).Trim();
                    var url = match.Groups[2].Value;

                    var item = new ActionItem
                    {
                        Title = title,
                        Tags = new Dictionary<string, string>{ { "url", url }},
                        ImportHash = match.Value.CalculateHash(),
                        LastModified = DateTime.Now
                    };

                    yield return item;
                }
            }
        }

        public bool IsValid => _fileSystem.File.Exists(Filename);
    }
}

