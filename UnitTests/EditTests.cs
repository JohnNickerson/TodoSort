using System.Reflection;
using System.Runtime.Serialization;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using AssimilationSoftware.TodoSort.CoreGui.ViewModels;
using Moq;

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

    [Fact]
    public void Editing_MultiLine_Notes_Preserves_Notes_Without_Blank_Lines()
    {
        // Arrange
        var originalItem = new ActionItem
        {
            Title = "Test item",
            Context = "inbox",
            Notes = new List<string> { "Original note" },
            Tags = new Dictionary<string, string>()
        };

        var navigationServiceMock = new Mock<INavigationService>();
        var fakeMainViewModel = (MainViewModel)FormatterServices.GetUninitializedObject(typeof(MainViewModel));
        var actionViewModel = new ActionViewModel(originalItem, fakeMainViewModel);

        var editedNotes = string.Join(Environment.NewLine, new[] { "First line", "Second line" });
        navigationServiceMock
            .Setup(n => n.ShowEditView(fakeMainViewModel, actionViewModel))
            .Returns(new ItemDialogResult
            {
                DialogResult = true,
                Title = originalItem.Title,
                Context = originalItem.Context,
                Notes = editedNotes,
                Tags = new List<TagViewModel>()
            });

        var navigationField = typeof(MainViewModel).GetField("_navigationService", BindingFlags.NonPublic | BindingFlags.Instance);
        navigationField!.SetValue(fakeMainViewModel, navigationServiceMock.Object);

        // Act
        actionViewModel.EditExecuted();

        // Assert
        Assert.Equal(2, originalItem.Notes.Count);
        Assert.Equal(new[] { "First line", "Second line" }, originalItem.Notes);
        Assert.DoesNotContain(string.Empty, originalItem.Notes);
    }
}

internal class TagValueModel
{
    public string Tag { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}