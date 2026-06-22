using AssimilationSoftware.Maroon.Model;
using System;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ReturnDateLessThanSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? ToReturnDate;

        public ReturnDateLessThanSpecification(DateTime? ToReturnDate)
        {
            this.ToReturnDate = ToReturnDate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return ToReturnDate.HasValue && b.TickleDate <= ToReturnDate;
        }
    }
}
