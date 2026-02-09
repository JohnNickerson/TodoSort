using System.Collections.Generic;

namespace AssimilationSoftware.TodoSort.Core.Import
{
    public interface IImporter
    {
        IEnumerable<Maroon.Model.ActionItem> GetAllItems();
        bool IsValid { get; }
    }
}
