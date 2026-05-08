using System.Windows;
using AssimilationSoftware.TodoSort.WpfGui.Interfaces;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for BalanceView.xaml
    /// </summary>
    public partial class BalanceView : Window, IDialogWindow
    {
        public BalanceView()
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
