using System.Windows;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : Window, IDialogWindow
    {
        public ImportView()
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
