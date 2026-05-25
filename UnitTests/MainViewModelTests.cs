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
}
