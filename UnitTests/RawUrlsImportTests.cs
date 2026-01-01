using System.IO.Abstractions;
using System.Text;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using UnitTests.Scaffolding;

namespace UnitTests;

public class RawUrlImportTests
{
    [Fact]
    public void Test_Update_Existing_Item()
    {
        // Arrange
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        UpdateSubOptions options = new()
        {
            Context = "inbox",
            Filename = "c:\\Downloads\\links.csv",
            Format = "urls",
            FileSystem = mockFileSystem
        };
        System.IO.Abstractions.TestingHelpers.MockFileData mockFile = new(
            "\"URL\"\n" +
            "\"https://theoatmeal.com:443/\"\n" +
            "\"https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts\"\n" +
            "\"http://www.youtube.com/watch?v=example\"\n"
        );
        mockFileSystem.AddFile("c:\\Downloads\\links.csv", mockFile);
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
            item.Title == "https://theoatmeal.com:443/" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "https://theoatmeal.com:443/"
        );
        // The Ganxy entry should be unchanged.
        Assert.Contains(mockRepository.Items, item =>
            item.Title == "Robert Brockway â Rx - Episode 1: The Blackouts" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts"
        );
        // The YouTube entry, already done, should remain unchanged.
        Assert.Contains(mockRepository.DoneItems, item =>
            item.Title == "YouTube Video" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "http://www.youtube.com/watch?v=example"
        );
    }

    [Fact]
    public void No_Error_On_Missing_Column()
    {
        // Arrange
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        UpdateSubOptions options = new()
        {
            Context = "inbox",
            Filename = "c:\\Downloads\\links.csv",
            Format = "urls",
            FileSystem = mockFileSystem
        };
        System.IO.Abstractions.TestingHelpers.MockFileData mockFile = new(
            "\"Link\"\n" +
            "\"https://theoatmeal.com:443/\"\n" +
            "\"https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts\"\n" +
            "\"http://www.youtube.com/watch?v=example\"\n"
        );
        mockFileSystem.AddFile("c:\\Downloads\\links.csv", mockFile);
        MockTodoRepository mockRepository = new(mockFileSystem);
        ViewModel viewModel = new(mockRepository);

        // Act & Assert
        AssimilationSoftware.TodoSort.CLI.Program.Update(options, viewModel);
    }
}