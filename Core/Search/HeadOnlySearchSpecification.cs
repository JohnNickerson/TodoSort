using AssimilationSoftware.Maroon.Model;

namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class HeadOnlySearchSpecification : ISearchSpecification<ActionItem>
    {
        public bool IsSatisfiedBy(ActionItem b)
        {
            return !b.ParentId.HasValue;
        }
    }
}
