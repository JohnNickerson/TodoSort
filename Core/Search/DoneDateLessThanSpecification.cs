using AssimilationSoftware.Maroon.Model;
using System;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class DoneDateLessThanSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? ToDoneDate;

        public DoneDateLessThanSpecification(DateTime? ToDoneDate)
        {
            this.ToDoneDate = ToDoneDate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return !ToDoneDate.HasValue || b.DoneDate < ToDoneDate;
        }
    }
}
