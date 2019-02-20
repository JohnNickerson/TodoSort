using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ContextSearchSpecification : ISearchSpecification<ActionItem>
    {
        public ContextSearchSpecification(string context)
        {
            this.Context = context;
        }
        public string Context { get; set; }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.Context?.ToLower() == Context.ToLower();
        }
    }
}
