using AssimilationSoftware.Maroon.Model;

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

    public bool HasChangedOnDisk(string? filePath, DateTime lastKnownWriteTime, int lastKnownChangeCount)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        var currentChangeCount = fileInfo.DirectoryName is not null
            ? Directory.EnumerateFiles(fileInfo.DirectoryName, "*.xml").Count()
            : 0;

        return fileInfo.LastWriteTime != lastKnownWriteTime || currentChangeCount != lastKnownChangeCount;
    }

    public bool RequiresReload(int currentChangeCount, int lastKnownChangeCount, int dirtyState)
    {
        return currentChangeCount != lastKnownChangeCount || dirtyState > 0;
    }

    public IReadOnlyCollection<ActionItem> GetDeferredItemsReadyForReentry(IEnumerable<ActionItem>? items, DateTime? today = null)
    {
        if (items is null)
        {
            return Array.Empty<ActionItem>();
        }

        var referenceDate = today ?? DateTime.Today;
        var seenIds = new HashSet<Guid>();

        return items
            .Where(item => item.TickleDate.HasValue && item.TickleDate.Value <= referenceDate)
            .Where(item => seenIds.Add(item.ID))
            .ToList();
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
