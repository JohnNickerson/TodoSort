using System.Text;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using UnitTests.Scaffolding;

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

    [Fact]
    public void Test_Update_Existing_Item()
    {
        // Arrange
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        UpdateSubOptions options = new()
        {
            Context = "instapaper",
            Filename = "c:\\Downloads\\instapaper.csv",
            Format = "instapaper",
            FileSystem = mockFileSystem
        };
        System.IO.Abstractions.TestingHelpers.MockFileData mockFile = new(
            "URL,Title,Selection,Folder,Timestamp,Tags\n" +
            "https://theoatmeal.com:443/,Cyberlove,,Unread,1747964343,[]\n" +
            "https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts,Robert Brockway â Rx - Episode 1: The Blackouts,,Unread,1747964366,[]\n" +
            "http://www.lifehacker.com.au/2010/10/automate-just-about-anything-on-your-windows-pc-no-coding-required/,\"Automate Just About Anything On Your Windows PC, No Coding Required | Lifehacker Australia\",,Archive,1339641431,[]\n" +
            "http://www.youtube.com/watch?v=example,YouTube Video,,Unread,1747964343,[]\n"
        );
        mockFileSystem.AddFile("c:\\Downloads\\instapaper.csv", mockFile);
        MockTodoRepository mockRepository = new(mockFileSystem);
        mockRepository.Create(new AssimilationSoftware.Maroon.Model.ActionItem
        {
            ID = Guid.NewGuid(),
            Title = "Robert Brockway â Rx - Episode 1: The Blackouts",
            Tags = new Dictionary<string, string> { { "url", "https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts" } },
            IsDeleted = false
        });
        mockRepository.Create(new AssimilationSoftware.Maroon.Model.ActionItem
        {
            ID = Guid.NewGuid(),
            Title = "Automate Just About Anything On Your Windows PC, No Coding Required | Lifehacker Australia",
            Tags = new Dictionary<string, string> { { "url", "http://www.lifehacker.com.au/2010/10/automate-just-about-anything-on-your-windows-pc-no-coding-required/" } },
            IsDeleted = false
        });
        mockRepository.Create(new AssimilationSoftware.Maroon.Model.ActionItem
        {
            ID = Guid.NewGuid(),
            Title = "YouTube Video",
            Tags = new Dictionary<string, string> { { "url", "http://www.youtube.com/watch?v=example" } },
            IsDeleted = false,
            Done = true
        });
        ViewModel viewModel = new(mockRepository);

        // Act
        AssimilationSoftware.TodoSort.CLI.Program.Update(options, viewModel);

        // Assert
        // The Oatmeal entry should be added.
        Assert.Contains(mockRepository.Items, item =>
            item.Title == "Cyberlove" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "https://theoatmeal.com:443/"
        );
        // The Ganxy entry should be unchanged.
        Assert.Contains(mockRepository.Items, item =>
            item.Title == "Robert Brockway â Rx - Episode 1: The Blackouts" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts"
        );
        // The Lifehacker entry should be marked as done.
        Assert.Contains(mockRepository.DoneItems, item =>
            item.Title == "Automate Just About Anything On Your Windows PC, No Coding Required | Lifehacker Australia" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "http://www.lifehacker.com.au/2010/10/automate-just-about-anything-on-your-windows-pc-no-coding-required/"
        );
        // The YouTube entry, already done, should remain unchanged.
        Assert.Contains(mockRepository.DoneItems, item =>
            item.Title == "YouTube Video" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "http://www.youtube.com/watch?v=example"
        );
    }
}