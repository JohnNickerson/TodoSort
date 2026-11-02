namespace AssimilationSoftware.TodoSort.CoreGui.Model;

public sealed class FileLifecycleState
{
    public const int MaxRecentFiles = 10;

    public string? FilePath { get; private set; }

    public bool IsTracked => !string.IsNullOrWhiteSpace(FilePath);

    public bool IsDirty { get; private set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    public void Track(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        FilePath = filePath;
        MarkSaved();
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkSaved()
    {
        IsDirty = false;
    }

    public void Reset()
    {
        FilePath = null;
        IsDirty = false;
    }

    public static string[] OrderRecentFiles(IEnumerable<string> existingFiles, string? newestFile, int limit = MaxRecentFiles)
    {
        var normalized = existingFiles
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(newestFile))
        {
            normalized.RemoveAll(item => string.Equals(item, newestFile, StringComparison.OrdinalIgnoreCase));
            normalized.Insert(0, newestFile);
        }

        return normalized
            .Take(limit)
            .ToArray();
    }
}
