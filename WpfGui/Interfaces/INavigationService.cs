using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.ViewModels;

namespace AssimilationSoftware.TodoSort.WpfGui.Interfaces;

public interface INavigationService
{
    AddUrlDialogResult ShowAddUrlView(MainViewModel mainViewModel);
    void ShowBalanceView(ViewModel api);
    ItemDialogResult ShowEditView(MainViewModel mainViewModel);
    void ShowImportView(ViewModel api);
    FileDialogResult ShowOpenFileDialog(bool checkFileExists = true);
    void ShowRankView(List<ActionViewItem> items, ViewModel api);
}