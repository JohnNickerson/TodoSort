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

namespace WpfGui
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();

			// Test layout.

			// foreach Context in todolist
			// Add a TreeViewItem with Header=@Context
			// foreach Item in Context
			// Add a TreeViewItem under the Context item.
			// Add a Checkbox to the TreeViewItem with Content=Item Text

		}
	}
}
