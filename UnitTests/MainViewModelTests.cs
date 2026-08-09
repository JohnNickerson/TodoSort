using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using AssimilationSoftware.TodoSort.CoreGui.ViewModels;
using Moq;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class MainViewModelTests
{
    [Fact]
    public void ImportCommand_OpenImportView_CallsNavigationService()
    {
        // Arrange
        var navigationServiceMock = new Mock<INavigationService>();
        var settingsMock = new Mock<ISettings>();
        settingsMock.SetupProperty(s => s.RecentFiles, Array.Empty<string>());
        settingsMock.SetupProperty(s => s.Todo, string.Empty);

        var mainViewModel = new MainViewModel(null!, navigationServiceMock.Object, settingsMock.Object)
        {
            FileName = "test.txt"
        };

        navigationServiceMock.Setup(n => n.ShowImportView(null));

        // Act
        var canExecute = mainViewModel.ImportCommand.CanExecute(null);
        mainViewModel.ImportCommand.Execute(null);

        // Assert
        Assert.True(canExecute);
        navigationServiceMock.Verify(n => n.ShowImportView(null), Times.Once);
    }

    [Fact]
    public void AddExecuted_With_Null_Notes_Should_Not_Throw_Exception()
    {
        // Arrange
        var navigationServiceMock = new Mock<INavigationService>();
        var settingsMock = new Mock<ISettings>();
        settingsMock.SetupProperty(s => s.RecentFiles, Array.Empty<string>());
        settingsMock.SetupProperty(s => s.Todo, string.Empty);

        var mainViewModel = new MainViewModel(null!, navigationServiceMock.Object, settingsMock.Object)
        {
            FileName = "test.txt"
        };
        // Mock return value for navigationService.ShowEditView
        navigationServiceMock.Setup(n => n.ShowEditView(mainViewModel, null)).Returns(new CoreGui.Model.ItemDialogResult()
        {
            Context = "playlist",
            Notes = null,
            DialogResult = true,
            Title = "Test Title",
            Tags = null
        });

        // Act & Assert
        var exception = Record.Exception(() => mainViewModel.AddExecuted());
        Assert.Null(exception);
    }
}
