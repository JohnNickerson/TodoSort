using System.Windows;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for RankView.xaml
    /// </summary>
    public partial class RankView : Window, IDialogWindow
    {
        public RankView()
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
