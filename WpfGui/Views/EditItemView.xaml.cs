using System.Windows;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for EditItemView.xaml
    /// </summary>
    public partial class EditItemView : Window, IDialogWindow
    {
        public EditItemView()
        {
            InitializeComponent();
        }

        public bool? ShowDialog(ITaskListView parent)
        {
            Owner = parent as Window;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return base.ShowDialog();
        }
    }
}
