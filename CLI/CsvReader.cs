// This class has been moved to Core/CsvReader.cs
// Keeping this file for backward compatibility during transition
using AssimilationSoftware.TodoSort.Core;

namespace AssimilationSoftware.TodoSort.CLI
{
    // Alias for backward compatibility
    public class CsvReader : AssimilationSoftware.TodoSort.Core.CsvReader
    {
        public CsvReader(string? filename, System.IO.Abstractions.IFileSystem? fileSystem = null)
            : base(filename, fileSystem)
        {
        }
    }
}

