using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("check-chain", HelpText = "Checks an implicit chain for missing and excluded items")]
    public class CheckChainOptions : MultiSearchSubOptions { }
}
