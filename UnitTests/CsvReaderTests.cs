using System.Text;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class CsvReaderTests
{
    [Fact]
    public void ReadCsvFile()
    {
        // Arrange
        var csvPath = ".\\instapaper.csv";
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine(@"URL,Title,Selection,Folder,Timestamp,Tags
https://www.cracked.com/image-pictofact-15636-40-highly-volatile-bits-of-pop-culture-trivia-we-didnt-know-were-in-this-dark-cave-until-we-lit-up-a-big-match-to-get-our-bearings,40 Highly Volatile Bits of Pop-Culture Trivia We Didn’t Know Were in This Dark Cave Until We Lit Up a Big Match to Get Our Bearings,,Unread,1748490183,[]
https://www.cracked.com/article_46763_simon-pegg-regrets-roasting-jar-jar-binks.html,Simon Pegg Regrets Roasting Jar Jar Binks,,Unread,1748490176,[]
https://www.justwatch.com/au/movie/testament,Testament - movie: where to watch stream online,,Unread,1748489934,[]
https://www.justwatch.com/au/movie/thx-1138,THX 1138 - movie: where to watch stream online,,Unread,1748489876,[]
https://youtube.com/watch?v=Cz1XEWqL-Tc&si=0oFufM4HNW3lTiB8,,,Unread,1748482121,[]
https://www.youtube.com/watch?v=0b1d9a2f3c8,The 10 Most Dangerous Places on Earth,,Unread,1748482121,[]");
        fileSystem.AddDirectory(fileSystem.Path.GetDirectoryName(csvPath));
        fileSystem.AddFile(csvPath, new System.IO.Abstractions.TestingHelpers.MockFileData(csvContent.ToString()));
        var csvReader = new CLI.CsvReader(csvPath, fileSystem);

        // Act
        var items = csvReader.GetAllItems();

        // Assert
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        var itemList = items.ToList();
        Assert.Equal(6, itemList.Count);
        Assert.True(itemList[0].ContainsKey("URL"));
    }

    [Fact]
    public void ReadCsvFile_EmptyLines_Skipped()
    {
        // Arrange
        var csvPath = ".\\instapaper_with_empty_lines.csv";
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine(@"URL
https://www.example.com

https://www.example2.com");
        fileSystem.AddDirectory(fileSystem.Path.GetDirectoryName(csvPath));
        fileSystem.AddFile(csvPath, new System.IO.Abstractions.TestingHelpers.MockFileData(csvContent.ToString()));
        var csvReader = new CLI.CsvReader(csvPath, fileSystem);

        // Act
        var items = csvReader.GetAllItems();

        // Assert
        Assert.NotNull(items);
        Assert.NotEmpty(items);
        var itemList = items.ToList();
        Assert.Equal(2, itemList.Count); // Should only have 2 items, empty lines skipped
        Assert.True(itemList[0].ContainsKey("URL"));
    }
}