using System;
using System.Windows;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using AssimilationSoftware.TodoSort.CoreGui.Model;

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
            FormatComboBox.ItemsSource = Enum.GetValues(typeof(ImportFileType));
        }

        public bool? ShowDialog(ITaskListView parent)
        {
            Owner = parent as Window;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return base.ShowDialog();
        }

    }
}
