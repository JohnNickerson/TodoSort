using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class NotSpecification<T> : ISearchSpecification<T>
    {
        private List<ISearchSpecification<T>> _conditions;

        public NotSpecification(params ISearchSpecification<T>[] cond)
        {
            _conditions = new List<ISearchSpecification<T>>();
            _conditions.AddRange(cond);
        }

        public bool IsSatisfiedBy(T b)
        {
            return !_conditions.Any(c => c.IsSatisfiedBy(b));
        }
    }
}
