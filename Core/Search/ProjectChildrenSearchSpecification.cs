using AssimilationSoftware.PimData.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ProjectChildrenSearchSpecification : ISearchSpecification<ActionItem>
    {
        private ActionItem _project;

        public ProjectChildrenSearchSpecification(ActionItem project)
        {
            _project = project;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.Project == _project;
        }
    }
}
