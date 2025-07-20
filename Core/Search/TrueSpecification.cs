namespace AssimilationSoftware.TodoSort.Core.Search
{
    public class TrueSpecification<T> : ISearchSpecification<T>
    {
        public bool IsSatisfiedBy(T b)
        {
            return true;
        }
    }
}
