using AssimilationSoftware.TodoSort.CoreGui.Model;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class FileLifecycleStateTests
{
    [Fact]
    public void TrackFile_Records_The_File_And_Resets_It_To_Clean()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "todos.txt");
            File.WriteAllText(filePath, "@inbox");

            var lifecycle = new FileLifecycleState();
            lifecycle.Track(filePath);

            Assert.True(lifecycle.IsTracked);
            Assert.False(lifecycle.IsDirty);
            Assert.Equal(filePath, lifecycle.FilePath);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void RecentFiles_Are_Ordered_With_The_Newest_File_First_And_Capped_At_Ten()
    {
        var fileA = "C:/files/a.txt";
        var fileB = "C:/files/b.txt";
        var fileC = "C:/files/c.txt";

        var ordered = FileLifecycleState.OrderRecentFiles(new[] { fileA, fileB, fileC }, fileB);

        Assert.Equal(new[] { fileB, fileA, fileC }, ordered);

        var many = Enumerable.Range(1, 12).Select(i => $"C:/files/{i}.txt").ToArray();
        var capped = FileLifecycleState.OrderRecentFiles(many, "C:/files/7.txt");

        Assert.Equal(10, capped.Length);
        Assert.Equal("C:/files/7.txt", capped[0]);
    }

    [Fact]
    public void MarkDirty_And_MarkSaved_Reflect_The_Current_Lifecycle_State()
    {
        var lifecycle = new FileLifecycleState();

        lifecycle.MarkDirty();
        Assert.True(lifecycle.IsDirty);

        lifecycle.MarkSaved();
        Assert.False(lifecycle.IsDirty);
    }
}
