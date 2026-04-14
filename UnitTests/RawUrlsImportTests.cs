using System.IO.Abstractions;
using System.Text;
using AssimilationSoftware.TodoSort.Core.Import;

namespace UnitTests;

public class RawUrlImportTests
{
    [Fact]
    public void Test_ImportItems_CreatesActionItemsFromUrls()
    {
        // Arrange
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        System.IO.Abstractions.TestingHelpers.MockFileData mockFile = new(
            "\"URL\"\n" +
            "\"https://theoatmeal.com:443/\"\n" +
            "\"https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts\"\n" +
            "\"http://www.youtube.com/watch?v=example\"\n"
        );
        mockFileSystem.AddFile("c:\\Downloads\\links.csv", mockFile);
        
        var importer = new RawUrlsImporter("c:\\Downloads\\links.csv", mockFileSystem);

        // Act
        var result = importer.GetAllItems().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        
        // Check that URLs are set correctly
        Assert.Contains(result, item =>
            item.Title == "https://theoatmeal.com:443/" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "https://theoatmeal.com:443/"
        );
        
        Assert.Contains(result, item =>
            item.Title == "https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "https://ganxy.com/i/48595/robert-brockway/rx-episode-1-the-blackouts"
        );
        
        Assert.Contains(result, item =>
            item.Title == "http://www.youtube.com/watch?v=example" &&
            item.Tags.ContainsKey("url") &&
            item.Tags["url"] == "http://www.youtube.com/watch?v=example"
        );
        
        // Check that all items have import context
        foreach (var item in result)
        {
            Assert.Equal("import", item.Context);
        }
    }

    [Fact]
    public void Test_ImportItems_UsesTitleWhenProvided()
    {
        // Arrange
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        System.IO.Abstractions.TestingHelpers.MockFileData mockFile = new(
            "\"URL\",\"Title\"\n" +
            "\"https://theoatmeal.com:443/\",\"The Oatmeal\"\n" +
            "\"https://example.com\",\"\"\n"
        );
        mockFileSystem.AddFile("c:\\Downloads\\links.csv", mockFile);
        
        var importer = new RawUrlsImporter("c:\\Downloads\\links.csv", mockFileSystem);

        // Act
        var result = importer.GetAllItems().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        
        // First item should use title
        Assert.Contains(result, item =>
            item.Title == "The Oatmeal" &&
            item.Tags["url"] == "https://theoatmeal.com:443/"
        );
        
        // Second item should use URL as title
        Assert.Contains(result, item =>
            item.Title == "https://example.com" &&
            item.Tags["url"] == "https://example.com"
        );
    }

    [Fact]
    public void Test_ImportItems_SkipsMissingUrlColumn()
    {
        // Arrange
        var mockFileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        System.IO.Abstractions.TestingHelpers.MockFileData mockFile = new(
            "\"Link\"\n" +
            "\"https://theoatmeal.com:443/\"\n" +
            "\"https://ganxy.com/i/48595\"\n"
        );
        mockFileSystem.AddFile("c:\\Downloads\\links.csv", mockFile);
        
        var importer = new RawUrlsImporter("c:\\Downloads\\links.csv", mockFileSystem);

        // Act
        var result = importer.GetAllItems().ToList();

        // Assert - should import nothing since URL column is missing
        Assert.Empty(result);
    }
}
