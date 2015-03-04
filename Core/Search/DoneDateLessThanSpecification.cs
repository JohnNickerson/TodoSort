using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
