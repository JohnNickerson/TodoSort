using AssimilationSoftware.Maroon.Model;
using System;
using AssimilationSoftware.TodoSort.Core.Data;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    [Obsolete("This is just too slow to use now.")]
    public class DepthRangeSearchSpecification : ISearchSpecification<ActionItem>
    {
        private int _maxDepth;
        private int _minDepth;
        private ITodoRepository _repo;
        
        public DepthRangeSearchSpecification(int min, int max, ITodoRepository repository)
        {
            _minDepth = min;
            _maxDepth = max;
            _repo = repository;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            var d = b.GetRankDepth(_repo);
            return d <= _maxDepth && d >= _minDepth;
        }
    }
}
