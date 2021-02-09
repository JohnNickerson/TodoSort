using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.WpfGui.Annotations;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class BalanceViewModel : INotifyPropertyChanged
    {
        private List<BranchOption> _branchOptions;
        private RelayCommand _goCommand;
        private RelayCommand _cancelCommand;
        public event PropertyChangedEventHandler PropertyChanged;
        public Window _view;
        private ViewModel _vm;

        public BalanceViewModel(Window view, Core.ViewModel vm)
        {
            _view = view;
            _vm = vm;
            var contexts = new List<BranchOption>();
            foreach (var contextName in _vm.GetContextNames("Search", "someday", "done"))
            {
                contexts.Add(new BranchOption
                {
                    BranchFactor = 4,
                    ContextName = contextName,
                    IsSelected = true,
                    ResultCount = 0
                });
            }

            BranchOptions = contexts;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Rebalance()
        {
            _vm.ShowHeadOnly = false;
            foreach (var context in BranchOptions)
            {
                if (context.IsSelected && context.BranchFactor > 0)
                {
                    _vm.SearchSpecification = new ContextSearchSpecification(context.ContextName);
                    var depths = _vm.GetDepthsView();
                    var vine = _vm.SearchResults.OrderBy(i => depths[i.ID]).ThenByDescending(i => i.Upvotes).ToArray();
                    context.ResultCount = vine.Length;
                    _vm.Balance(vine, context.BranchFactor);
                }
                else
                {
                    context.ResultCount = 0;
                }
            }
            _vm.ShowHeadOnly = true;
        }

        private void CancelExecuted()
        {
            // Close the window.
            _view.Close();
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
