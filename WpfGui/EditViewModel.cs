using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.WpfGui.Model;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class EditViewModel : ViewModelBase
    {
        #region Fields

        private string _title;
        private string _notes;
        private ObservableCollection<TagValueModel> _tags;
        private string _context;
        private ActionItem _project;

        private ICommand _okCommand;
        private ICommand _addTagCommand;

        #endregion

        #region Constructors

        public EditViewModel(MainViewModel api, ActionViewItem item, Window window)
        {
            AllContexts = api.Contexts.Except(new[] { "done", "someday" }).OrderBy(c => c).ToList();
            AllProjects = api.Projects.OrderBy(p => p.Title).ToList();
            Window = window;

            if (item != null)
            {
                _title = item.Title;
                _notes = string.Join(Environment.NewLine, item.Notes);
                _tags = new ObservableCollection<TagValueModel>();
                foreach (var itemTag in item.Tags)
                {
                    _tags.Add(new TagValueModel
                    {
                        Tag = itemTag.Key,
                        Value = itemTag.Value,
                        Item = this
                    });
                }
                _context = item.Source.Context;
                _project = item.Source.Project;
            }
        }

        #endregion

        #region Methods

        private void OkExecuted()
        {
            Window.DialogResult = true;
            Window.Close();
        }

        private void AddTagExecuted()
        {
            Tags.Add(new TagValueModel { Item = this });
        }

        #endregion

        #region Properties

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Notes
        {
            get { return _notes; }
            set
            {
                if (_notes == value) return;
                _notes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TagValueModel> Tags
        {
            get { return _tags; }
            set
            {
                if (_tags.Equals(value)) return;
                _tags = value;
                OnPropertyChanged();
            }
        }

        public List<string> AllContexts { get; }

        public List<ActionItem> AllProjects { get; }

        public string Context
        {
            get { return _context; }
            set
            {
                if (_context == value) return;
                _context = value;
                OnPropertyChanged();
            }
        }

        public ActionItem Project
        {
            get { return _project; }
            set
            {
                if (_project != null && _project.Equals(value)) return;
                _project = value;
                OnPropertyChanged();
            }
        }

        public bool HasProject
        {
            get => Project != null;
            set
            {
                // Can only clear project this way, not set it.
                if (!value) Project = null;
            }
        }

        public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(OkExecuted));

        public ICommand AddTagCommand => _addTagCommand ?? (_addTagCommand = new RelayCommand(AddTagExecuted));

        #endregion
    }
}
