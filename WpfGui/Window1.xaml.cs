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
using WpfGui.Properties;
using AssimilationSoftware.PimData.Mappers;

namespace WpfGui
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
        private List<ActionItem> _todolist;
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
            _todolist = new ListItemDiskMapper().Deserialise(Settings.Default.Todo);
            _donelist = new ListItemDiskMapper().Deserialise(Settings.Default.Done);

            // Display list.
            ItemDisplay d = new ItemDisplay();
            somedayGrid.Children.Add(d);
            d.DataContext = _todolist[0];
		}

        private void RefreshTree()
        {
            todotree.Items.Clear();
            _todolist = new ListItemDiskMapper().Deserialise(Settings.Default.Todo);
            // foreach Context in todolist
            foreach (var c in (from a in _todolist orderby a.Context select a.Context).Distinct())
            {
                // Add a TreeViewItem with Header=@Context
                TreeViewItem context = new TreeViewItem();
                context.Header = c;
                todotree.Items.Add(context);
                // foreach Item in Context
                foreach (var t in (from a in _todolist where a.Context == c orderby a.Title select a))
                {
                    // Add a Checkbox to the TreeViewItem with Content=Item Text
                    CheckBox item = new CheckBox();
                    context.Items.Add(item);
                    item.Content = t.Title;
                    item.Tag = t;

                    item.Checked += new RoutedEventHandler(item_Checked);
                }
            }
        }

        void item_Checked(object sender, RoutedEventArgs e)
        {
            ListItemDiskMapper mapper = new ListItemDiskMapper();
            ActionItem item = (ActionItem)((CheckBox)sender).Tag;
            item.Done(_todolist, _donelist);
            mapper.Serialise(Settings.Default.Todo, _todolist);
            mapper.Serialise(Settings.Default.Done, _donelist);
            RefreshTree();
        }
	}
}
