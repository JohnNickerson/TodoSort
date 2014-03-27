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
using AssimilationSoftware.TodoSort.Core.Mappers;

namespace AssimilationSoftware.TodoSort.WpfGui
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
        private ViewModel vm;

        [Obsolete]
        private List<ActionItem> _todolist;
        [Obsolete]
        private List<ActionItem> _donelist;

		public Window1()
		{
			InitializeComponent();

            if (Settings.Default.Reconfigure)
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
            }
            vm = new ViewModel(new TodoTxtFileMapper(Settings.Default.Todo), new TodoTxtFileMapper(Settings.Default.Done), new TodoTxtFileMapper(Settings.Default.Someday));
            this.DataContext = vm;
		}
	}
}
