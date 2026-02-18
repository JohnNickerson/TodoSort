using AssimilationSoftware.Maroon.Model;
using System.Collections.Generic;

namespace AssimilationSoftware.TodoSort.Core.Export
{
    public interface IExporter
    {
        void Export(List<ActionItem> items);
    }
}
