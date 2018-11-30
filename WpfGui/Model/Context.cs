using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class Context : ViewModelBase
    {
        private string _title;
        private ISearchSpecification<ActionItem> _searchSpecification;
        private string _dateColumnTitle;
        private Visibility _dateVisible;
        private RelayCommand<Context> _moveAllCommand;

        private void MoveAllExecuted(Context fromContext)
        {
            if (fromContext != null)
                ParentVm.MoveAll(fromContext.Title, _title);
        }

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

        public List<Context> AllOtherContexts { get; set; }

        public MainViewModel ParentVm { get; set; }

        public ICommand MoveAllCommand => _moveAllCommand ?? (_moveAllCommand = new RelayCommand<Context>(MoveAllExecuted));
    }
}
