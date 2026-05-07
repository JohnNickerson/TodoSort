using System.Windows;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : Window, Interfaces.IDialogWindow
    {
        public ImportView()
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
