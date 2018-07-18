using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Data
{
    public interface ITodoRepository : IRepository<ActionItem>
    {
        IEnumerable<ActionItem> SomedayItems { get; }
        IEnumerable<ActionItem> DoneItems { get; }

        IEnumerable<ActionItem> GetProjectItems(ActionItem i);
        IEnumerable<string> GetContexts(params string[] exclude);
        IEnumerable<ActionItem> GetChildren(ActionItem selected);
    }
}
