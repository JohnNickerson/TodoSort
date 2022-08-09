using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("rank", HelpText = "Vote on the relative importance of items to assign priorities.")]
    public class RankOptions : MultiSearchSubOptions { }
}
