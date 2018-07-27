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
        }

        public IEnumerable<ActionItem> SomedayItems
        {
            get
            {
                return FindAll().Where(i => i.Context == "someday");
            }
        }

        public IEnumerable<ActionItem> DoneItems
        {
            get
            {
                return FindAll().Where(i => i.Context == "done");
            }
        }

        public IEnumerable<ActionItem> GetChildren(ActionItem selected)
        {
            return FindAll().Where(t => t.RankParent != null && t.RankParent.Equals(selected));
        }

        public IEnumerable<string> GetContexts(params string[] exclude)
        {
            return (from i in FindAll() select i.Context).Distinct().Except(exclude);
        }

        public IEnumerable<ActionItem> GetProjectItems(ActionItem i)
        {
            return FindAll().Where(a => a.Project != null && a.Project.Equals(i));
        }
    }
}
