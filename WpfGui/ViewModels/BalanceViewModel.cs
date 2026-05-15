using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.Core.Search;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.ViewModels
{
    public class BalanceViewModel : ObservableObject
    {
        private List<BranchOption> _branchOptions;
        private RelayCommand _goCommand;
        private RelayCommand _cancelCommand;
        public IDialogWindow _view;
        private ViewModel _vm;

        public BalanceViewModel(IDialogWindow view, Core.ViewModel vm)
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
                    ResultCount = "Pending"
                });
            }

            BranchOptions = contexts;
        }

        private void RebalanceExecuted()
        {
            var balanceThread = new Thread(Rebalance) { Priority = ThreadPriority.BelowNormal };
            balanceThread.Start();
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
                    _vm.Balance(vine, context.BranchFactor);
                    context.ResultCount = vine.Length.ToString();
                }
                else
                {
                    context.ResultCount = "Skipped";
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

        public ICommand GoCommand => _goCommand ?? (_goCommand = new RelayCommand(RebalanceExecuted));

        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new RelayCommand(CancelExecuted));
    }
}
