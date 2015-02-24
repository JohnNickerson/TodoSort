using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class AndSpecification<T> : ISearchSpecification<T>
    {
        private ISearchSpecification<T> _conditionOne;
        private ISearchSpecification<T> _conditionTwo;

        public AndSpecification(ISearchSpecification<T> first, ISearchSpecification<T> second)
        {
            _conditionOne = first;
            _conditionTwo = second;
        }

        public bool IsSatisfiedBy(T b)
        {
            return (_conditionOne.IsSatisfiedBy(b) && _conditionTwo.IsSatisfiedBy(b));
        }
    }
}
