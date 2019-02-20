using AssimilationSoftware.Maroon.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ProjectChildrenSearchSpecification : ISearchSpecification<ActionItem>
    {
        private string _projectId;

        public ProjectChildrenSearchSpecification(ActionItem project)
        {
            _projectId = project.ID.ToString().ToLower();
        }

        public ProjectChildrenSearchSpecification(string id)
        {
            _projectId = id;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.Project != null && b.Project.ID.ToString().ToLower().StartsWith(_projectId);
        }
    }
}
