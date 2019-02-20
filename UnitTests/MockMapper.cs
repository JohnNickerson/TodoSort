using AssimilationSoftware.Maroon.Interfaces;
using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.UnitTests
{
    internal class MockMapper : IMapper<ActionItem>
    {
        private List<ActionItem> _items = new List<ActionItem>();

        public ActionItem Load(Guid id)
        {
            var search = (from i in _items where i.ID == id select i);
            if (search.Count() > 0)
            {
                return search.First();
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<ActionItem> LoadAll()
        {
            return _items;
        }

        public void Save(ActionItem item)
        {
            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        public void SaveAll(IEnumerable<ActionItem> items)
        {
            _items = items.ToList();
        }

        public void Delete(ActionItem item)
        {
            if (_items.Contains(item))
            {
                _items.Remove(item);
            }
        }
    }
}
