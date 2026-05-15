using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.ViewModels;

namespace AssimilationSoftware.TodoSort.WpfGui.Interfaces;

public interface INavigationService
{
    void CloseApplication();
    void CopyToClipboard(string v);
    AddUrlDialogResult ShowAddUrlView(MainViewModel mainViewModel);
    void ShowBalanceView(ViewModel api);
    ItemDialogResult ShowEditView(MainViewModel mainViewModel);
    void ShowImportView(ViewModel api);
    bool? ShowMessageBox(string message, string title, bool allowCancel = false);
    FileDialogResult ShowOpenFileDialog(bool checkFileExists = true);
    void ShowRankView(List<ActionViewModel> items, ViewModel api);
}