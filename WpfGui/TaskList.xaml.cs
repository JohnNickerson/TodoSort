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
using AssimilationSoftware.Maroon;
using AssimilationSoftware.Maroon.Mappers;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core;
using AssimilationSoftware.TodoSort.WpfGui.Properties;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : Window
    {
        private readonly MainViewModel _vm;

        public TaskList()
        {
            InitializeComponent();
            // TODO: This goes in App.xaml.cs
            var recentFile = Settings.Default.RecentFiles?.FirstOrDefault();
            _vm = new MainViewModel(recentFile);
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
                var result = MessageBox.Show("You have unsaved changes. Save and quit?", "Unsaved changes", MessageBoxButton.OKCancel);
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
            _vm.SelectedItem.EditCommand.Execute(_vm.SelectedItem);
        }
    }
}
