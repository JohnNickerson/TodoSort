using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public static class SpecificationExtensionMethods
    {
        public static ISearchSpecification<T> And<T>(this ISearchSpecification<T> specOne, ISearchSpecification<T> specTwo)
        {
            return new AndSpecification<T>(specOne, specTwo);
        }

        public static ISearchSpecification<T> Or<T>(this ISearchSpecification<T> specOne, ISearchSpecification<T> specTwo)
        {
            return new OrSpecification<T>(specOne, specTwo);
        }

        public static ISearchSpecification<T> Not<T>(this ISearchSpecification<T> negateMe)
        {
            return new NotSpecification<T>(negateMe);
        }
    }
}
