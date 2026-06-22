using AssimilationSoftware.Maroon.Model;
using System;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ReturnDateGreaterThanSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? FromReturnDate;

        public ReturnDateGreaterThanSpecification(DateTime? FromReturnDate)
        {
            this.FromReturnDate = FromReturnDate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return FromReturnDate.HasValue && b.TickleDate >= FromReturnDate;
        }
    }
}
