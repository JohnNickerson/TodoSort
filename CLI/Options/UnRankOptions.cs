using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("unrank", HelpText = "Remove priority ranking data for one particular item.")]
    public class UnRankOptions : SingleSearchSubOptions { }
}
