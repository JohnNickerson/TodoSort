using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("commit", HelpText = "Commits pending changes to the main data file")]
    public class CommitOptions : UniversalOptions
    {
    }
}
