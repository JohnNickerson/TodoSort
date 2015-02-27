using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TickleDateSearchSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? _maxDate;
        private DateTime? _minDate;
        
        public TickleDateSearchSpecification(DateTime? mindate, DateTime? maxdate)
        {
            _minDate = mindate;
            _maxDate = maxdate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            if (_minDate.HasValue)
            {
                if (_maxDate.HasValue)
                {
                    return _minDate.Value <= b.TickleDate && b.TickleDate <= _maxDate.Value;
                }
                else
                {
                    return _minDate.Value <= b.TickleDate;
                }
            }
            else
            {
                if (_maxDate.HasValue)
                {
                    return b.TickleDate <= _maxDate.Value;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
