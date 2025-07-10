using System.IO.Enumeration;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class EditTests
{
    [Fact]
    public void Adding_Empty_Tag_Set_Crashes_GUI()
    {
        // TODO: Actually test the GUI code, not a replica of it.
        // Arrange
        var item = new ActionItem();
        var editVmTags = new List<TagValueModel>();
        var FileName = Path.Combine(Path.GetTempPath(), "test.csv");
        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        var mapper = new ActionItemDiskMapper(FileName);
        var repo = new Core.Data.TodoRepository(mapper, Path.GetDirectoryName(FileName), Environment.MachineName);
        var _api = new Core.ViewModel(repo);

        // Act
        item.Tags = editVmTags.ToDictionary(k => k.Tag, v => v.Value);
        _api.AddItem(item);

        // Assert
        _api.SearchSpecification = new Core.Search.TagValueSpecification(string.Empty, string.Empty);
        Assert.NotEmpty(_api.SearchResults);
    }
}

internal class TagValueModel
{
        public string Tag{ get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
}