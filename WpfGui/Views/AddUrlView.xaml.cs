using System.Windows;

namespace AssimilationSoftware.TodoSort.WpfGui.Views;

public partial class AddUrlView : Window, Interfaces.IDialogWindow
{
    public AddUrlView()
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