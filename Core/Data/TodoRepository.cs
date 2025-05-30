using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.Maroon.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssimilationSoftware.TodoSort.Core.Data
{
    public class TodoRepository : MergeDiskRepository<ActionItem>, ITodoRepository
    {
        public TodoRepository(Maroon.Interfaces.IMapper<ActionItem> mapper, string path, string thisMachineName) : base(mapper, path)
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
            return Items.Where(t => t.ParentId != null && t.ParentId.Equals(selected.ID));
        }

        public IEnumerable<string> GetContexts(params string[] exclude)
        {
            return (from i in Items select i.Context).Distinct().Except(exclude);
        }

        public IEnumerable<ActionItem> GetProjectItems(ActionItem i)
        {
            return Items.Where(a => a.ProjectId != null && a.ProjectId.Equals(i.ID));
        }
    }
}
