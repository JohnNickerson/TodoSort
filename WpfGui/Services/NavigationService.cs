using System.Windows;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.ViewModels;
using AssimilationSoftware.TodoSort.WpfGui.Views;

namespace AssimilationSoftware.TodoSort.WpfGui.Services;

public class NavigationService : INavigationService
{
    public void CloseApplication()
    {
        System.Windows.Application.Current.Shutdown();
    }

    public void CopyToClipboard(string clip)
    {
        System.Windows.Clipboard.SetText(clip);
    }

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

    public FileDialogResult ShowImportFileDialog(ImportFileType fileType)
    {
        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
        {
            FileName = "Document",
            DefaultExt = "txt",
            Filter = "Text documents (.txt)|*.txt|Instapaper Exports (.csv)|*.html|All documents (*.*)|*.*",
            Title = "Todo file"
        };
        switch (fileType)
        {
            case ImportFileType.Instapaper:
                dlg.FilterIndex = 2;
                break;
            case ImportFileType.TodoSort:
                dlg.FilterIndex = 1;
                break;
            default:
                dlg.FilterIndex = 3;
                break;
        }

        // Show open file dialog box
        var result = dlg.ShowDialog();
        return new FileDialogResult { DialogResult = result, FileName = dlg.FileName };
    }

    public void ShowImportView(ViewModel api)
    {
        var importView = new ImportView();
        var importVm = new ImportViewModel(api, importView, this);
        importView.DataContext = importVm;
        importView.ShowDialog();
    }

    public bool? ShowMessageBox(string message, string title, bool allowCancel = false)
    {
        var button = allowCancel ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo;
        var result = System.Windows.MessageBox.Show(message, title, button);
        switch (result)
        {
            case MessageBoxResult.No:
                return false;
            case MessageBoxResult.Yes:
                return true;
            case MessageBoxResult.Cancel:
            default:
                return null;
        }
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

    public FolderDialogResult ShowOpenFolderDialog(string folder)
    {
        var fdlg = new System.Windows.Forms.FolderBrowserDialog();
        fdlg.SelectedPath = folder;
        fdlg.ShowNewFolderButton = true;
        fdlg.Description = "Import items source";
        var answer = fdlg.ShowDialog();
        return new FolderDialogResult { DialogResult = answer == DialogResult.OK, SelectedPath = fdlg.SelectedPath };
    }

    public void ShowRankView(List<ActionViewModel> items, Core.ViewModel api)
    {
        var rv = new RankView();
        var rvm = new RankViewModel(items, rv, api);
        rv.DataContext = rvm;
        rv.ShowDialog();
    }
}