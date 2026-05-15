using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class ContextViewModel : ObservableObject
    {
        #region Fields
        public ITaskListView View { get; set; }
        private string _title;
        private ISearchSpecification<ActionItem> _searchSpecification;
        private string _dateColumnTitle;
        private Visibility _dateVisible;
        private RelayCommand<ContextViewModel> _moveAllCommand;
        #endregion

        #region Methods
        private void MoveAllExecuted(ContextViewModel fromContext)
        {
            if (fromContext != null)
                ParentVm.MoveAll(fromContext.Title, _title);
        }
        #endregion

        #region Properties
        public string DateColumnTitle
        {
            get => _dateColumnTitle;
            set
            {
                if (value == _dateColumnTitle) return;
                _dateColumnTitle = value;
                OnPropertyChanged();
            }
        }

        public Visibility DateVisible
        {
            get => _dateVisible;
            set
            {
                if (value == _dateVisible) return;
                _dateVisible = value;
                OnPropertyChanged();
            }
        }

        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get => _searchSpecification;
            set
            {
                if (Equals(value, _searchSpecification)) return;
                _searchSpecification = value;
                OnPropertyChanged();
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearch => Title.Equals("Search", StringComparison.CurrentCultureIgnoreCase);

        public List<ContextViewModel> AllOtherContexts { get; set; }

        public MainViewModel ParentVm { get; set; }

        public bool CanMoveFrom { get; set; } = true;
        #endregion

        #region Commands

        public ICommand MoveAllCommand => _moveAllCommand ?? (_moveAllCommand = new RelayCommand<ContextViewModel>(MoveAllExecuted));

        #endregion
    }
}
