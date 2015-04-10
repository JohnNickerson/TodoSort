using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class AndSpecification<T> : ISearchSpecification<T>
    {
        private List<ISearchSpecification<T>> _conditions;

        public AndSpecification(params ISearchSpecification<T>[] conds)
        {
            _conditions = new List<ISearchSpecification<T>>();
            _conditions.AddRange(conds);
        }

        public bool IsSatisfiedBy(T b)
        {
            bool sat = true;
            foreach (var cond in _conditions)
            {
                sat &= cond.IsSatisfiedBy(b);
                if (!sat) break;
            }
            return sat;
        }
    }
}
