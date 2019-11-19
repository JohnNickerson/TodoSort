using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public interface IImporter
    {
        Maroon.Model.ActionItem[] GetAllItems();
        bool IsValid { get; }
    }
}
