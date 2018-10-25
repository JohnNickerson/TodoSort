using AssimilationSoftware.PimData.Model;
using AssimilationSoftware.PimData.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Data
{
    public class TodoRepository : DiskRepository<ActionItem>, ITodoRepository
    {
        public TodoRepository(PimData.Interfaces.IPimDataMapper<ActionItem> mapper) : base(mapper)
        {
			FindAll();
        }

        public IEnumerable<ActionItem> SomedayItems
        {
            get
            {
                return Items.Where(i => i.Context == "someday");
            }
        }

        public IEnumerable<ActionItem> DoneItems
        {
            get
            {
                return Items.Where(i => i.Context == "done");
            }
        }

        public IEnumerable<ActionItem> GetChildren(ActionItem selected)
        {
            return Items.Where(t => t.RankParent != null && t.RankParent.Equals(selected));
        }

        public IEnumerable<string> GetContexts(params string[] exclude)
        {
            return (from i in Items select i.Context).Distinct().Except(exclude);
        }

        public IEnumerable<ActionItem> GetProjectItems(ActionItem i)
        {
            return Items.Where(a => a.Project != null && a.Project.Equals(i));
        }
    }
}
