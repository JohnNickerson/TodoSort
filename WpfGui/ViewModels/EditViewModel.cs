using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.ViewModels
{
    public class EditViewModel : ObservableObject
    {
        #region Fields

        private string? _title;
        private string? _notes;
        private ObservableCollection<TagValueModel> _tags;
        private string? _context;
        private ActionItem? _project;
        private DateTime? _tickleDate;
        private readonly string[] _excludeContexts = { "done", "someday", "Search" };

        private ICommand _okCommand;
        private ICommand _addTagCommand;
        private IDialogWindow _view;

        #endregion

        #region Constructors

        public EditViewModel(MainViewModel api, ActionViewItem item, IDialogWindow view)
        {
            AllContexts = api.Contexts.Where(c => !_excludeContexts.Contains(c.Title)).Select(c => c.Title).OrderBy(c => c).ToList();
            AllProjects = api.Projects.Where(p => p != null).OrderBy(p => p.Title).ToList();
            _view = view;

            if (item != null)
            {
                _title = item.Source.Title;
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
                _project = item.Source.GetProject(api.Repository);
                _tickleDate = item.Source.TickleDate;
            }
        }

        #endregion

        #region Methods

        private void OkExecuted()
        {
            _view.DialogResult = true;
            _view.Close();
        }

        private void AddTagExecuted()
        {
            Tags.Add(new TagValueModel { Item = this });
        }

        #endregion

        #region Properties

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes == value) return;
                _notes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TagValueModel> Tags
        {
            get => _tags;
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
            get => _context;
            set
            {
                if (_context == value) return;
                _context = value;
                OnPropertyChanged();
            }
        }

        // TODO: Consider changing to ProjectId
        public ActionItem? Project
        {
            get => _project;
            set
            {
                if (_project != null && _project.Equals(value)) return;
                _project = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProject));
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

        public bool HasTickleDate
        {
            get => TickleDate != null;
            set
            {
                if (!value) TickleDate = null;
            }
        }

        public DateTime? TickleDate
        {
            get => _tickleDate;
            set
            {
                if (_tickleDate == value) return;
                _tickleDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTickleDate));
            }
        }

        public bool IsDeferred => Context == "someday";

        public bool IsNotDeferred => Context != "someday";

        #endregion
    }
}
