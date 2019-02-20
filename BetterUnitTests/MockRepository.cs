using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Data;

namespace BetterUnitTests
{
    class MockRepository : ITodoRepository
    {
        private Dictionary<Guid, ActionItem> _items = new Dictionary<Guid, ActionItem>();

        public ActionItem Find(Guid id)
        {
            if (_items.ContainsKey(id))
            {
                return _items[id];
            }

            return null;
        }

        public IEnumerable<ActionItem> FindAll()
        {
            return _items.Values;
        }

        public void Create(ActionItem entity)
        {
            _items[entity.ID] = entity;
        }

        public void Delete(ActionItem entity)
        {
            if (_items.ContainsKey(entity.ID))
            {
                _items.Remove(entity.ID);
            }
        }

        public void Update(ActionItem entity)
        {
            _items[entity.ID] = entity;
        }

        public void SaveChanges()
        {
        }

        public IEnumerable<ActionItem> Items => _items.Values;
        public IEnumerable<ActionItem> SomedayItems => _items.Values.Where(i => i.Context == "someday");
        public IEnumerable<ActionItem> DoneItems => _items.Values.Where(i => i.Context == "done");
        public IEnumerable<ActionItem> GetProjectItems(ActionItem i)
        {
            return  _items.Values.Where(p => p.Project != null && p.Project.ID == i.ID);
        }

        public IEnumerable<string> GetContexts(params string[] exclude)
        {
            return _items.Values.Select(i => i.Context).Distinct().Except(exclude);
        }

        public IEnumerable<ActionItem> GetChildren(ActionItem i)
        {
            return _items.Values.Where(p => p.Parent != null && p.Parent.ID == i.ID);
        }
    }
}
