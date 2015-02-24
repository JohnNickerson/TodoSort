using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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
