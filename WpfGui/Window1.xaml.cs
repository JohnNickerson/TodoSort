using System;
using System.Collections.Generic;
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
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
        private MainViewModel vm;

		public Window1()
		{
			InitializeComponent();
		    string lastfile = null;
		    if (Settings.Default.RecentFiles != null && Settings.Default.RecentFiles.Count > 0)
		    {
		        lastfile = Settings.Default.RecentFiles[0];
		    }
		    vm = new MainViewModel(lastfile);
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
	        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
	        e.Handled = true;
	    }
	}
}
