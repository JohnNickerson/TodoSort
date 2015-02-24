using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class NotSpecification<T> : ISearchSpecification<T>
    {
        private ISearchSpecification<T> condition;

        public NotSpecification(ISearchSpecification<T> cond)
        {
            condition = cond;
        }

        public bool IsSatisfiedBy(T b)
        {
            return !condition.IsSatisfiedBy(b);
        }
    }
}
