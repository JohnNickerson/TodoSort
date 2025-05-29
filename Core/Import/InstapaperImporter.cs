using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AssimilationSoftware.Maroon.Mappers.Csv;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Import;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    /// <summary>
    /// Instapaper Importer
    /// </summary>
    /// <remarks>
    /// This class is responsible for importing items from Instapaper.
    /// </remarks>
    public class InstapaperImporter : IImporter
    {
        public bool IsValid => throw new System.NotImplementedException();
        public string Filename { get; set; }
        private System.IO.Abstractions.IFileSystem _fileSystem;

        public InstapaperImporter(string fileName) : this(fileName, new System.IO.Abstractions.FileSystem())
        {
        }

        public InstapaperImporter(string fileName, System.IO.Abstractions.IFileSystem fileSystem)
        {
            Filename = fileName;
            _fileSystem = fileSystem;
        }

        public ActionItem[] GetAllItems()
        {
            var result = new List<ActionItem>();
            if (_fileSystem.File.Exists(Filename))
            {
                var lines = _fileSystem.File.ReadAllLines(Filename).Skip(1);
                foreach (var line in lines)
                {
                    var tokens = line.Tokenise();
                    if (Validate(tokens))
                    {
                        // Parse out the details.
                        // URL,Title,Selection,Folder,Timestamp,Tags
                        var item = new ActionItem
                        {
                            Context = "instapaper",
                            ID = Guid.NewGuid(),
                            ImportHash = line.CalculateHash(),
                            IsDeleted = false,
                            LastModified = DateTime.Now,
                            RevisionGuid = Guid.NewGuid(),
                            Title = string.IsNullOrWhiteSpace(tokens[1]) ? tokens[0] : tokens[1],
                            Tags = new Dictionary<string, string>()
                            {
                                { "url", tokens[0] },
                            }
                        };
                        result.Add(item);
                    }
                    else
                    {
                        // Log the error.
                        System.Diagnostics.Debug.WriteLine($"Invalid line: {line}");
                    }
                }
                return result.ToArray();
            }
            return new ActionItem[] { };
        }

        public bool Validate(IEnumerable<string> tokens)
        {
            // Validate the tokens based on the expected format.
            // URL,Title,Selection,Folder,Timestamp,Tags
            return tokens != null && tokens.Count() == 6 && !string.IsNullOrWhiteSpace(tokens.ElementAt(0));
        }
    }
}