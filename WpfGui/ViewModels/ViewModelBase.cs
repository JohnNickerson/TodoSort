using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class ViewModelBase : ObservableObject
    {
        public Window Window { get; set; }
    }
}
