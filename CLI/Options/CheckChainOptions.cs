using CommandLine;

namespace AssimilationSoftware.TodoSort.CLI.Options
{
    [Verb("check-chain", HelpText = "Checks an implicit chain for missing and excluded items")]
    public class CheckChainOptions : MultiSearchSubOptions
    {
        [Option("pause-on-problem", HelpText = "Pause for user input if there's a problem discovered", Default = false)]
        public bool PauseOnProblems { get; set; }
    }
}
