using System.Windows;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for RankView.xaml
    /// </summary>
    public partial class RankView : Window, Interfaces.IDialogWindow
    {
        public RankView()
        {
            InitializeComponent();
        }

        public bool? ShowDialog(Interfaces.ITaskListView parent)
        {
            Owner = parent as Window;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return base.ShowDialog();
        }
    }
}
