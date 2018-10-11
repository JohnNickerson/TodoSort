using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Mappers.Text;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Data;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.WpfGui.Properties;
using Microsoft.Win32;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _selectedContext;
        private string _fileName;
        private bool _hasUnsavedChanges;
        private IPimDataMapper<ActionItem> _mapper;
        private ITodoRepository _repo;
        private ViewModel _api;
        public event PropertyChangedEventHandler PropertyChanged;
        private RelayCommand<string> _openRecentCommand;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

            _mapper = new ActionItemDiskMapper(FileName);
            _repo = new Core.Data.TodoRepository(_mapper);
            _api = new Core.ViewModel(_mapper);
            OnPropertyChanged("Contexts");
            OnPropertyChanged("Items");
            OnPropertyChanged("RecentFileList");
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
            OnPropertyChanged("Items");
        }

        #endregion

        #region Properties

        public List<string> Contexts => _api != null ? _api.GetContextNames("done", "someday").Union(new[] { "done", "someday" }).ToList() : new List<string>();

        public string SelectedContext
        {
            get => _selectedContext;
            set
            {
                _selectedContext = value;
                OnPropertyChanged();
                OnPropertyChanged("Items");
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged();
                OnPropertyChanged("WindowTitle");
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged();
                OnPropertyChanged("WindowTitle");
            }
        }

        public List<ActionViewItem> Items
        {
            get
            {
                if (SelectedContext == null) return new List<ActionViewItem>();
                switch (SelectedContext)
                {
                    case "done":
                        return _api.DoneSearchResults.Select(s => new ActionViewItem(s, _api)).OrderByDescending(i => i.DoneDate).ToList();
                    case "someday":
                        return _api.SomedaySearchResults.Select(s => new ActionViewItem(s, _api)).OrderBy(i => i.TickleDate).ToList();
                    default:
                        _api.SearchSpecification = new ContextSearchSpecification(SelectedContext);
                        return _api.SearchResults.Select(s => new ActionViewItem(s, _api)).OrderByDescending(i => i.Upvotes).ToList();
                }
            }
        }

        public string WindowTitle => $"TodoSort {FileName} {(HasUnsavedChanges ? "*" : "")}";

        public List<string> RecentFileList
        {
            get => Settings.Default.RecentFiles ?? (Settings.Default.RecentFiles = new List<string>());
            set
            {
                Settings.Default.RecentFiles = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand<string> OpenRecentCommand =>
            _openRecentCommand ?? (_openRecentCommand = new RelayCommand<string>(OpenFile));

        #endregion
    }
}
