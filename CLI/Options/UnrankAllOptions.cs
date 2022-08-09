using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("unrank-all", HelpText = "Removes all ranking data from a set of items.")]
    public class UnrankAllOptions : MultiSearchSubOptions { }
}
