using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.Maroon.Mappers.Csv;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    /// <summary>
    /// Raw URLs Importer
    /// </summary>
    /// <remarks>
    /// This class is responsible for importing items from a CSV file containing raw URLs.
    /// </remarks>
    public class RawUrlsImporter : IImporter
    {
        public string Filename { get; set; }
        private IFileSystem _fileSystem;

        public RawUrlsImporter(string filename) : this(filename, new FileSystem())
        {
        }

        public RawUrlsImporter(string filename, IFileSystem fileSystem)
        {
            Filename = filename;
            _fileSystem = fileSystem;
        }

        public IEnumerable<ActionItem> GetAllItems()
        {
            var result = new List<ActionItem>();
            if (_fileSystem.File.Exists(Filename))
            {
                var itemSource = new CsvReader(Filename, _fileSystem);
                var items = itemSource.GetAllItems();

                foreach (var item in items)
                {
                    if (!item.ContainsKey("URL") || string.IsNullOrWhiteSpace(item["URL"]))
                    {
                        // Skipping items without URLs
                        continue;
                    }

                    var url = item["URL"];
                    bool hasTitle = item.ContainsKey("Title") && !string.IsNullOrWhiteSpace(item["Title"]);

                    var actionItem = new ActionItem
                    {
                        ID = Guid.NewGuid(),
                        Title = hasTitle ? item["Title"] : url,
                        Context = "import",
                        Tags = new Dictionary<string, string>
                        {
                            { "url", url },
                        },
                        LastModified = DateTime.Now,
                        RevisionGuid = Guid.NewGuid(),
                        ImportHash = url.CalculateHash(),
                        IsDeleted = false
                    };
                    result.Add(actionItem);
                }
            }
            return result.ToArray();
        }

        public bool IsValid => _fileSystem.File.Exists(Filename);
    }
}
