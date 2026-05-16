using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.CoreGui.Model;
using AssimilationSoftware.TodoSort.CoreGui.ViewModels;

namespace AssimilationSoftware.TodoSort.CoreGui.Interfaces;

public interface INavigationService
{
    void CloseApplication();
    void CopyToClipboard(string v);
    AddUrlDialogResult ShowAddUrlView(MainViewModel mainViewModel);
    void ShowBalanceView(ViewModel api);
    ItemDialogResult ShowEditView(MainViewModel mainViewModel);
    FileDialogResult ShowImportFileDialog(ImportFileType fileType);
    void ShowImportView(ViewModel api);
    bool? ShowMessageBox(string message, string title, bool allowCancel = false);
    FileDialogResult ShowOpenFileDialog(bool checkFileExists = true);
    FolderDialogResult ShowOpenFolderDialog(string folder);
    void ShowRankView(List<ActionViewModel> items, ViewModel api);
}