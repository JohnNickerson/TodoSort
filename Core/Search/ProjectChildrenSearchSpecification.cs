using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class ProjectChildrenSearchSpecification : ISearchSpecification<ActionItem>
    {
        private string _projectId;

        public ProjectChildrenSearchSpecification(ActionItem project)
        {
            _projectId = project?.ID.ToString().ToLower();
        }

        public ProjectChildrenSearchSpecification(string id)
        {
            _projectId = id;
        }

        public bool IsSatisfiedBy(ActionItem b)
        {
            return b.ProjectId != null && b.ProjectId.ToString().ToLower().StartsWith(_projectId);
        }
    }
}
