using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AssimilationSoftware.PimData.Mappers.Text;
using AssimilationSoftware.PimData.Model;
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

        #endregion

        public MainViewModel(string filename)
        {
            if (filename != null)
            {
                OpenFile(filename);
            }
        }

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

            _repo = new TodoRepository(new ActionItemDiskMapper(FileName));
            _api = new ViewModel(_repo);

            // Process pending items for any to return to the main list.
            _api.SomedaySearchSpecification = new TickleDateSearchSpecification(null, DateTime.Today);
            _api.Undefer("inbox", _api.SomedaySearchResults.ToArray());

            _contexts = null;
            OnPropertyChanged(nameof(Contexts));
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(RecentFileList));
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void RankItems()
        {
            // Open up a ranking window.
            var rv = new RankView();
            var rvm = new RankViewModel(Items, rv, _api);
            rv.DataContext = rvm;
            rv.ShowDialog();
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void ReloadFile()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                OpenFile(FileName);
            }
        }

        private void SaveSettings()
        {
            Settings.Default.Save();
        }

        public void OpenUrlCommandExecuted(ActionItem item)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = item.Tags["url"];
            p.Start();
        }

        public void OpenCommandExecuted(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            dlg.Title = "Todo file";

            // Show open file dialog box
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                OpenFile(dlg.FileName);
            }
        }

        public void SaveCommandExecuted(object sender, RoutedEventArgs e)
        {
            _api.Save();
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(WindowTitle));
        }

        public void MarkDone(ActionItem item, DateTime? doneDate = null)
        {
            _api.MarkDone(doneDate, item);
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(WindowTitle));
        }

        public void Undo(ActionItem item, string context = "inbox")
        {
            _api.Undo(context, item);
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void CloseExecute()
        {
            // Confirm close if unsaved changes.
            if (HasUnsavedChanges)
            {
                var result = MessageBox.Show("You have unsaved changes. Save and quit?", "Unsaved changes", MessageBoxButton.OKCancel);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        SaveCommandExecuted(this, null);
                        Application.Current.Shutdown();
                        break;
                    case MessageBoxResult.Cancel:
                        break;
                }

            }
            else
            {
                // No unsaved changes. Just quit.
                Application.Current.Shutdown();
            }
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
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(WindowTitle));
        }

        public void Defer(ActionItem item)
        {
            _api.Defer(item);
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(WindowTitle));
        }

        public void Undefer(ActionItem item)
        {
            _api.Undefer("inbox", item);
            _currentItems = null;
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(Contexts));
        }

        private void AddExecuted()
        {
            // Show the Edit view.
            var editWindow = new EditItemView();
            var item = new ActionItem("inbox", null);
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
                item.Project = editVm.Project;
                item.Context = editVm.Context;
                if (editVm.IsDeferred)
                {
                    item.TickleDate = editVm.TickleDate;
                }
                _api.AddItem(item);
                _currentItems = null;
                OnPropertyChanged(nameof(Items));
                _contexts = null;
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
                    _contexts = new List<Context>();
                    foreach (var con in _api.GetContextNames("done", "someday").OrderBy(c => c))
                    {
                        _contexts.Add(new Context
                        {
                            Title = con,
                            SearchSpecification = new ContextSearchSpecification(con),
                            Window = Window,
                            DateVisible = Visibility.Collapsed
                        });
                    }
                    _contexts.Add(new Context { Title = "done", Window = Window, DateVisible = Visibility.Visible, DateColumnTitle = "Done Date" });
                    _contexts.Add(new Context { Title = "someday", Window = Window, DateVisible = Visibility.Visible, DateColumnTitle = "Return Date" });
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
                    default:
                        _api.SearchSpecification = SelectedContext.SearchSpecification;
                        _currentItems = _api.SearchResults.Select(s => new ActionViewItem(s, this)).OrderByDescending(i => i.Upvotes).ThenBy(i => i.Title).ToList();
                        break;
                }
                return _currentItems;
            }
        }

        public string WindowTitle => $"TodoSort - {FileName} {(HasUnsavedChanges ? "*" : "")}";

        public ObservableCollection<string> RecentFileList
        {
            get => Settings.Default.RecentFiles ?? (Settings.Default.RecentFiles = new ObservableCollection<string>());
            set
            {
                Settings.Default.RecentFiles = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand<string> OpenRecentCommand => _openRecentCommand ?? (_openRecentCommand = new RelayCommand<string>(OpenFile));

        public RelayCommand RankCommand => _rankCommand ?? (_rankCommand = new RelayCommand(RankItems));

        public RelayCommand ReloadCommand => _reloadCommand ?? (_reloadCommand = new RelayCommand(ReloadFile, () => !string.IsNullOrEmpty(FileName)));

        public RelayCommand CloseCommand => _closeCommand ?? (_closeCommand = new RelayCommand(CloseExecute));

        public RelayCommand AddItemCommand => _addItemCommand ?? (_addItemCommand = new RelayCommand(AddExecuted));

        public List<ActionItem> Projects => _api != null ? _api.GetProjects() : new List<ActionItem>();

        public string VersionNumber => "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        #endregion
    }
}
