using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AssimilationSoftware.PimData;
using AssimilationSoftware.PimData.Mappers;
using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.WpfGui.Properties;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : Window
    {
        private MainViewModel vm;

        public TaskList()
        {
            InitializeComponent();
            string lastfile = null;
            if (Settings.Default.RecentFiles != null && Settings.Default.RecentFiles.Count > 0)
            {
                lastfile = Settings.Default.RecentFiles[0];
            }
            vm = new MainViewModel(lastfile);
            vm.Window = this;
            DataContext = vm;
        }

        private void OpenClick(object sender, RoutedEventArgs e)
        {
            vm.OpenCommandExecuted(sender, e);
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            vm.SaveCommandExecuted(sender, e);
        }

        private void OpenUrlClick(object sender, RequestNavigateEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Uri.OriginalString))
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                e.Handled = true;
            }
        }

        private void TaskList_OnClosing(object sender, CancelEventArgs e)
        {
            // Confirm close if the ViewModel says there are unsaved changes.
            if (vm.HasUnsavedChanges)
            {
                e.Cancel = MessageBox.Show("You have unsaved changes. Quit without saving?", "Unsaved changes", MessageBoxButton.YesNo) != MessageBoxResult.Yes;
            }
        }
    }
}
