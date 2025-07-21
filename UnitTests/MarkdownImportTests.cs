namespace AssimilationSoftware.TodoSort.Core.Import
{
    using System;
    using System.IO;
    using AssimilationSoftware.Maroon.Model;
    using System.Linq;
    using System.IO.Abstractions.TestingHelpers;

    public class MarkdownImportTests
    {
        [Fact]
        public void GetAllItems_ShouldReturnActionItems_WhenMarkdownLinksPresent()
        {
            // Arrange
            MockFileSystem fileSystem = new();
            var importer = new MarkdownImporter("test.md", fileSystem);
            fileSystem.File.WriteAllText(importer.Filename, "[Task 1](context1)\n[\"Task 2\"](context2)\n");

            // Act
            var items = importer.GetAllItems().ToList();

            // Assert
            Assert.Equal(2, items.Count);
            Assert.Equal("Task 1", items[0].Title);
            Assert.True(items[0].Tags.ContainsKey("url"));
            Assert.Equal("context1", items[0].Tags["url"]);
            Assert.Equal("Task 2", items[1].Title);
            Assert.True(items[1].Tags.ContainsKey("url"));
            Assert.Equal("context2", items[1].Tags["url"]);
        }
    }
}