using System.Text;

namespace UnitTests;

public class InstapaperImportTests
{
    [Fact]
    public void Test_Constructor()
    {
        // Arrange
        var instapaperImport = new AssimilationSoftware.TodoSort.Core.Import.InstapaperImporter("instapaper.csv");

        // Act
        var result = instapaperImport;

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Test_GetAllItems()
    {
        // Arrange
        var instapaperImport = new AssimilationSoftware.TodoSort.Core.Import.InstapaperImporter("instapaper.csv");

        // Act
        var result = instapaperImport.GetAllItems();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<AssimilationSoftware.Maroon.Model.ActionItem[]>(result);
    }

    [Fact]
    public void Test_Load_From_File()
    {
        // Arrange
        System.IO.Abstractions.TestingHelpers.MockFileSystem fileSystem = new();
        var fileName = "D:\\Temp\\TodoSortTests\\instapaper.csv";
        var instapaperImport = new AssimilationSoftware.TodoSort.Core.Import.InstapaperImporter(fileName, fileSystem);

        // Create a mock CSV file with test data
        var csvContent = new StringBuilder();
        csvContent.AppendLine("URL,Title,Selection,Folder,Timestamp,Tags");
        csvContent.AppendLine("https://theoatmeal.com:443/,,,Unread,1747964343,[]");
        csvContent.AppendLine("https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts,Robert Brockway â Rx - Episode 1: The Blackouts,,Unread,1747964366,[]");
        csvContent.AppendLine("http://www.lifehacker.com.au/2010/10/automate-just-about-anything-on-your-windows-pc-no-coding-required/,\"Automate Just About Anything On Your Windows PC, No Coding Required | Lifehacker Australia\",,Archive,1339641431,[]");
        fileSystem.AddDirectory("D:\\Temp\\TodoSortTests");
        fileSystem.AddFile(fileName, new System.IO.Abstractions.TestingHelpers.MockFileData(csvContent.ToString()));

        // Act
        var result = instapaperImport.GetAllItems();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<AssimilationSoftware.Maroon.Model.ActionItem[]>(result);
        Assert.Equal(3, result.Length);
        foreach (var item in result)
        {
            Assert.NotNull(item);
            Assert.False(item.IsDeleted);
            Assert.NotEmpty(item.Title);
            Assert.NotEmpty(item.Tags);
            Assert.Contains("url", item.Tags.Keys);
        }
    }
}