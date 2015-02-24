using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ExactPropertyValueSpecification<T, P> : ISearchSpecification<T>
    {
        Func<T, P> _propExpression;
        P _checkValue;

        public ExactPropertyValueSpecification(Expression<Func<T, P>> property, P value)
        {
            _propExpression = property.Compile();
            _checkValue = value;
        }
        
        public bool IsSatisfiedBy(T b)
        {
            return _propExpression.Invoke(b).Equals(_checkValue);
        }
    }
}
