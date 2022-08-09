using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("defer-all", HelpText = "Defer all items that match given search criteria.")]
    public class DeferAllOptions : MultiSearchSubOptions { }
}
