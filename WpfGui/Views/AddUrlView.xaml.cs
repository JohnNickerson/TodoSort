using System.Windows;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;

namespace AssimilationSoftware.TodoSort.WpfGui.Views;

public partial class AddUrlView : Window, IDialogWindow
{
    public AddUrlView()
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