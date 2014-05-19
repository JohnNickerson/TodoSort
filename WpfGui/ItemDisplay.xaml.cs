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
using AssimilationSoftware.PimData.Model;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    /// <summary>
    /// Interaction logic for ItemDisplay.xaml
    /// </summary>
    public partial class ItemDisplay : UserControl
    {
        public delegate void DeleteItemEventHandler(object sender, DeleteItemEventArgs e);
        public event DeleteItemEventHandler DeleteItem;

        public ItemDisplay()
        {
            InitializeComponent();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteItem != null)
            {
                DeleteItem(this, new DeleteItemEventArgs { Item = (ActionItem)DataContext });
            }
        }
    }
}
