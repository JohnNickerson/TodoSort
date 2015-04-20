using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class PriorityChildrenSearchSpecification : ISearchSpecification<ActionItem>
    {
        private string _parentId;

        public PriorityChildrenSearchSpecification(ActionItem parent)
        {
            _parentId = parent.ID.ToString().ToLower();
        }

        public PriorityChildrenSearchSpecification(string id)
        {
            _parentId = id;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.RankParent != null && b.RankParent.ID.ToString().ToLower().StartsWith(_parentId);
        }
    }
}
