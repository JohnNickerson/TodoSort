using System.Windows.Input;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Model;
using AssimilationSoftware.TodoSort.WpfGui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class RankViewModel : ObservableObject
    {
        #region Fields

        private int _currentIndex;
        private IDialogWindow _view;
        private Core.ViewModel _api;

        private RelayCommand _backCommand;
        private RelayCommand _forwardCommand;
        private RelayCommand _vote1Command;
        private RelayCommand _vote2Command;
        private RelayCommand _okCommand;

        #endregion

        #region Constructors

        public RankViewModel(List<ActionViewModel> items, IDialogWindow rv, Core.ViewModel api)
        {
            _view = rv;
            _api = api;
            // Randomise the list into RankPairs.
            var index = new List<int>();
            for (var dex = 0; dex < items.Count; dex++) index.Add(dex);
            // Randomise an index list
            var rand = new Random();
            for (var dex = 0; dex < index.Count; dex++)
            {
                var r = rand.Next(index.Count);
                var b = index[dex];
                index[dex] = index[r];
                index[r] = b;
            }
            RankPairs = new List<RankPair>();
            for (var x = 0; x < items.Count - 1; x += 2)
            {
                RankPairs.Add(new RankPair
                {
                    Action1 = items.ElementAt(index[x]),
                    Action2 = items.ElementAt(index[x + 1]),
                    Vote = RankWinner.Draw
                });
            }

            CurrentIndex = 0;
        }

        #endregion

        #region Methods

        private void Vote1Execute()
        {
            CurrentPair.Vote = RankWinner.First;
            if (CurrentIndex == RankPairs.Count - 1)
            {
                OkExecute();
            }
            else CurrentIndex++;
        }

        private void Vote2Execute()
        {
            CurrentPair.Vote = RankWinner.Second;
            if (CurrentIndex == RankPairs.Count - 1)
            {
                OkExecute();
            }
            else CurrentIndex++;
        }

        private void OkExecute()
        {
            // Save results and return.
            foreach (var rankPair in RankPairs)
            {
                if (rankPair.Vote != RankWinner.Draw)
                {
                    _api.SetParent(rankPair.Loser.Source, rankPair.Winner.Source);
                }
            }
            // Close the window.
            _view.DialogResult = true;
            _view.Close();
        }

        #endregion

        #region Commands

        public ICommand BackCommand
        {
            get
            {
                return _backCommand ?? (_backCommand = new RelayCommand(() => CurrentIndex--, CanGoBack));
            }
        }

        public bool CanGoBack()
        {
            return CurrentIndex > 0;
        }

        public ICommand ForwardCommand
        {
            get
            {
                return _forwardCommand ?? (_forwardCommand = new RelayCommand(() => CurrentIndex++, () => CurrentIndex < (RankPairs?.Count - 1 ?? 0)));
            }
        }

        public ICommand Vote1Command
        {
            get
            {
                return _vote1Command ?? (_vote1Command = new RelayCommand(Vote1Execute, () => CurrentPair != null));
            }
        }

        public ICommand Vote2Command
        {
            get
            {
                return _vote2Command ?? (_vote2Command = new RelayCommand(Vote2Execute, () => CurrentPair != null));
            }
        }

        public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(OkExecute));

        #endregion

        #region Properties
        public List<RankPair> RankPairs { get; set; }

        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (_currentIndex == value) return;
                if (value >= RankPairs.Count || value < 0) return;
                _currentIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPair));
                // Update command states.
                ((RelayCommand)BackCommand).NotifyCanExecuteChanged();
                ((RelayCommand)ForwardCommand).NotifyCanExecuteChanged();
            }
        }

        public RankPair CurrentPair => RankPairs?[CurrentIndex];

        #endregion
    }

    public class RankPair
    {
        public ActionViewModel Action1 { get; set; }
        public ActionViewModel Action2 { get; set; }

        public RankWinner Vote { get; set; }

        public ActionViewModel Winner
        {
            get
            {
                if (Vote == RankWinner.First)
                {
                    return Action1;
                }
                else if (Vote == RankWinner.Second)
                {
                    return Action2;
                }
                else return null;
            }
        }

        public ActionViewModel Loser
        {
            get
            {
                if (Vote == RankWinner.First)
                {
                    return Action2;
                }
                else if (Vote == RankWinner.Second)
                {
                    return Action1;
                }
                else return null;
            }
        }
    }

    public enum RankWinner
    {
        First,
        Second,
        Draw
    }
}
