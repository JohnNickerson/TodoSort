using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AssimilationSoftware.TodoSort.WpfGui.Annotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AssimilationSoftware.TodoSort.WpfGui
{
    public class ViewModelBase : ObservableObject
    {
        public Window Window { get; set; }
    }
}
