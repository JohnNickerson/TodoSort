using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AssimilationSoftware.TodoSort.CoreGui.Interfaces;
using AssimilationSoftware.TodoSort.WpfGui.Properties;
using AssimilationSoftware.TodoSort.CoreGui.ViewModels;

namespace AssimilationSoftware.TodoSort.WpfGui.Views
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : Window, ITaskListView
    {
        private readonly MainViewModel _vm;

        public TaskList()
        {
            InitializeComponent();
            // TODO: This goes in App.xaml.cs
            var recentFile = Settings.Default.Todo;
            _vm = new MainViewModel(recentFile, new Services.NavigationService(), Settings.Default);
            _vm.Window = this;
            DataContext = _vm;
        }

        public void ResizeColumns()
        {
            foreach (var column in TaskGridView.Columns)
            {
                if (double.IsNaN(column.Width)) column.Width = 1;
                column.Width = double.NaN;
            }
        }

        private void TaskList_OnClosing(object sender, CancelEventArgs e)
        {
            // Confirm close if the ViewModel says there are unsaved changes.
            if (_vm.HasUnsavedChanges)
            {
                var result = System.Windows.MessageBox.Show("You have unsaved changes. Save and quit?", "Unsaved changes", MessageBoxButton.OKCancel);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        // save.
                        _vm.SaveCommandExecuted();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _vm.SelectedItem?.EditCommand.Execute(_vm.SelectedItem);
        }

        private void MainWindow_OnGotFocus(object sender, EventArgs eventArgs)
        {
            ((MainViewModel)DataContext).CheckForUpdatedFile();
            ((MainViewModel)DataContext).CheckForCommit();
            ((MainViewModel)DataContext).CheckForSnoozedItemsOnce();

            // Check window position and bounds.
            if (ActualWidth > SystemParameters.WorkArea.Width)
            {
                Width = SystemParameters.WorkArea.Width;
                Left = 0;
            }
            if (ActualHeight > SystemParameters.WorkArea.Height)
            {
                Height = SystemParameters.WorkArea.Height;
                Top = 0;
            }
            if (Left + ActualWidth > SystemParameters.WorkArea.Width)
            {
                Left = Math.Max(0, SystemParameters.WorkArea.Width - Width);
            }
            if (Top + ActualHeight > SystemParameters.WorkArea.Height)
            {
                Top = Math.Max(0, SystemParameters.WorkArea.Height - ActualHeight);
            }
        }

        private void SearchVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KeywordSearchBox.Focus();
        }
    }
}
