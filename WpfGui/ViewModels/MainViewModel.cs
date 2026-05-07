using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.Maroon.Interfaces;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Properties;
using AssimilationSoftware.TodoSort.WpfGui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        #region Fields
        const string _defaultContext = "inbox";

        public ITaskListView Window;
        private Context? _selectedContext;
        private Context _searchResultsContext;
        private List<Context> _contexts;
        private string? _fileName;
        private TodoFileInfo? _lastOpenedFile;
        private List<ActionViewItem> _currentItems;
        private const int CommitLimit = 256;

        private ITodoRepository _repo;
        private ViewModel _api;

        private RelayCommand<string> _openRecentCommand;
        private RelayCommand _rankCommand;
        private RelayCommand _reloadCommand;
        private RelayCommand _importCommand;
        private RelayCommand _closeCommand;
        private RelayCommand _addItemCommand;
        private RelayCommand _addUrlCommand;
        private RelayCommand _openFileCommand;
        private RelayCommand _saveFileCommand;
        private RelayCommand _applySearchCommand;
        private RelayCommand _cleanupCommand;
        private RelayCommand _commitCommand;
        private RelayCommand _maskTextCommand;
        private RelayCommand _toggleHeadCommand;
        private RelayCommand _searchCommand;
        private RelayCommand _newFileCommand;
        private RelayCommand _balanceCommand;

        private string _searchKeyword;
        private string _searchTagName;
        private string _searchTagValue;
        private ActionViewItem _selectedItem;
        private bool _searchExpanded;
        private ActionItem? _searchProject;
        private SortByProperty _sortBy = SortByProperty.Upvotes;
        private Context? _searchContext;
        private decimal? _lastPendingCount;
        private bool _isSearchContextSelected;
        private bool _isSearchProjectSelected;
        private string _statusMessage;
        private DateTime? _lastDeferredCheckDate;

        #endregion

        #region Constructors
        public MainViewModel(string filename)
        {
            if (filename != null)
            {
                OpenFile(filename);
            }
        }
        #endregion

        #region Methods

        private async void OpenFile(string filename)
        {
            await OpenFileAsync(filename);
        }

        private async Task OpenFileAsync(string? filename)
        {
            try
            {
                // Validate filename
                if (string.IsNullOrEmpty(filename))
                {
                    StatusMessage = "Error: No file selected.";
                    return;
                }

                if (!File.Exists(filename))
                {
                    StatusMessage = $"Error: File not found: {filename}";
                    return;
                }

                FileName = filename;

                // Store the file name as the most recent one opened.
                _recentFilesList = new ObservableCollection<string>(Settings.Default.RecentFiles);
                if (!string.IsNullOrEmpty(filename))
                {
                    RecentFileList.Remove(filename);
                    RecentFileList.Insert(0, filename);
                }
                while (RecentFileList.Count > 10)
                {
                    RecentFileList.RemoveAt(10);
                }
                Settings.Default.RecentFiles = _recentFilesList.ToArray();
                _recentFilesList.Clear();
                SaveSettings();

                await Task.Run(() =>
                {
                    string? directoryName = Path.GetDirectoryName(FileName);
                    if (string.IsNullOrEmpty(directoryName))
                    {
                        throw new InvalidOperationException($"Cannot determine directory for file: {FileName}");
                    }
                    _repo = new TodoRepository(new ActionItemDiskMapper(FileName), directoryName, Environment.MachineName);
                    _api = new ViewModel(_repo);
                });

                if (_api?.SomedayItems != null)
                {
                    var undefers = _api.SomedayItems.Where(s => s.TickleDate <= DateTime.Today).Select(u => u.Title).ToArray();
                    if (undefers.Any())
                    {
                        var titleList = string.Join(Environment.NewLine, undefers);
                        // Confirm.
                        if (System.Windows.MessageBox.Show(
                            $"There are deferred items ready to return to the main lists:\n{titleList}\n Do you want to process them now?",
                                "Auto-undefer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Cleanup();
                        }
                    }
                }

                RaisePropertyChanged(nameof(Contexts));
                RaisePropertyChanged(nameof(Items));
                RaisePropertyChanged(nameof(RecentFileList));
                RaisePropertyChanged(nameof(Projects));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening file: {ex.Message}";
                System.Windows.MessageBox.Show($"Failed to open file:\n\n{ex.Message}", "Error Opening File", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Cleanup()
        {
            // Check that _api and _repo are initialized
            if (_api == null || _repo == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }

            // Confirm first.
            switch (ConfirmSaveCancel("Save changes before continuing? Unsaved changes will be discarded."))
            {
                case null:
                    return;
                case true:
                    _api.Save();
                    break;
            }


            foreach (var i in _repo.FindAll())
            {
                if (string.IsNullOrEmpty(i.Context))
                {
                    i.Context = _defaultContext;
                    _api.Update(i);
                }
            }

            // Process pending items for any to return to the main list.
            _api.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
            _api.Undefer(_defaultContext, _api.SomedaySearchResults.ToArray());

            // Check for out-of-order chain items.
            foreach (var headItem in Items.ToArray())
            {
                if (headItem.IsInChain)
                {
                    var chainItems = _repo.FindAll().Where(i => i.Context == headItem.Source.Context &&
                        i.Tags.ContainsKey("order") && i.GetIntTag("order", 0) < headItem.Source.GetIntTag("order", 0));
                    if (headItem.Source.ProjectId != null)
                    {
                        chainItems = chainItems.Where(i => i.ProjectId != null && i.ProjectId.Equals(headItem.Source.ProjectId));
                    }
                    else
                    {
                        chainItems = chainItems.Where(i =>
                            i.Tags.ContainsKey("series") && i.Tags["series"] == headItem.Tags["series"]);
                    }

                    // Get the earliest item from the chain.
                    var first = chainItems.OrderBy(i => i.GetIntTag("order", 0)).FirstOrDefault();
                    if (first != null)
                    {
                        first.ParentId = null;
                        _api.SetParent(headItem.Source, first);
                    }
                }
            }

            RaisePropertyChanged(nameof(Items));
        }

        private void CommitChanges()
        {
            if (_repo == null || _api == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }

            var pendingChanges = _repo.GetPendingChanges();
            if (pendingChanges.Count == 0)
            {
                StatusMessage = "No changes pending.";
            }
            else if (pendingChanges.Any(p => p.IsConflict))
            {
                StatusMessage = "Could not commit changes. Conflicting edits detected.";
            }
            else
            {
                var committed = _repo.CommitChanges();
                switch (committed)
                {
                    case 0:
                        StatusMessage = "Could not commit changes. Possible conflicts.";
                        break;
                    case 1:
                        StatusMessage = $"{committed} change committed.";
                        _api.UnsavedChanges = false;
                        break;
                    default:
                        StatusMessage = $"{committed} changes committed.";
                        _api.UnsavedChanges = false;
                        break;
                }
                // Refresh the last opened file info, so that the updated file check doesn't trigger right now.
                SaveLastOpenedFileMetaData(_lastOpenedFile?.FullName ?? string.Empty);
                RaisePropertyChanged(nameof(HasUnsavedChanges));
                RaisePropertyChanged(nameof(WindowTitle));
            }
        }

        public void CheckForCommit()
        {
            var pendingCount = _repo?.GetPendingChanges().Sum(p => p.Updates.Count);
            if (pendingCount > CommitLimit && _lastPendingCount != pendingCount)
            {
                var response = System.Windows.MessageBox.Show($"{pendingCount} changes are pending. Commit now?", "Commit Changes",
                    MessageBoxButton.YesNo);
                if (response == MessageBoxResult.Yes)
                {
                    CommitChanges();
                }
            }

            _lastPendingCount = pendingCount;
        }

        /// <summary>
        /// Checks for deferred items ready to return to the main list once per day.
        /// Called automatically when the window gains focus.
        /// </summary>
        public void CheckForDeferredItemsOnce()
        {
            // Skip if no file is loaded
            if (_api == null)
            {
                return;
            }

            // Skip if we've already checked today
            if (_lastDeferredCheckDate.HasValue && _lastDeferredCheckDate.Value.Date == DateTime.Today)
            {
                return;
            }

            // Record that we've checked today
            _lastDeferredCheckDate = DateTime.Today;

            // Get items ready to return to the main list
            var readyItems = _api.SomedayItems.Where(s => s.TickleDate <= DateTime.Today).ToArray();

            if (readyItems.Any())
            {
                // Process pending items without showing a confirmation dialog
                _api.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
                var itemsToUndefer = _api.SomedaySearchResults.ToArray();

                if (itemsToUndefer.Any())
                {
                    // Only process if there are unsaved changes or we need to save new ones
                    _api.Undefer(_defaultContext, itemsToUndefer);
                    _api.Save();

                    // Refresh the UI
                    RaisePropertyChanged(nameof(Items));
                    RaisePropertyChanged(nameof(Contexts));

                    // Notify the user (status message only, no dialog)
                    StatusMessage = $"Auto-processed {itemsToUndefer.Length} deferred item(s) ready to return.";
                }
            }
        }

        private void RankItems()
        {
            if (_api == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }
            // Open up a ranking window.
            var rv = new RankView();
            var rvm = new RankViewModel(Items, rv, _api);
            rv.DataContext = rvm;
            rv.ShowDialog();
            RaisePropertyChanged(nameof(Items));
        }

        private async void ReloadFile()
        {
            if (string.IsNullOrEmpty(FileName)) return;
            var result = HasUnsavedChanges ? System.Windows.MessageBox.Show("You have unsaved changes in memory. Abandon these changes and reload the file?", "Abandon changes?", MessageBoxButton.YesNoCancel) : MessageBoxResult.None;
            if (!HasUnsavedChanges || result == MessageBoxResult.Yes)
            {
                await OpenFileAsync(FileName);
            }
        }

        private void ImportExecuted()
        {
            var importView = new ImportView();
            var importVm = new ImportViewModel(_api, importView);
            importView.DataContext = importVm;
            importView.ShowDialog();
            RaisePropertyChanged(nameof(Contexts));
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
        }

        public async void OpenCommandExecuted()
        {
            switch (ConfirmSaveCancel("You have unsaved changes. Save first before opening another file?"))
            {
                case null:
                    return;
                case true:
                    _api.Save();
                    break;
            }

            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Document",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt|All documents (*.*)|*.*",
                Title = "Todo file"
            };

            // Show open file dialog box
            var result = dlg.ShowDialog();
            if (result == true)
            {
                await OpenFileAsync(dlg.FileName);
            }
        }

        public async void NewFileExecuted()
        {
            switch (ConfirmSaveCancel("You have unsaved changes. Save first before creating a new file?"))
            {
                case null:
                    return;
                case true:
                    _api.Save();
                    break;
            }

            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Document",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt|All documents (*.*)|*.*",
                Title = "Todo file",
                CheckFileExists = false
            };

            // Show open file dialog box
            var result = dlg.ShowDialog();
            if (result == true)
            {
                if (File.Exists(dlg.FileName))
                {
                    // Replace the file?
                }
                await OpenFileAsync(dlg.FileName);
            }
        }

        private void BalanceExecuted()
        {
            if (_api == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }
            var balanceView = new BalanceView();
            var balanceVm = new BalanceViewModel(balanceView, _api);
            balanceView.DataContext = balanceVm;
            balanceView.ShowDialog();
            RaisePropertyChanged(nameof(Items));
        }

        public void SaveCommandExecuted()
        {
            if (_api == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }
            _api.Save();
            SaveLastOpenedFileMetaData(_lastOpenedFile?.FullName);
            RaisePropertyChanged(nameof(Items));
        }

        public void MarkDone(ActionItem item, DateTime? doneDate = null)
        {
            if (_api == null) return;
            _api.MarkDone(doneDate, item);
            RaisePropertyChanged(nameof(Items));
        }

        public void Undo(ActionItem item, string context = _defaultContext)
        {
            if (_api == null) return;
            _api.Undo(context, item);
            RaisePropertyChanged(nameof(Items));
        }

        private void CloseExecute()
        {
            // Confirm close if unsaved changes.
            if (_api != null)
            {
                switch (ConfirmSaveCancel("You have unsaved changes. Save before quitting?"))
                {
                    case null:
                        return;
                    case true:
                        _api.Save();
                        break;
                }
            }
            System.Windows.Application.Current.Shutdown();
        }

        public void Update(ActionItem item)
        {
            if (_api == null) return;
            _api.Update(item);
            RaisePropertyChanged(nameof(WindowTitle));
            RaisePropertyChanged(nameof(HasUnsavedChanges));
        }

        public void Move(ActionItem source, string newContext, bool disconnectChildren = true)
        {
            if (_api == null) return;
            _api.SetContext(source, newContext);
            if (disconnectChildren)
            {
                _api.ResetPriorityParents(source);
            }
            RaisePropertyChanged(nameof(Contexts));
            RaisePropertyChanged(nameof(Items));
        }

        public void Defer(ActionItem item)
        {
            if (_api == null) return;
            _api.Defer(item);
            RaisePropertyChanged(nameof(Items));
        }

        public void Undefer(ActionItem item)
        {
            if (_api == null) return;
            _api.Undefer(_defaultContext, item);
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(Contexts));
        }

        private void AddExecuted()
        {
            if (_api == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }
            // Show the Edit view.
            var editWindow = new EditItemView();
            var item = new ActionItem
            {
                Context = SelectedContext?.IsSearch ?? false ? SelectedContext?.Title : _defaultContext
            };
            var editVm = new EditViewModel(this, new ActionViewItem(item, this), editWindow);
            editWindow.DataContext = editVm;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = editWindow.ShowDialog(Window);
            if (result.HasValue && result.Value)
            {
                // Update the source item.
                item.Title = editVm.Title;
                item.Notes = editVm.Notes.Split('\n').ToList();
                item.Tags = editVm.Tags.ToDictionary(k => k.Tag, v => v.Value);
                item.ProjectId = editVm.Project?.ID;
                item.Context = editVm.Context;
                if (editVm.IsDeferred)
                {
                    item.TickleDate = editVm.TickleDate;
                }
                _api.AddItem(item);
                RaisePropertyChanged(nameof(Items));
                RaisePropertyChanged(nameof(Contexts));
            }
        }

        private void AddUrlExecuted()
        {
            if (_api == null)
            {
                StatusMessage = "Error: No file loaded.";
                return;
            }
            var addWindow = new AddUrlView();
            var item = new ActionItem
            {
                Context = SelectedContext?.IsSearch ?? false ? SelectedContext?.Title : _defaultContext
            };
            var addVm = new AddUrlViewModel(this, new ActionViewItem(item, this), addWindow);
            addWindow.DataContext = addVm;
            addWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = addWindow.ShowDialog(Window);
            if (result.HasValue && result.Value)
            {
                item.Title = string.IsNullOrEmpty(addVm.Title) ? addVm.Url : addVm.Title;
                item.Tags = new Dictionary<string, string> { { "url", addVm.Url } };
                item.Context = addVm.SelectedContext?.IsSearch ?? false ? _defaultContext : addVm.SelectedContext?.Title;
                _api.AddItem(item);
                RaisePropertyChanged(nameof(Items));
                RaisePropertyChanged(nameof(Contexts));
            }
        }

        public void MoveAll(string context, string newContext)
        {
            if (_api == null) return;
            _api.SearchSpecification = new ContextSearchSpecification(context);
            _api.ShowHeadOnly = false;
            foreach (var item in _api.SearchResults.ToList())
            {
                _api.SetContext(item, newContext);
            }
            _api.ShowHeadOnly = true;
            RaisePropertyChanged(nameof(Contexts));
            RaisePropertyChanged(nameof(Items));
        }

        public void Delete(ActionItem source)
        {
            if (_api == null) return;
            _api.ResetPriorityParents(source);
            _api.Delete(source);
            RaisePropertyChanged(nameof(Items));
        }

        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Cached properties.
            if (propertyName == nameof(Items) || propertyName == nameof(ShowHeadOnly))
            {
                _currentItems?.Clear();
            }
            else if (propertyName == nameof(Contexts))
            {
                _contexts?.Clear();
            }

            base.OnPropertyChanged(propertyName);

            // Dependent properties.
            if (propertyName == nameof(Items))
            {
                base.OnPropertyChanged(nameof(HasUnsavedChanges));
                base.OnPropertyChanged(nameof(WindowTitle));
            }

            if (propertyName == nameof(ShowHeadOnly))
            {
                base.OnPropertyChanged(nameof(Items));
            }

            if (propertyName == nameof(SelectedContext))
            {
                // Reset column widths.
                Window.ResizeColumns();
            }

            if (propertyName == nameof(Contexts))
            {
                RaisePropertyChanged(nameof(SearchContexts));
            }
        }

        private bool? ConfirmSaveCancel(string message)
        {
            if (!HasUnsavedChanges) return false;
            var result = System.Windows.MessageBox.Show(message, "Save changes?", MessageBoxButton.YesNoCancel);
            switch (result)
            {
                case MessageBoxResult.Cancel:
                    return null;
                case MessageBoxResult.No:
                    return false;
                case MessageBoxResult.Yes:
                    return true;
                default:
                    return null;
            }
        }

        private async void OpenRecentFile(string? filename)
        {
            switch (ConfirmSaveCancel("You have unsaved changes. Save before opening this file?"))
            {
                case null:
                    return;
                case true:
                    _api.Save();
                    break;
            }
            await OpenFileAsync(filename);
        }

        public void ApplySearchExecuted()
        {
            var specs = new List<ISearchSpecification<ActionItem>>();
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                specs.Add(new FullTextSearchSpecification(SearchKeyword.Trim()));
            }

            if (!string.IsNullOrEmpty(SearchTagName) || !string.IsNullOrWhiteSpace(SearchTagValue))
            {
                specs.Add(new TagValueSpecification(SearchTagName, SearchTagValue));
            }

            if (SearchProject != null && IsSearchProjectSelected)
            {
                specs.Add(new ProjectChildrenSearchSpecification(SearchProject));
            }

            if (SearchContext != null && IsSearchContextSelected)
            {
                specs.Add(new ContextSearchSpecification(SearchContext.Title));
            }
            _searchResultsContext.SearchSpecification = new AndSpecification<ActionItem>(specs.ToArray());
            // _searchResultsContext.ShowHeadOnly = false;
            ShowHeadOnly = false;
            SelectedContext = _searchResultsContext;
        }

        private void SearchExecuted()
        {
            SearchExpanded = !SearchExpanded;
        }

        public void CheckForContext(string context)
        {
            if (_contexts.All(c => c.Title != context))
            {
                // Context not found. Refresh.
                RaisePropertyChanged(nameof(Contexts));
            }
        }

        public async void CheckForUpdatedFile()
        {
            // If current file in memory is different to current file on disk, offer to reload.
            if (_lastOpenedFile != null)
            {
                var fileOnDisk = new FileInfo(_lastOpenedFile.FullName);
                var changeCount = ChangeCountOnDisk(fileOnDisk.DirectoryName!);
                if (_lastOpenedFile.LastWriteTime != fileOnDisk.LastWriteTime || changeCount != _lastOpenedFile.ChangeCount)
                {
                    // Confirm.
                    bool doOpen;
                    if (HasUnsavedChanges)
                    {
                        doOpen = System.Windows.MessageBox.Show(
                            "The file on disk has been modified and you have unsaved changes in memory. Abandon changes and reload the file from disk?",
                            "Abandon changes?", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                    }
                    else
                    {
                        doOpen = true;
                        StatusMessage = "File on disk has been modified. Loading changes...";
                    }
                    // Open.
                    if (doOpen)
                    {
                        await OpenFileAsync(_lastOpenedFile.FullName);
                        StatusMessage = string.Empty;
                    }
                    else
                    {
                        // If we're not opening this version of the file on disk, we need to make sure we don't ask again.
                        SaveLastOpenedFileMetaData(fileOnDisk.FullName);
                    }
                }
            }
        }

        internal static int ChangeCountOnDisk(string changeFilesPath)
        {
            return changeFilesPath != null && Directory.Exists(changeFilesPath)
                ? Directory.EnumerateFiles(changeFilesPath, "*.xml").Count()
                : 0;
        }

        private void SaveLastOpenedFileMetaData(string? fileName)
        {
            if (fileName is not null)
            {
                _lastOpenedFile = new TodoFileInfo(fileName);
            }
            else
            {
                _lastOpenedFile = null;
            }
            Settings.Default.Todo = fileName ?? string.Empty;
            SaveSettings();
        }

        public void RefreshContexts()
        {
            RaisePropertyChanged(nameof(Contexts));
        }

        #endregion

        #region Properties

        public List<Context> Contexts
        {
            get
            {
                if (_api == null) return new List<Context>();
                if (_contexts == null || !_contexts.Any())
                {
                    // Preserve selected context.
                    var saveContext = SelectedContext;
                    _contexts = new List<Context>();
                    foreach (var con in _api.GetContextNames("done", "someday").OrderBy(c => c))
                    {
                        _contexts.Add(new Context
                        {
                            Title = con,
                            SearchSpecification = new ContextSearchSpecification(con),
                            View = Window,
                            DateVisible = Visibility.Collapsed,
                            AllOtherContexts = new List<Context>(),
                            ParentVm = this
                        });
                    }

                    foreach (var con in _contexts)
                    {
                        con.AllOtherContexts = new List<Context>(_contexts);
                        con.AllOtherContexts.Remove(con);
                    }
                    _contexts.Add(new Context { Title = "done", View = Window, DateVisible = Visibility.Visible, DateColumnTitle = "Done Date" });
                    _contexts.Add(new Context { Title = "someday", View = Window, DateVisible = Visibility.Visible, DateColumnTitle = "Return Date" });
                    _searchResultsContext = new Context
                    {
                        Title = "Search",
                        View = Window,
                        DateVisible = Visibility.Collapsed,
                        // TODO: Get full search spec from a new method or property.
                        SearchSpecification = new FullTextSearchSpecification(SearchKeyword),
                        CanMoveFrom = false
                    };
                    _contexts.Add(_searchResultsContext);
                    if (saveContext != null)
                    {
                        // Can't just set to the saved one, because it's been reconstructed.
                        SelectedContext = _contexts.FirstOrDefault(c => c.Title == saveContext.Title);
                    }
                }
                return _contexts;
            }
        }

        public Context? SelectedContext
        {
            get => _selectedContext;
            set
            {
                if (_selectedContext == value && value != _searchResultsContext) return;
                // First, if we are currently in the Search context, and we're moving away from it, enable ShowHeadOnly.
                if (_selectedContext == _searchResultsContext && value != _searchResultsContext)
                {
                    ShowHeadOnly = true;
                    SearchExpanded = false;
                }
                else if (value == _searchResultsContext)
                {
                    SearchExpanded = true;
                }
                _selectedContext = value;
                RaisePropertyChanged();
                _currentItems = [];
                RaisePropertyChanged(nameof(Items));
            }
        }

        public ActionViewItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                RaisePropertyChanged();
            }
        }

        public string? FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                SaveLastOpenedFileMetaData(value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(WindowTitle));
            }
        }

        public bool HasUnsavedChanges => _api?.UnsavedChanges ?? false;

        public List<ActionViewItem> Items
        {
            get
            {
                if (_api == null || SelectedContext == null) return new List<ActionViewItem>();
                if (_currentItems != null && _currentItems.Any()) return _currentItems;
                switch (SelectedContext.Title)
                {
                    case "done":
                        _api.DoneSearchSpecification = SelectedContext.SearchSpecification;
                        _currentItems = _api.DoneSearchResults.Select(s => new ActionViewItem(s, this)).OrderByDescending(i => i.DoneDate).ThenBy(i => i.Title).ToList();
                        break;
                    case "someday":
                        _api.SomedaySearchSpecification = SelectedContext.SearchSpecification;
                        _currentItems = _api.SomedaySearchResults.Select(s => new ActionViewItem(s, this)).OrderBy(i => i.TickleDate ?? DateTime.MaxValue).ThenBy(i => i.Title).ToList();
                        break;
                    case "Search":
                        if (SearchContext != null && SearchContext.Title == "done")
                        {
                            _api.DoneSearchSpecification = SelectedContext.SearchSpecification;
                            _currentItems = _api.DoneSearchResults.Select(s => new ActionViewItem(s, this)).OrderByDescending(i => i.DoneDate).ThenBy(i => i.Title).ToList();
                        }
                        else if (SearchContext != null && SearchContext.Title == "someday")
                        {
                            _api.SomedaySearchSpecification = SelectedContext.SearchSpecification;
                            _currentItems = _api.SomedaySearchResults.Select(s => new ActionViewItem(s, this)).OrderBy(i => i.TickleDate).ThenBy(i => i.Title).ToList();
                        }
                        else
                        {
                            _api.SearchSpecification = SelectedContext.SearchSpecification;
                            var selectedItems = _api.SearchResults.Select(s => new ActionViewItem(s, this));
                            if (SortByUpvotes)
                            {
                                _currentItems = selectedItems.OrderByDescending(i => i.UpVotes).ThenBy(i => i.Title).ToList();
                            }
                            else if (SortByTitle)
                            {
                                _currentItems = selectedItems.OrderBy(i => i.Title).ToList();
                            }
                            else if (SortByOrder)
                            {
                                _currentItems = selectedItems.OrderBy(a => a.Tags.ContainsKey("order") ? a.Tags["order"] : "0", new SemiNumericComparer()).ToList();
                            }
                            else if (SortByCreatedDate)
                            {
                                _currentItems = selectedItems.OrderByDescending(a => a.Tags.ContainsKey("created-date") ? DateTime.Parse(a.Tags["created-date"]) : DateTime.MinValue).ToList();
                            }
                        }
                        break;
                    default:
                        _api.SearchSpecification = SelectedContext.SearchSpecification;
                        // Apply user-specified sorting options.
                        var defaultItems = _api.SearchResults.Select(s => new ActionViewItem(s, this));
                        if (SortByUpvotes)
                        {
                            _currentItems = defaultItems.OrderByDescending(i => i.UpVotes).ThenBy(i => i.Title).ToList();
                        }
                        else if (SortByTitle)
                        {
                            _currentItems = defaultItems.OrderBy(i => i.Title).ToList();
                        }
                        else if (SortByOrder)
                        {
                            _currentItems = defaultItems.OrderBy(a => a.Tags.ContainsKey("order") ? a.Tags["order"] : "0", new SemiNumericComparer()).ToList();
                        }
                        else if (SortByCreatedDate)
                        {
                            _currentItems = defaultItems.OrderByDescending(a => a.Tags.ContainsKey("created-date") ? DateTime.Parse(a.Tags["created-date"]) : DateTime.MinValue).ToList();
                        }
                        break;
                }
                return _currentItems ?? [];
            }
        }

        public string WindowTitle => $"{(HasUnsavedChanges ? "*" : "")}TodoSort - {FileName}";

        private ObservableCollection<string> _recentFilesList;
        public ObservableCollection<string> RecentFileList
        {
            get => _recentFilesList ?? (_recentFilesList = new ObservableCollection<string>(Settings.Default.RecentFiles));
            set
            {
                _recentFilesList = value;
                Settings.Default.RecentFiles = value.ToArray();
                RaisePropertyChanged();
            }
        }

        public List<ActionItem> Projects => _api != null ? _api.GetProjects().OrderBy(p => p?.Title).ToList() : new List<ActionItem> { };

        public List<Context> SearchContexts => Contexts.Where(c => c.Title != "Search").OrderBy(c => c.Title).ToList();

        public string VersionNumber => "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public bool ShowHeadOnly
        {
            get => _api?.ShowHeadOnly ?? true;
            set
            {
                if (_api.ShowHeadOnly == value) return;
                _api.ShowHeadOnly = value;
                RaisePropertyChanged();
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (_searchKeyword == value) return;
                _searchKeyword = value;
                RaisePropertyChanged();
            }
        }

        public string SearchTagName
        {
            get => _searchTagName;
            set
            {
                if (_searchTagName == value) return;
                _searchTagName = value;
                RaisePropertyChanged();
            }
        }

        public string SearchTagValue
        {
            get => _searchTagValue;
            set
            {
                if (_searchTagValue == value) return;
                _searchTagValue = value;
                RaisePropertyChanged();
            }
        }

        public ActionItem? SearchProject
        {
            get => _searchProject;
            set
            {
                if (Equals(_searchProject, value)) return;
                _searchProject = value;
                if (value != null) IsSearchProjectSelected = true;
                RaisePropertyChanged();
            }
        }

        public bool SearchExpanded
        {
            get => _searchExpanded;
            set
            {
                if (_searchExpanded == value) return;
                _searchExpanded = value;
                RaisePropertyChanged();
            }
        }

        public bool Masked
        {
            get => Settings.Default.MaskNsfwItems;
            set
            {
                if (Settings.Default.MaskNsfwItems == value) return;
                Settings.Default.MaskNsfwItems = value;
                Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public IRepository<ActionItem> Repository => _repo;

        public ViewModel Api => _api;

        public Context? SearchContext
        {
            get => _searchContext;
            set
            {
                if (_searchContext == value) return;
                _searchContext = value;
                if (value != null) IsSearchContextSelected = true;
                RaisePropertyChanged();
            }
        }

        public bool SortByUpvotes
        {
            get => _sortBy == SortByProperty.Upvotes;
            set
            {
                if (_sortBy == SortByProperty.Upvotes == value) return;
                _sortBy = value ? SortByProperty.Upvotes : SortByProperty.None;
                NotifySortChanged();
            }
        }

        public bool SortByTitle
        {
            get => _sortBy == SortByProperty.Title;
            set
            {
                if (_sortBy == SortByProperty.Title == value) return;
                _sortBy = value ? SortByProperty.Title : SortByProperty.None;
                NotifySortChanged();
            }
        }

        public bool SortByOrder
        {
            get => _sortBy == SortByProperty.Order;
            set
            {
                if (_sortBy == SortByProperty.Order == value) return;
                _sortBy = value ? SortByProperty.Order : SortByProperty.None;
                NotifySortChanged();
            }
        }

        public bool SortByCreatedDate
        {
            get => _sortBy == SortByProperty.CreatedDate;
            set
            {
                if (_sortBy == SortByProperty.CreatedDate == value) return;
                _sortBy = value ? SortByProperty.CreatedDate : SortByProperty.None;
                NotifySortChanged();
            }
        }

        private void NotifySortChanged()
        {
            OnPropertyChanged(nameof(SortByCreatedDate));
            OnPropertyChanged(nameof(SortByOrder));
            OnPropertyChanged(nameof(SortByTitle));
            OnPropertyChanged(nameof(SortByUpvotes));
            OnPropertyChanged(nameof(Items));
        }

        public bool IsSearchProjectSelected
        {
            get => _isSearchProjectSelected;
            set
            {
                if (_isSearchProjectSelected == value) return;
                _isSearchProjectSelected = value;
                if (!value) SearchProject = null;
                RaisePropertyChanged();
            }
        }

        public bool IsSearchContextSelected
        {
            get => _isSearchContextSelected;
            set
            {
                if (_isSearchContextSelected == value) return;
                _isSearchContextSelected = value;
                if (!value) SearchContext = null;
                RaisePropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (value == _statusMessage) return;
                _statusMessage = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        public RelayCommand<string> OpenRecentCommand => _openRecentCommand ?? (_openRecentCommand = new RelayCommand<string>(OpenRecentFile));

        public RelayCommand RankCommand => _rankCommand ?? (_rankCommand = new RelayCommand(RankItems));

        public RelayCommand ReloadCommand => _reloadCommand ?? (_reloadCommand = new RelayCommand(ReloadFile, () => !string.IsNullOrEmpty(FileName)));

        public RelayCommand ImportCommand => _importCommand ?? (_importCommand = new RelayCommand(ImportExecuted, () => !string.IsNullOrEmpty(FileName)));

        public RelayCommand CloseCommand => _closeCommand ?? (_closeCommand = new RelayCommand(CloseExecute));

        public RelayCommand AddItemCommand => _addItemCommand ?? (_addItemCommand = new RelayCommand(AddExecuted));

        public RelayCommand AddUrlCommand => _addUrlCommand ?? (_addUrlCommand = new RelayCommand(AddUrlExecuted));

        public RelayCommand OpenFileCommand => _openFileCommand ?? (_openFileCommand = new RelayCommand(OpenCommandExecuted));

        public RelayCommand SaveFileCommand => _saveFileCommand ?? (_saveFileCommand = new RelayCommand(SaveCommandExecuted));

        public RelayCommand ApplySearchCommand => _applySearchCommand ?? (_applySearchCommand = new RelayCommand(ApplySearchExecuted));

        public ICommand CleanupCommand => _cleanupCommand ?? (_cleanupCommand = new RelayCommand(Cleanup));

        public ICommand CommitCommand => _commitCommand ?? (_commitCommand = new RelayCommand(CommitChanges));

        public ICommand MaskTextCommand => _maskTextCommand ?? (_maskTextCommand = new RelayCommand(() => Masked = !Masked));

        public ICommand ToggleHeadCommand => _toggleHeadCommand ?? (_toggleHeadCommand = new RelayCommand(() => ShowHeadOnly = !ShowHeadOnly));

        public ICommand SearchCommand => _searchCommand ?? (_searchCommand = new RelayCommand(SearchExecuted));

        public ICommand NewFileCommand => _newFileCommand ?? (_newFileCommand = new RelayCommand(NewFileExecuted));

        public ICommand BalanceCommand => _balanceCommand ?? (_balanceCommand = new RelayCommand(BalanceExecuted, () => false));

        #endregion
    }

    internal class TodoFileInfo
    {
        private FileInfo _fileInfo;

        public TodoFileInfo(string fullName)
        {
            FullName = fullName;
            _fileInfo = new FileInfo(fullName);
            if (_fileInfo.DirectoryName != null)
            {
                ChangeCount = MainViewModel.ChangeCountOnDisk(_fileInfo.DirectoryName);
            }
            else
            {
                ChangeCount = 0;
            }
        }

        public string FullName { get; set; }
        public DateTime LastWriteTime => _fileInfo.LastWriteTime;
        public int ChangeCount { get; set; }
    }
}
