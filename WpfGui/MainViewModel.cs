using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.Maroon.Interfaces;
using AssimilationSoftware.Maroon.Mappers.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Properties;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class MainViewModel : ViewModelBase
    {
        #region Fields

        private Context _selectedContext;
        private Context _searchResultsContext;
        private List<Context> _contexts;
        private string _fileName;
        private List<ActionViewItem> _currentItems;

        private ITodoRepository _repo;
        private ViewModel _api;

        private RelayCommand<string> _openRecentCommand;
        private RelayCommand _rankCommand;
        private RelayCommand _reloadCommand;
        private RelayCommand _closeCommand;
        private RelayCommand _addItemCommand;
        private RelayCommand _openFileCommand;
        private RelayCommand _saveFileCommand;
        private RelayCommand _applySearchCommand;
        private RelayCommand _cleanupCommand;
        private RelayCommand _commitCommand;
        private RelayCommand _maskTextCommand;
        private RelayCommand _toggleHeadCommand;
        private RelayCommand _searchCommand;
        private RelayCommand _newFileCommand;

        private string _searchKeyword;
        private string _searchMissingTagName;
        private ActionViewItem _selectedItem;
        private bool _searchExpanded;
        private ActionItem _searchProject;
        private bool _sortByUpvotes = true;
        private bool _sortByTitle;
        private bool _sortByOrder;

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

        private void OpenFile(string filename)
        {
            FileName = filename;

            // Store the file name as the most recent one opened.
            RecentFileList.Remove(filename);
            RecentFileList.Insert(0, filename);
            while (RecentFileList.Count > 10)
            {
                RecentFileList.RemoveAt(10);
            }
            SaveSettings();

            _repo = new TodoRepository(new ActionItemDiskMapper(FileName), Path.GetDirectoryName(FileName));
            _api = new ViewModel(_repo);

            if (_api.SomedayItems.Any(s => s.TickleDate <= DateTime.Today))
            {
                // Confirm.
                if (MessageBox.Show(
                        "There are deferred items ready to return to the main lists. Do you want to process them now?",
                        "Auto-undefer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Cleanup();
                }
            }

            OnPropertyChanged(nameof(Contexts));
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(RecentFileList));
            OnPropertyChanged(nameof(Projects));
        }

        private void Cleanup()
        {
            // Confirm first.
            switch (ConfirmSaveCancel("You have unsaved changes. This operation will discard them. Save first?"))
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
                    i.Context = "inbox";
                    _api.Update(i);
                }
            }

            // Process pending items for any to return to the main list.
            _api.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
            _api.Undefer("inbox", _api.SomedaySearchResults.ToArray());

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

            OnPropertyChanged(nameof(Items));
        }

        private void CommitChanges()
        {
            var pendingChanges = _repo.GetPendingChanges();
            if (pendingChanges.Count == 0)
            {
                _api.StatusMessage = "No changes pending.";
            }
            else if (pendingChanges.Any(p => p.IsConflict))
            {
                _api.StatusMessage = "Could not commit changes. Conflicting edits detected.";
            }
            else
            {
                var committed = _repo.CommitChanges();
                switch (committed)
                {
                    case 0:
                        _api.StatusMessage = "Could not commit changes. Possible conflicts.";
                        break;
                    case 1:
                        _api.StatusMessage = $"{committed} change committed.";
                        break;
                    default:
                        _api.StatusMessage = $"{committed} changes committed.";
                        break;
                }
            }
        }

        private void RankItems()
        {
            // Open up a ranking window.
            var rv = new RankView();
            var rvm = new RankViewModel(Items, rv, _api);
            rv.DataContext = rvm;
            rv.ShowDialog();
            OnPropertyChanged(nameof(Items));
        }

        private void ReloadFile()
        {
            if (string.IsNullOrEmpty(FileName)) return;
            var result = MessageBox.Show("You have unsaved changes in memory. Abandon these changes and reload the file?", "Save changes?", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Yes)
            {
                OpenFile(FileName);
            }
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
        }

        public void OpenCommandExecuted()
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
                OpenFile(dlg.FileName);
            }
        }

        public void NewFileExecuted()
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
                OpenFile(dlg.FileName);
            }
        }

        public void SaveCommandExecuted()
        {
            _api.Save();
            OnPropertyChanged(nameof(Items));
        }

        public void MarkDone(ActionItem item, DateTime? doneDate = null)
        {
            _api.MarkDone(doneDate, item);
            OnPropertyChanged(nameof(Items));
        }

        public void Undo(ActionItem item, string context = "inbox")
        {
            _api.Undo(context, item);
            OnPropertyChanged(nameof(Items));
        }

        private void CloseExecute()
        {
            // Confirm close if unsaved changes.
            switch (ConfirmSaveCancel("You have unsaved changes. Save before quitting?"))
            {
                case null:
                    return;
                case true:
                    _api.Save();
                    break;
            }
            Application.Current.Shutdown();
        }

        public void Update(ActionItem item)
        {
            _api.Update(item);
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        public void Move(ActionItem source, string newContext, bool disconnectChildren = true)
        {
            _api.SetContext(source, newContext);
            if (disconnectChildren)
            {
                _api.ResetPriorityParents(source);
            }
            OnPropertyChanged(nameof(Contexts));
            OnPropertyChanged(nameof(Items));
        }

        public void Defer(ActionItem item)
        {
            _api.Defer(item);
            OnPropertyChanged(nameof(Items));
        }

        public void Undefer(ActionItem item)
        {
            _api.Undefer("inbox", item);
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(Contexts));
        }

        private void AddExecuted()
        {
            // Show the Edit view.
            var editWindow = new EditItemView();
            var item = new ActionItem
            {
                Context = SelectedContext?.Title ?? "inbox"
            };
            var editVm = new EditViewModel(this, new ActionViewItem(item, this), editWindow);
            editWindow.DataContext = editVm;
            editWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            editWindow.Owner = Window;
            var result = editWindow.ShowDialog();
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
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(Contexts));
            }
        }

        public void MoveAll(string context, string newContext)
        {
            _api.SearchSpecification = new ContextSearchSpecification(context);
            _api.ShowHeadOnly = false;
            foreach (var item in _api.SearchResults.ToList())
            {
                _api.SetContext(item, newContext);
            }
            _api.ShowHeadOnly = true;
            OnPropertyChanged(nameof(Contexts));
            OnPropertyChanged(nameof(Items));
        }

        public void Delete(ActionItem source)
        {
            _api.ResetPriorityParents(source);
            _api.Delete(source);
            OnPropertyChanged(nameof(Items));
        }

        protected override void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            // Cached properties.
            if (propertyName == nameof(Items) || propertyName == nameof(ShowHeadOnly))
            {
                _currentItems = null;
            }
            else if (propertyName == nameof(Contexts))
            {
                _contexts = null;
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
                // TODO: Raise an event that the view can respond to, or depend on this event.
                (Window as TaskList)?.ResizeColumns();
            }

            if (propertyName == nameof(SortByUpvotes) || propertyName == nameof(SortByOrder) ||
                propertyName == nameof(SortByTitle))
            {
                OnPropertyChanged(nameof(Items));
            }
        }

        private bool? ConfirmSaveCancel(string message)
        {
            if (!HasUnsavedChanges) return false;
            var result = MessageBox.Show(message, "Save changes?", MessageBoxButton.YesNoCancel);
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

        private void OpenRecentFile(string filename)
        {
            switch (ConfirmSaveCancel("You have unsaved changes. Save before opening this file?"))
            {
                case null:
                    return;
                case true:
                    _api.Save();
                    break;
            }
            OpenFile(filename);
        }

        private void ApplySearchExecuted()
        {
            var specs = new List<ISearchSpecification<ActionItem>>();
            if (!string.IsNullOrEmpty(SearchKeyword))
            {
                specs.Add(new FullTextSearchSpecification(SearchKeyword));
            }

            if (!string.IsNullOrEmpty(SearchMissingTagName))
            {
                specs.Add(new NotSpecification<ActionItem>(new TagValueSpecification(SearchMissingTagName, null)));
            }

            if (SearchProject != null)
            {
                specs.Add(new ProjectChildrenSearchSpecification(SearchProject));
            }

            if (SearchContext != null)
            {
                specs.Add(new ContextSearchSpecification(SearchContext.Title));
            }
            _searchResultsContext.SearchSpecification = new AndSpecification<ActionItem>(specs.ToArray());
            SelectedContext = _searchResultsContext;
        }

        private void SearchExecuted()
        {
            SearchExpanded = !SearchExpanded;
            if (SearchExpanded)
            {
                (Window as TaskList)?.KeywordSearchBox.Focus();
            }
        }

        public void CheckForContext(string context)
        {
            if (_contexts.All(c => c.Title != context))
            {
                // Context not found. Refresh.
                OnPropertyChanged(nameof(Contexts));
            }
        }

        #endregion

        #region Properties

        public List<Context> Contexts
        {
            get
            {
                if (_api == null) return new List<Context>();
                if (_contexts == null)
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
                            Window = Window,
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
                    _contexts.Add(new Context { Title = "done", Window = Window, DateVisible = Visibility.Visible, DateColumnTitle = "Done Date" });
                    _contexts.Add(new Context { Title = "someday", Window = Window, DateVisible = Visibility.Visible, DateColumnTitle = "Return Date" });
                    _searchResultsContext = new Context
                    {
                        Title = "Search",
                        Window = Window,
                        DateVisible = Visibility.Collapsed,
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

        public Context SelectedContext
        {
            get => _selectedContext;
            set
            {
                _selectedContext = value;
                OnPropertyChanged();
                _currentItems = null;
                OnPropertyChanged(nameof(Items));
            }
        }

        public ActionViewItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public bool HasUnsavedChanges => _api?.UnsavedChanges ?? false;

        public List<ActionViewItem> Items
        {
            get
            {
                if (SelectedContext == null) return new List<ActionViewItem>();
                if (_currentItems != null) return _currentItems;
                switch (SelectedContext.Title)
                {
                    case "done":
                        _api.DoneSearchSpecification = SelectedContext.SearchSpecification;
                        _currentItems = _api.DoneSearchResults.Select(s => new ActionViewItem(s, this)).OrderByDescending(i => i.DoneDate).ThenBy(i => i.Title).ToList();
                        break;
                    case "someday":
                        _api.SomedaySearchSpecification = SelectedContext.SearchSpecification;
                        _currentItems = _api.SomedaySearchResults.Select(s => new ActionViewItem(s, this)).OrderBy(i => i.TickleDate).ThenBy(i => i.Title).ToList();
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
                                _currentItems = selectedItems.OrderByDescending(i => i.Upvotes).ThenBy(i => i.Title).ToList();
                            }
                            else if (SortByTitle)
                            {
                                _currentItems = selectedItems.OrderBy(i => i.Title).ToList();
                            }
                            else if (SortByOrder)
                            {
                                _currentItems = selectedItems.OrderBy(a => a.Tags.ContainsKey("order") ? a.Tags["order"] : "0", new SemiNumericComparer()).ToList();
                            }
                        }
                        break;
                    default:
                        _api.SearchSpecification = SelectedContext.SearchSpecification;
                        // Apply user-specified sorting options.
                        var defaultItems = _api.SearchResults.Select(s => new ActionViewItem(s, this));
                        if (SortByUpvotes)
                        {
                            _currentItems = defaultItems.OrderByDescending(i => i.Upvotes).ThenBy(i => i.Title).ToList();
                        }
                        else if (SortByTitle)
                        {
                            _currentItems = defaultItems.OrderBy(i => i.Title).ToList();
                        }
                        else if (SortByOrder)
                        {
                            _currentItems = defaultItems.OrderBy(a => a.Tags.ContainsKey("order") ? a.Tags["order"] : "0", new SemiNumericComparer()).ToList();
                        }
                        break;
                }
                return _currentItems;
            }
        }

        public string WindowTitle => $"{(HasUnsavedChanges ? "*" : "")}TodoSort - {FileName}";

        public ObservableCollection<string> RecentFileList
        {
            get => Settings.Default.RecentFiles ?? (Settings.Default.RecentFiles = new ObservableCollection<string>());
            set
            {
                Settings.Default.RecentFiles = value;
                OnPropertyChanged();
            }
        }

        public List<ActionItem> Projects => _api != null ? _api.GetProjects().Union(new List<ActionItem> { null }).OrderBy(p => p?.Title).ToList() : new List<ActionItem> { null };

        public string VersionNumber => "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public bool ShowHeadOnly
        {
            get => _api?.ShowHeadOnly ?? true;
            set
            {
                if (_api.ShowHeadOnly == value) return;
                _api.ShowHeadOnly = value;
                OnPropertyChanged();
            }
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (_searchKeyword == value) return;
                _searchKeyword = value;
                OnPropertyChanged();
            }
        }

        public string SearchMissingTagName
        {
            get => _searchMissingTagName;
            set
            {
                if (_searchMissingTagName == value) return;
                _searchMissingTagName = value;
                OnPropertyChanged();
            }
        }

        public ActionItem SearchProject
        {
            get => _searchProject;
            set
            {
                if (Equals(_searchProject, value)) return;
                _searchProject = value;
                OnPropertyChanged();
            }
        }

        public bool SearchExpanded
        {
            get => _searchExpanded;
            set
            {
                if (_searchExpanded == value) return;
                _searchExpanded = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        public IRepository<ActionItem> Repository => _repo;

        public ViewModel Api => _api;

        public Context SearchContext { get; set; }

        public bool SortByUpvotes
        {
            get => _sortByUpvotes;
            set
            {
                if (_sortByUpvotes == value) return;
                _sortByUpvotes = value;
                if (value)
                {
                    SortByTitle = false;
                    SortByOrder = false;
                }
                OnPropertyChanged(nameof(SortByUpvotes));
            }
        }

        public bool SortByTitle
        {
            get => _sortByTitle;
            set
            {
                if (_sortByTitle == value) return;
                _sortByTitle = value;
                if (value)
                {
                    SortByUpvotes = false;
                    SortByOrder = false;
                }
                OnPropertyChanged(nameof(SortByTitle));
            }
        }

        public bool SortByOrder
        {
            get => _sortByOrder;
            set
            {
                if (_sortByOrder == value) return;
                _sortByOrder = value;
                if (value)
                {
                    SortByTitle = false;
                    SortByUpvotes = false;
                }
                OnPropertyChanged(nameof(SortByOrder));
            }
        }

        #endregion

        #region Commands

        public RelayCommand<string> OpenRecentCommand => _openRecentCommand ?? (_openRecentCommand = new RelayCommand<string>(OpenRecentFile));

        public RelayCommand RankCommand => _rankCommand ?? (_rankCommand = new RelayCommand(RankItems));

        public RelayCommand ReloadCommand => _reloadCommand ?? (_reloadCommand = new RelayCommand(ReloadFile, () => !string.IsNullOrEmpty(FileName)));

        public RelayCommand CloseCommand => _closeCommand ?? (_closeCommand = new RelayCommand(CloseExecute));

        public RelayCommand AddItemCommand => _addItemCommand ?? (_addItemCommand = new RelayCommand(AddExecuted));

        public RelayCommand OpenFileCommand => _openFileCommand ?? (_openFileCommand = new RelayCommand(OpenCommandExecuted));

        public RelayCommand SaveFileCommand => _saveFileCommand ?? (_saveFileCommand = new RelayCommand(SaveCommandExecuted));

        public RelayCommand ApplySearchCommand => _applySearchCommand ?? (_applySearchCommand = new RelayCommand(ApplySearchExecuted));

        public ICommand CleanupCommand => _cleanupCommand ?? (_cleanupCommand = new RelayCommand(Cleanup));

        public ICommand CommitCommand => _commitCommand ?? (_commitCommand = new RelayCommand(CommitChanges));

        public ICommand MaskTextCommand => _maskTextCommand ?? (_maskTextCommand = new RelayCommand(() => Masked = !Masked));

        public ICommand ToggleHeadCommand => _toggleHeadCommand ?? (_toggleHeadCommand = new RelayCommand(() => ShowHeadOnly = !ShowHeadOnly));

        public ICommand SearchCommand => _searchCommand ?? (_searchCommand = new RelayCommand(SearchExecuted));

        public ICommand NewFileCommand => _newFileCommand ?? (_newFileCommand = new RelayCommand(NewFileExecuted));

        #endregion
    }
}
