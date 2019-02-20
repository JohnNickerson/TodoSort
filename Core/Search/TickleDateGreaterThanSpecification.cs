using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TickleDateGreaterThanSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? FromTickleDate;

        public TickleDateGreaterThanSpecification(DateTime? FromTickleDate)
        {
            this.FromTickleDate = FromTickleDate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return FromTickleDate.HasValue && b.TickleDate >= FromTickleDate;
        }
    }
}
