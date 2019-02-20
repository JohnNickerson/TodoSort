using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssimilationSoftware.Maroon.Interfaces;
using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Data
{
    public class ActionItemSqliteMapper : IMapper<ActionItem>
    {
        public ActionItem Load(Guid id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ActionItem> LoadAll()
        {
            throw new NotImplementedException();
        }

        public void Save(ActionItem item)
        {
            throw new NotImplementedException();
        }

        public void SaveAll(IEnumerable<ActionItem> items)
        {
            throw new NotImplementedException();
        }

        public void Delete(ActionItem item)
        {
            throw new NotImplementedException();
        }
    }
}
