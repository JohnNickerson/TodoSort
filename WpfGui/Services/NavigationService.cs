using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.ViewModels;
using AssimilationSoftware.TodoSort.WpfGui.Views;

namespace AssimilationSoftware.TodoSort.WpfGui.Services;

public class NavigationService : INavigationService
{
    public AddUrlDialogResult ShowAddUrlView(MainViewModel mainViewModel)
    {
        var addWindow = new AddUrlView();
        var addVm = new AddUrlViewModel(mainViewModel, addWindow);
        addWindow.DataContext = addVm;
        var result = addWindow.ShowDialog(mainViewModel.Window);
        return new AddUrlDialogResult { DialogResult = result, Url = addVm.Url, Title = addVm.Title, Context = addVm.SelectedContext.Title };
    }

    public void ShowBalanceView(ViewModel api)
    {
        var balanceView = new BalanceView();
        var balanceVm = new BalanceViewModel(balanceView, api);
        balanceView.DataContext = balanceVm;
        balanceView.ShowDialog();
    }

    public ItemDialogResult ShowEditView(MainViewModel mainViewModel)
    {
            var editWindow = new EditItemView();
            var editVm = new EditViewModel(mainViewModel, editWindow, mainViewModel.SelectedItem);
            editWindow.DataContext = editVm;
            var result = editWindow.ShowDialog(mainViewModel.Window);
            return new ItemDialogResult { DialogResult = result, Title = editVm.Title, Context = editVm.Context, Notes = editVm.Notes, Tags = editVm.Tags, ProjectId = editVm.Project?.ID, IsDeferred = editVm.IsDeferred, TickleDate = editVm.TickleDate };
    }

    public void ShowImportView(ViewModel api)
    {
        var importView = new ImportView();
        var importVm = new ImportViewModel(api, importView);
        importView.DataContext = importVm;
        importView.ShowDialog();
    }

    public FileDialogResult ShowOpenFileDialog(bool checkFileExists = true)
    {
        // Configure open file dialog box
        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
        {
            FileName = "Document",
            DefaultExt = ".txt",
            Filter = "Text documents (.txt)|*.txt|All documents (*.*)|*.*",
            Title = "Todo file",
            CheckFileExists = checkFileExists
        };
        var result = dlg.ShowDialog();
        return new FileDialogResult { DialogResult = result, FileName = dlg.FileName };
    }

    public void ShowRankView(List<ActionViewModel> items, Core.ViewModel api)
    {
        var rv = new RankView();
        var rvm = new RankViewModel(items, rv, api);
        rv.DataContext = rvm;
        rv.ShowDialog();
    }
}