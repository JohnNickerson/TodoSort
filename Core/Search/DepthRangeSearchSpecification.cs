using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class DepthRangeSearchSpecification : ISearchSpecification<ActionItem>
    {
        private int _maxDepth;
        private int _minDepth;
        
        public DepthRangeSearchSpecification(int min, int max)
        {
            _minDepth = min;
            _maxDepth = max;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.RankDepth <= _maxDepth && b.RankDepth >= _minDepth;
        }
    }
}
