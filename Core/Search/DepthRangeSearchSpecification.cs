using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssimilationSoftware.Maroon.Interfaces;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class DepthRangeSearchSpecification : ISearchSpecification<ActionItem>
    {
        private int _maxDepth;
        private int _minDepth;
        private IRepository<ActionItem> _repo;
        
        public DepthRangeSearchSpecification(int min, int max, IRepository<ActionItem> repository)
        {
            _minDepth = min;
            _maxDepth = max;
            _repo = repository;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.GetRankDepth(_repo) <= _maxDepth && b.GetRankDepth(_repo) >= _minDepth;
        }
    }
}
