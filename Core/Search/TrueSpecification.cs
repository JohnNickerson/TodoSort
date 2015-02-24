using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TrueSpecification<T> : ISearchSpecification<T>
    {
        public bool IsSatisfiedBy(T b)
        {
            return true;
        }
    }
}
