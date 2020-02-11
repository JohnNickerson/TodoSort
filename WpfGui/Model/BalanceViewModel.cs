using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AssimilationSoftware.TodoSort.WpfGui.Annotations;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class BalanceViewModel : INotifyPropertyChanged
    {
        private List<BranchOption> _branchOptions;
        private RelayCommand _goCommand;
        private RelayCommand _cancelCommand;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Rebalance()
        {
            throw new NotImplementedException();
        }

        private void CancelExecuted()
        {
            throw new NotImplementedException();
        }

        public List<BranchOption> BranchOptions
        {
            get => _branchOptions;
            set
            {
                if (Equals(value, _branchOptions)) return;
                _branchOptions = value;
                OnPropertyChanged();
            }
        }

        public ICommand GoCommand => _goCommand ?? (_goCommand = new RelayCommand(Rebalance));

        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new RelayCommand(CancelExecuted));
    }
}
