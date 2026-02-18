using AssimilationSoftware.Maroon.Model;
using System;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TickleDateLessThanSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? ToTickleDate;

        public TickleDateLessThanSpecification(DateTime? ToTickleDate)
        {
            this.ToTickleDate = ToTickleDate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return ToTickleDate.HasValue && b.TickleDate <= ToTickleDate;
        }
    }
}
