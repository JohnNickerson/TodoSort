using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AssimilationSoftware.TodoSort.WpfGui.Model
{
    public class TagValueModel : ViewModelBase
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
