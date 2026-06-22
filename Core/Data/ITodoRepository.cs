using AssimilationSoftware.Maroon.Interfaces;
using AssimilationSoftware.Maroon.Model;
using System.Collections.Generic;

namespace AssimilationSoftware.TodoSort.Core.Data
{
    public interface ITodoRepository : IMergeRepository<ActionItem>
    {
        IEnumerable<ActionItem> SnoozedItems { get; }
        IEnumerable<ActionItem> DoneItems { get; }

        IEnumerable<ActionItem> GetProjectItems(ActionItem i);
        IEnumerable<string> GetContexts(params string[] exclude);
        IEnumerable<ActionItem> GetChildren(ActionItem selected);
    }
}
