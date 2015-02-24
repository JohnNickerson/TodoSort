using AssimilationSoftware.TodoSort.Core.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class OrSpecification<T> : ISearchSpecification<T>
    {
        private ISearchSpecification<T> _conditionOne;
        private ISearchSpecification<T> _conditionTwo;

        public OrSpecification(ISearchSpecification<T> first, ISearchSpecification<T> second)
        {
            _conditionOne = first;
            _conditionTwo = second;
        }

        public bool IsSatisfiedBy(T b)
        {
            return _conditionOne.IsSatisfiedBy(b) || _conditionTwo.IsSatisfiedBy(b);
        }
    }
}
