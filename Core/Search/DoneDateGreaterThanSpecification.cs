using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class DoneDateGreaterThanSpecification : ISearchSpecification<ActionItem>
    {
        private DateTime? FromDoneDate;

        public DoneDateGreaterThanSpecification(DateTime? FromDoneDate)
        {
            this.FromDoneDate = FromDoneDate;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return !FromDoneDate.HasValue || b.DoneDate >= FromDoneDate;
        }
    }
}
