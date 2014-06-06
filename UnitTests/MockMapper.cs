using AssimilationSoftware.PimData.Interfaces;
using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.UnitTests
{
    internal class MockMapper : IPimDataMapper<ActionItem>
    {
        private List<ActionItem> _items = new List<ActionItem>();

        public PimData.Model.ActionItem Load(Guid id)
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

        public List<PimData.Model.ActionItem> LoadAll()
        {
            return _items;
        }

        public void Save(PimData.Model.ActionItem item)
        {
            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        public void SaveAll(List<PimData.Model.ActionItem> items)
        {
            _items = items;
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
