using AssimilationSoftware.Maroon.Model;
using System;
using System.Linq.Expressions;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class PartialPropertyValueSpecification<P> : ISearchSpecification<ActionItem>
    {
        Func<ActionItem, P> _propExpression;
        P _checkValue;

        public PartialPropertyValueSpecification(Expression<Func<ActionItem, P>> property, P value)
        {
            _propExpression = property.Compile();
            _checkValue = value;
        }

        public bool IsSatisfiedBy(ActionItem item)
        {
            return _propExpression.Invoke(item).ToString().ToLower().Contains(_checkValue.ToString().ToLower());
        }
    }
}
