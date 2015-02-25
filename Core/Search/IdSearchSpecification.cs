using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class IdSearchSpecification : ISearchSpecification<ActionItem>
    {
        private string _id;

        public IdSearchSpecification(string id)
        {
            _id = id.ToLower();
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.ID.ToString().ToLower().StartsWith(_id);
        }
    }
}
