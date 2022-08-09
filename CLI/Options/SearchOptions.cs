using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("search", HelpText = "Search through the collection of items.")]
    public class SearchOptions : MultiSearchSubOptions { }
}
