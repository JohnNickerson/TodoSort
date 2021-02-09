using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AssimilationSoftware.TodoSort.WpfGui.Annotations;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class BranchOption : INotifyPropertyChanged
    {
        private int _branchFactor;
        private bool _isSelected;
        private string _contextName;
        private int _resultCount;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ContextName
        {
            get => _contextName;
            set
            {
                if (value == _contextName) return;
                _contextName = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public int BranchFactor
        {
            get => _branchFactor;
            set
            {
                if (value == _branchFactor) return;
                _branchFactor = value;
                OnPropertyChanged();
            }
        }

        public int ResultCount
        {
            get => _resultCount;
            set
            {
                if (value == _resultCount) return;
                _resultCount = value;
                OnPropertyChanged();
            }
        }
    }
}
