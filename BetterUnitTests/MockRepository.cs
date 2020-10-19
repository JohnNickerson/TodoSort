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

        public void Update(ActionItem entity, bool isNew = false)
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
            return  _items.Values.Where(p => p.ProjectId != null && p.ProjectId == i.ID);
        }

        public IEnumerable<string> GetContexts(params string[] exclude)
        {
            return _items.Values.Select(i => i.Context).Distinct().Except(exclude);
        }

        public IEnumerable<ActionItem> GetChildren(ActionItem i)
        {
            return _items.Values.Where(p => p.ParentId != null && p.ParentId == i.ID);
        }

        public int GetRankDepth(ActionItem i)
        {
            if (!i.ParentId.HasValue)
            {
                return 0;
            }
            else
            {
                return GetRankDepth(Find(i.ParentId.Value)) + 1;
            }
        }

        public int CommitChanges()
        {
            throw new NotImplementedException();
        }

        public List<PendingChange<ActionItem>> FindConflicts()
        {
            throw new NotImplementedException();
        }

        public void ResolveConflict(ActionItem item)
        {
            throw new NotImplementedException();
        }

        public void ResolveByDelete(Guid id)
        {
            throw new NotImplementedException();
        }

        public void Revert(Guid id)
        {
            throw new NotImplementedException();
        }

        public List<PendingChange<ActionItem>> GetPendingChanges()
        {
            throw new NotImplementedException();
        }
    }
}
