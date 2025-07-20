using AssimilationSoftware.Maroon.Model;
using AssimilationSoftware.TodoSort.Core.Search;
using CommandLine;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("bump", HelpText = "Halves the depth of an item, as a way of increasing its priority")]
    public class BumpOptions : UniversalOptions
    {
        [Option('s', "search", HelpText = "A partial name, tag value or note contents to search for.")]
        public string? SearchTerm { get; set; }

        [Option('i', "id", HelpText = "The beginning of the ID of the item to work on. If present, overrides full-text search.")]
        public string? ItemId { get; set; }

        // Tag name
        [Option('t', "tag", HelpText = "Tag name.")]
        public string? TagName { get; set; }

        // Tag value
        [Option('u', "value", HelpText = "Tag value.")]
        public string? TagValue { get; set; }

        // Project ID
        [Option("project", HelpText = "The beginning of a project ID.")]
        public string? ProjectId { get; set; }

        [Option("top", HelpText = "The maximum number of search results to bump.")]
        public int Top { get; set; }

        [Option("sort", HelpText = "The name of a tag to sort by.")]
        public string? SortTag { get; set; }

        [Option("sort-desc", HelpText = "Specifies a tag by which to sort in descending order. Will not be used if 'sort' is present.")]
        public string? SortDescTag { get; set; }



        public ISearchSpecification<ActionItem> SearchSpecification
        {
            get
            {
                var conditions = new List<ISearchSpecification<ActionItem>>();
                if (!string.IsNullOrEmpty(ItemId))
                {
                    conditions.Add(new IdSearchSpecification(ItemId));
                }
                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    conditions.Add(new FullTextSearchSpecification(SearchTerm));
                }
                if (!string.IsNullOrEmpty(ProjectId))
                {
                    conditions.Add(new ProjectChildrenSearchSpecification(ProjectId));
                }
                if (!string.IsNullOrEmpty(TagName) || !string.IsNullOrEmpty(TagValue))
                {
                    conditions.Add(new TagValueSpecification(TagName, TagValue));
                }
                return new AndSpecification<ActionItem>(conditions.ToArray());
            }
        }
    }
}
