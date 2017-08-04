using System;
using System.Collections.Generic;
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
using AssimilationSoftware.TodoSort.WpfGui.Properties;
using AssimilationSoftware.TodoSort.Core;

namespace AssimilationSoftware.TodoSort.WpfGui
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
        private ViewModel vm;

		public Window1()
		{
			InitializeComponent();

            if (Settings.Default.Reconfigure)
            {
                Reconfigure(this, null);
            }
            RefreshViewModel();
		}

        private void RefreshViewModel()
        {
            vm = new ViewModel(new ActionItemDiskMapper(Settings.Default.Todo));
            this.DataContext = vm;
        }

        public void Reconfigure(object sender, RoutedEventArgs args)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            dlg.Title = "Todo file";

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                Settings.Default.Todo = dlg.FileName;
            }

            dlg.Title = "Done file";
            result = dlg.ShowDialog();
            if (result == true)
            {
                Settings.Default.Done = dlg.FileName;
            }

            dlg.Title = "Someday file";
            result = dlg.ShowDialog();
            if (result == true)
            {
                Settings.Default.Someday = dlg.FileName;
            }

            Settings.Default.Reconfigure = false;
            Settings.Default.Save();
            RefreshViewModel();
        }

        private void ItemDisplay_DeleteItem(object sender, ActionItemEventArgs e)
        {
            vm.Delete(e.Item);
        }

        private void ItemDisplay_MarkDone(object sender, ActionItemEventArgs e)
        {
            vm.MarkDone(null, e.Item);
        }

        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            vm.Save();
        }
	}
}
