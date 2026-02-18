namespace AssimilationSoftware.TodoSort.Core.Search
{
    public interface ISearchSpecification<T>
    {
        bool IsSatisfiedBy(T b);
    }
}
