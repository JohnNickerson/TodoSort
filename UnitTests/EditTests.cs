using System.Reflection;
using System.Runtime.Serialization;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CLI.Controllers;
using AssimilationSoftware.TodoSort.CLI.Enums;
using AssimilationSoftware.TodoSort.CLI.Interfaces;
using AssimilationSoftware.TodoSort.CLI.Options;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;
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
        var _api = new Core.TodoService(repo);

        // Act
        item.Tags = editVmTags.ToDictionary(k => k.Tag, v => v.Value);
        _api.AddItem(item);

        // Assert
        _api.SearchSpecification = new Core.Search.TagValueSpecification(string.Empty, string.Empty);
        Assert.NotEmpty(_api.SearchResults);
    }

    [Fact]
    public void TodoService_Exposes_Repository_Operations_Without_Leaking_The_Repository()
    {
        // Arrange
        var fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        var mapper = new ActionItemDiskMapper(fileName);
        var repo = new Core.Data.TodoRepository(mapper, Path.GetDirectoryName(fileName)!, Environment.MachineName);
        var service = new Core.TodoService(repo);

        var item = new ActionItem { ID = Guid.NewGuid(), Title = "Alpha", Context = "inbox" };
        repo.Create(item);

        // Act
        var byId = service.Find(item.ID);
        var all = service.FindAll();
        var search = service.Search(new TrueSpecification<ActionItem>(), true).ToList();
        var pending = service.GetPendingChanges();

        // Assert
        Assert.NotNull(byId);
        Assert.Contains(item, all);
        Assert.Contains(item, search);
        Assert.NotEmpty(pending);
    }

    [Fact]
    public void SearchDone_Uses_Done_Collection_Not_Main_Search_Results()
    {
        // Arrange
        var fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        var mapper = new ActionItemDiskMapper(fileName);
        var repo = new Core.Data.TodoRepository(mapper, Path.GetDirectoryName(fileName)!, Environment.MachineName);
        var service = new Core.TodoService(repo);
        var doneItem = new ActionItem { ID = Guid.NewGuid(), Title = "Completed item", Context = "done", DoneDate = DateTime.Today };
        repo.Create(doneItem);
        var view = new RecordingItemDisplay();
        var controller = new SearchController(view);
        var options = new DoneSearchSubOptions { Title = "Completed" };

        // Act
        controller.SearchDone(options, service);

        // Assert
        Assert.Single(view.Items);
        Assert.Equal(doneItem, view.Items.Single());
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

internal class RecordingItemDisplay : IItemDisplay
{
    public List<ActionItem> Items { get; } = new();

    public IOrderedEnumerable<ActionItem> ApplySort(string sortTag, IEnumerable<ActionItem> list, SortOrder sort = SortOrder.Ascending)
        => list.OrderBy(x => x.Title);

    public ActionItem? Disambiguate(IEnumerable<ActionItem> todoList, TodoService api, bool autoAcceptOne = false, bool nsfw = false, bool verbose = false, bool includeCancel = false)
        => todoList.FirstOrDefault();

    public string FormatTitle(ActionItem i, bool nsfw = false) => i.Title;

    public void PrintItem(ActionItem i, int? index, TodoService api, bool nsfw = false, bool verbose = false)
        => Items.Add(i);

    public void PrintItems(string sortTag, IEnumerable<ActionItem> list, TodoService api, bool nsfw = false, bool verbose = false)
        => Items.AddRange(list);

    public void PrintTree(List<ActionItem> bold, List<ActionItem> strike, TodoService api, bool nsfw = false, bool verbose = false)
    {
        Items.AddRange(bold);
        Items.AddRange(strike);
    }

    public void WriteLine(string message = "", params object?[]? args)
    {
    }
}

internal class TagValueModel
{
    public string Tag { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}