using Spectre.Console;
using AssimilationSoftware.Maroon.Mappers.Csv;

namespace AssimilationSoftware.TodoSort.CLI
{
    public class CsvReader
    {
        private string? filename;
        private System.IO.Abstractions.IFileSystem fileSystem;

        public CsvReader(string? filename, System.IO.Abstractions.IFileSystem? fileSystem = null)
        {
            this.fileSystem = fileSystem ?? new System.IO.Abstractions.FileSystem();
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));
            }
            if (!this.fileSystem.File.Exists(filename))
            {
                throw new FileNotFoundException("CSV file not found.", filename);
            }
            this.filename = filename;
        }

        public IEnumerable<Dictionary<string, string>> GetAllItems()
        {
            if (string.IsNullOrEmpty(filename) || !fileSystem.File.Exists(filename))
            {
                throw new FileNotFoundException("CSV file not found.", filename);
            }

            var lines = fileSystem.File.ReadAllLines(filename);
            // Assuming the first line contains headers.
            if (lines.Length == 0)
            {
                throw new InvalidOperationException("CSV file is empty.");
            }
            var headers = lines[0].Tokenise().Select(h => h.Trim()).ToArray();
            for (var i = 1; i < lines.Length; i++)
            {
                var tokens = lines[i].Tokenise();
                if (tokens.Count != headers.Length)
                {
                    throw new InvalidOperationException($"Line {i + 1} does not match header count.");
                }
                var item = new Dictionary<string, string>();
                for (var j = 0; j < headers.Length; j++)
                {
                    item[headers[j]] = tokens[j].Trim();
                }
                yield return item;
            }
        }
    }
}
