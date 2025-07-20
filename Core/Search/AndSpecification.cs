using System.Collections.Generic;

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
                if (cond != null)
                {
                    sat &= cond.IsSatisfiedBy(b);
                }
                if (!sat) break;
            }
            return sat;
        }
    }
}
