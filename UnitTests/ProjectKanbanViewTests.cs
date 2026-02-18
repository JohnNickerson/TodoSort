using System.IO.Abstractions.TestingHelpers;
using UnitTests.Scaffolding;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class ProjectKanbanViewTests
{
    [Fact]
    public void Search_Projects()
    {
        // Arrange
        MockFileSystem mockFileSystem = new();
        MockTodoRepository repo = new(mockFileSystem);
        var vm = new AssimilationSoftware.TodoSort.Core.ViewModel(repo);
        Maroon.Model.ActionItem project = new()
        {
            Context = "projects",
            ID = Guid.NewGuid(),
            RevisionGuid = Guid.NewGuid(),
            Title = "First Project"
        };
        vm.AddItem(project);
        Maroon.Model.ActionItem item = new()
        {
            Context = "inbox",
            ID = Guid.NewGuid(),
            RevisionGuid = Guid.NewGuid(),
            Title = "Current action",
            ProjectId = project.ID
        };
        vm.AddItem(item);

        // Act
        var foundProject = vm.GetProjects()[0];
        var foundItem = vm.GetProjectItems(foundProject.ID).First();

        // Assert
        Assert.Equal(project.ID, foundProject.ID);
        Assert.Equal(item.ID, foundItem.ID);
        Assert.Equal(project.ID, foundItem.ProjectId);
    }

    [Fact]
    public void Search_Done_Project_Items()
    {
        // Arrange
        MockFileSystem mockFileSystem = new();
        MockTodoRepository repo = new(mockFileSystem);
        var vm = new AssimilationSoftware.TodoSort.Core.ViewModel(repo);
        Maroon.Model.ActionItem project = new()
        {
            Context = "projects",
            ID = Guid.NewGuid(),
            RevisionGuid = Guid.NewGuid(),
            Title = "First Project"
        };
        vm.AddItem(project);
        Maroon.Model.ActionItem item = new()
        {
            Context = "done",
            ID = Guid.NewGuid(),
            RevisionGuid = Guid.NewGuid(),
            Title = "Current action",
            ProjectId = project.ID,
            DoneDate = new DateTime(2025, 04, 29)
        };
        vm.AddItem(item);

        // Act
        var foundProject = vm.GetProjects()[0];
        var foundItem = vm.GetDoneProjectItems(foundProject.ID).First();

        // Assert
        Assert.Equal(project.ID, foundProject.ID);
        Assert.Equal(item.ID, foundItem.ID);
        Assert.Equal(project.ID, foundItem.ProjectId);
    }
}