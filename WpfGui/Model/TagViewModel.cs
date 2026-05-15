using System.Windows.Input;
using AssimilationSoftware.TodoSort.WpfGui.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class TagViewModel : ObservableObject
    {
        #region Fields
        private string _tag;
        private string _value;
        private ICommand _deleteTagCommand;
        #endregion

        #region Methods
        private void DeleteTagExecuted()
        {
            Item.Tags.Remove(this);
        }
        #endregion

        #region Properties

        public string Tag
        {
            get => _tag;
            set
            {
                if (_tag == value) return;
                _tag = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public ICommand DeleteTagCommand => _deleteTagCommand ?? (_deleteTagCommand = new RelayCommand(DeleteTagExecuted));

        public EditViewModel Item { get; set; }

        #endregion
    }
}
