using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AssimilationSoftware.PimData;
using WpfGui.Properties;

namespace WpfGui
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
        private List<ActionItem> _todolist = ActionItem.ReadFile(Settings.Default.TodoFilename);

		public Window1()
		{
			InitializeComponent();


			// Test layout.
            todotree.Items.Clear();
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
            ActionItem item = (ActionItem)((CheckBox)sender).Tag;
            //item.Done();
        }
	}
}
